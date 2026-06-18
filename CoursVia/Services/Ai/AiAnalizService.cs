using System.Text.Encodings.Web;
using System.Text.Json;
using CoursVia.ViewModels.Ai;

namespace CoursVia.Services.Ai;

// Eğitmen ve öğrenci AI analizlerini ilgili modele yöneten ana servis.
public class AiAnalizService
{
    private readonly AiPromptBuilder _promptBuilder;
    private readonly GeminiAiService _geminiAiService;
    private readonly LocalGemmaAiService _localGemmaAiService;
    private readonly MiniCoursViaAiService _miniCoursViaAiService;

    // MiniCoursVia Python script'i snake_case JSON beklediği için property isimleri elle korunur.
    // snake case bir JSON oluşturmak için özel JsonSerializerOptions tanımlanır; bu sayede property isimleri olduğu gibi kalır, nokta atışı okunur.
    private static readonly JsonSerializerOptions MiniCoursViaJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = null,
        WriteIndented = false
    };

    public AiAnalizService(
        AiPromptBuilder promptBuilder,
        GeminiAiService geminiAiService,
        LocalGemmaAiService localGemmaAiService,
        MiniCoursViaAiService miniCoursViaAiService)
    {
        _promptBuilder = promptBuilder;
        _geminiAiService = geminiAiService;
        _localGemmaAiService = localGemmaAiService;
        _miniCoursViaAiService = miniCoursViaAiService;
    }

    // Eğitmen kurs analizini üretir; prompt tabanlı modeller için prompt, MiniCoursVia için JSON hazırlar.
    public async Task<List<AiAnalizSonucu>> EgitmenKursAnaliziAsync(
        AiEgitmenKursAnalizVerisi veri,
        AiModelTipi modelTipi,
        CancellationToken cancellationToken = default)
    {
        // Gemini ve LocalGemma doğal dil promptu ile çalışır.
        var prompt = _promptBuilder.EgitmenKursAnaliziPromptOlustur(veri);

        // MiniCoursVia ise prompt değil, kısa ve kontrollü JSON veri ister.
        var miniCoursViaJson = MiniCoursViaEgitmenJsonOlustur(veri);

        return await CevapUretAsync(
            prompt,
            miniCoursViaJson,
            AiIstekTipi.EgitmenKursAnalizi,
            modelTipi,
            cancellationToken);
    }

    // Öğrenci çalışma önerisini üretir; seçilen modele göre tek veya çoklu sonuç döndürür.
    public async Task<List<AiAnalizSonucu>> OgrenciCalismaOnerisiAsync(
        AiOgrenciCalismaVerisi veri,
        AiModelTipi modelTipi,
        CancellationToken cancellationToken = default)
    {
        // Prompt modelleri için öğrenciye özel çalışma önerisi talimatı oluşturulur.
        var prompt = _promptBuilder.OgrenciCalismaOnerisiPromptOlustur(veri);

        // MiniCoursVia için aynı veri daha kompakt JSON formatına dönüştürülür.
        var miniCoursViaJson = MiniCoursViaOgrenciJsonOlustur(veri);

        return await CevapUretAsync(
            prompt,
            miniCoursViaJson,
            AiIstekTipi.OgrenciCalismaOnerisi,
            modelTipi,
            cancellationToken);
    }

    private async Task<List<AiAnalizSonucu>> CevapUretAsync(
        string prompt,
        string miniCoursViaJson,
        AiIstekTipi istekTipi,
        AiModelTipi modelTipi,
        CancellationToken cancellationToken)
    {
        // "Hepsi" seçeneği model karşılaştırması için üç servisi de sırayla çalıştırır.
        if (modelTipi == AiModelTipi.Hepsi)
        {
            var sonuclar = new List<AiAnalizSonucu>
            {
                await _geminiAiService.CevapUretAsync(
                    prompt,
                    istekTipi,
                    cancellationToken),

                await _localGemmaAiService.CevapUretAsync(
                    prompt,
                    istekTipi,
                    cancellationToken),

                await _miniCoursViaAiService.CevapUretAsync(
                    miniCoursViaJson,
                    istekTipi,
                    cancellationToken)
            };

            return sonuclar;
        }

        // Tek model seçildiyse sadece ilgili servis çağrılır.
        var sonuc = modelTipi switch
        {
            AiModelTipi.Gemini => await _geminiAiService.CevapUretAsync(
                prompt,
                istekTipi,
                cancellationToken),

            AiModelTipi.LocalGemma => await _localGemmaAiService.CevapUretAsync(
                prompt,
                istekTipi,
                cancellationToken),

            AiModelTipi.MiniCoursVia => await _miniCoursViaAiService.CevapUretAsync(
                miniCoursViaJson,
                istekTipi,
                cancellationToken),

            // Enum dışı değerler kullanıcıya kontrollü hata olarak döndürülür.
            _ => AiAnalizSonucu.Hatali(
                modelTipi,
                "Bilinmeyen Model",
                "Geçersiz AI model tipi seçildi.")
        };

        return new List<AiAnalizSonucu> { sonuc };
    }

    // Eğitmen analiz verisini MiniCoursVia'nın beklediği küçük JSON payload'a çevirir.
    private static string MiniCoursViaEgitmenJsonOlustur(AiEgitmenKursAnalizVerisi veri)
    {
        // Controller zaten AiEgitmenKursAnalizVerisi doldurduğu için alanlar nokta atışı okunur.
        var zorlanilanDersler = veri.ZorlanilanDersler
            .Select(x => x.DersAdi)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // MiniCoursVia tek oran alanı beklediği için zorlanılan derslerdeki en yüksek yanlış oranı gönderilir.
        var yanlisOrani = veri.ZorlanilanDersler.Any()
            ? veri.ZorlanilanDersler.Max(x => x.YanlisOrani)
            : 0;

        // Python script'i Türkçe alan adı yerine sade snake_case alanlar bekler.
        var payload = new
        {
            kurs = veri.KursAdi,
            ogrenci_sayisi = veri.ToplamOgrenciSayisi,
            ortalama_puan = veri.OrtalamaPuan,
            tamamlanma = YuzdeFormatla(veri.GenelTamamlanmaOrani),
            zorlanilan_bolum = veri.ZorlanilanBolum,
            zorlanilan_dersler = zorlanilanDersler,
            yanlis_orani = YuzdeFormatla(yanlisOrani)
        };

        return JsonSerializer.Serialize(payload, MiniCoursViaJsonOptions);
    }

    // Öğrenci çalışma verisini MiniCoursVia'nın beklediği JSON payload'a çevirir.
    private static string MiniCoursViaOgrenciJsonOlustur(AiOgrenciCalismaVerisi veri)
    {
        // Controller zaten AiOgrenciCalismaVerisi doldurduğu için alanlar doğrudan okunur.
        var yanlisYapilanDersler = veri.YanlisYapilanDersler
            .Select(x => x.DersAdi)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // MiniCoursVia tarafı gereksiz uzun prompt yerine sadece bu alanlarla çalışır.
        var payload = new
        {
            kurs = veri.KursAdi,
            sinav_puani = veri.SinavPuani,
            gecme_puani = veri.GecmePuani,
            zorlanilan_bolum = veri.YanlislarinYogunlastigiBolum,
            zorlanilan_dersler = yanlisYapilanDersler
        };

        return JsonSerializer.Serialize(payload, MiniCoursViaJsonOptions);
    }

    // 0-1 arası oranları yüzdeye çevirir, 0-100 arası değerleri olduğu gibi yüzde formatında yazar.
    private static string YuzdeFormatla(decimal value)
    {
        // Decimal oran 0-1 aralığındaysa yüzde karşılığına dönüştürülür.
        if (value > 0 && value <= 1)
            value *= 100;

        return $"%{value:0}";
    }
}

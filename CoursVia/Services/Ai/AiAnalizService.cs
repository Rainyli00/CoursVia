using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using CoursVia.ViewModels.Ai;

namespace CoursVia.Services.Ai;

public class AiAnalizService
{
    private readonly AiPromptBuilder _promptBuilder;
    private readonly GeminiAiService _geminiAiService;
    private readonly LocalGemmaAiService _localGemmaAiService;
    private readonly MiniCoursViaAiService _miniCoursViaAiService;

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

    public async Task<List<AiAnalizSonucu>> EgitmenKursAnaliziAsync(
        AiEgitmenKursAnalizVerisi veri,
        AiModelTipi modelTipi,
        CancellationToken cancellationToken = default)
    {
        var prompt = _promptBuilder.EgitmenKursAnaliziPromptOlustur(veri);
        var miniCoursViaJson = MiniCoursViaEgitmenJsonOlustur(veri);

        return await CevapUretAsync(
            prompt,
            miniCoursViaJson,
            AiIstekTipi.EgitmenKursAnalizi,
            modelTipi,
            cancellationToken);
    }

    public async Task<List<AiAnalizSonucu>> OgrenciCalismaOnerisiAsync(
        AiOgrenciCalismaVerisi veri,
        AiModelTipi modelTipi,
        CancellationToken cancellationToken = default)
    {
        var prompt = _promptBuilder.OgrenciCalismaOnerisiPromptOlustur(veri);
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

            _ => AiAnalizSonucu.Hatali(
                modelTipi,
                "Bilinmeyen Model",
                "Geçersiz AI model tipi seçildi.")
        };

        return new List<AiAnalizSonucu> { sonuc };
    }

    private static string MiniCoursViaEgitmenJsonOlustur(AiEgitmenKursAnalizVerisi veri)
    {
        var kursAdi = GetString(veri, "KursAdi", "Kurs", "KursAdı");
        var toplamOgrenciSayisi = GetInt(veri, "ToplamOgrenciSayisi", "OgrenciSayisi", "KayitliOgrenciSayisi");
        var ortalamaPuan = GetDecimal(veri, "OrtalamaPuan", "KursOrtalamaPuan", "PuanOrtalamasi");

        var tamamlanma = GetObject(veri, "GenelTamamlanmaOrani", "TamamlanmaOrani", "Tamamlanma", "GenelTamamlanma");
        var zorlanilanBolum = GetString(veri, "ZorlanilanBolum", "ZorlanilanBolumAdi", "YanlislarinYogunlastigiBolum");

        var zorlanilanDerslerObj = GetObject(veri, "ZorlanilanDersler", "YanlisYapilanDersler", "Dersler");
        var zorlanilanDersler = DersAdlariniAl(zorlanilanDerslerObj);

        var yanlisOrani = GetObject(veri, "YanlisOrani", "EnYuksekYanlisOrani", "ZorlanilanYanlisOrani")
            ?? EnYuksekOranAl(zorlanilanDerslerObj, "YanlisOrani", "HataOrani", "YanlisYuzdesi");

        var payload = new
        {
            kurs = kursAdi,
            ogrenci_sayisi = toplamOgrenciSayisi,
            ortalama_puan = ortalamaPuan,
            tamamlanma = YuzdeFormatla(tamamlanma),
            zorlanilan_bolum = zorlanilanBolum,
            zorlanilan_dersler = zorlanilanDersler,
            yanlis_orani = YuzdeFormatla(yanlisOrani)
        };

        return JsonSerializer.Serialize(payload, MiniCoursViaJsonOptions);
    }

    private static string MiniCoursViaOgrenciJsonOlustur(AiOgrenciCalismaVerisi veri)
    {
        var kursAdi = GetString(veri, "KursAdi", "Kurs", "KursAdı");
        var sinavPuani = GetInt(veri, "SinavPuani", "Puan", "AlinanPuan");
        var gecmePuani = GetInt(veri, "GecmePuani", "BasariPuani", "MinimumGecmePuani");

        var zorlanilanBolum = GetString(
            veri,
            "YanlislarinYogunlastigiBolum",
            "ZorlanilanBolum",
            "ZorlanilanBolumAdi");

        var yanlisYapilanDerslerObj = GetObject(veri, "YanlisYapilanDersler", "ZorlanilanDersler", "Dersler");
        var yanlisYapilanDersler = DersAdlariniAl(yanlisYapilanDerslerObj);

        var payload = new
        {
            kurs = kursAdi,
            sinav_puani = sinavPuani,
            gecme_puani = gecmePuani,
            zorlanilan_bolum = zorlanilanBolum,
            zorlanilan_dersler = yanlisYapilanDersler
        };

        return JsonSerializer.Serialize(payload, MiniCoursViaJsonOptions);
    }

    private static object? GetObject(object source, params string[] propertyNames)
    {
        var type = source.GetType();

        foreach (var propertyName in propertyNames)
        {
            var property = type.GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (property == null)
                continue;

            return property.GetValue(source);
        }

        return null;
    }

    private static string GetString(object source, params string[] propertyNames)
    {
        var value = GetObject(source, propertyNames);

        if (value == null)
            return string.Empty;

        return Convert.ToString(value, CultureInfo.CurrentCulture)?.Trim() ?? string.Empty;
    }

    private static int GetInt(object source, params string[] propertyNames)
    {
        var value = GetObject(source, propertyNames);

        if (value == null)
            return 0;

        if (value is int intValue)
            return intValue;

        if (value is long longValue)
            return (int)longValue;

        if (value is decimal decimalValue)
            return (int)Math.Round(decimalValue);

        if (value is double doubleValue)
            return (int)Math.Round(doubleValue);

        if (value is float floatValue)
            return (int)Math.Round(floatValue);

        var text = Convert.ToString(value, CultureInfo.CurrentCulture);

        if (string.IsNullOrWhiteSpace(text))
            return 0;

        text = text.Replace("%", "").Trim();

        if (int.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out var parsedCurrent))
            return parsedCurrent;

        if (int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedInvariant))
            return parsedInvariant;

        return 0;
    }

    private static decimal GetDecimal(object source, params string[] propertyNames)
    {
        var value = GetObject(source, propertyNames);

        return ConvertToDecimal(value);
    }

    private static decimal ConvertToDecimal(object? value)
    {
        if (value == null)
            return 0;

        if (value is decimal decimalValue)
            return decimalValue;

        if (value is double doubleValue)
            return (decimal)doubleValue;

        if (value is float floatValue)
            return (decimal)floatValue;

        if (value is int intValue)
            return intValue;

        if (value is long longValue)
            return longValue;

        var text = Convert.ToString(value, CultureInfo.CurrentCulture);

        if (string.IsNullOrWhiteSpace(text))
            return 0;

        text = text.Replace("%", "").Trim();

        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out var parsedCurrent))
            return parsedCurrent;

        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedInvariant))
            return parsedInvariant;

        text = text.Replace(",", ".");

        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedDot))
            return parsedDot;

        return 0;
    }

    private static List<string> DersAdlariniAl(object? value)
    {
        var dersler = new List<string>();

        if (value == null)
            return dersler;

        if (value is string stringValue)
        {
            dersler.AddRange(
                stringValue
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

            return dersler;
        }

        if (value is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item == null)
                    continue;

                if (item is string itemString)
                {
                    if (!string.IsNullOrWhiteSpace(itemString))
                    {
                        dersler.Add(itemString.Trim());
                    }

                    continue;
                }

                var dersAdi = GetString(item, "DersAdi", "DersAdı", "Ders", "Ad", "Adi", "Adı", "LessonName");

                if (!string.IsNullOrWhiteSpace(dersAdi))
                {
                    dersler.Add(dersAdi);
                }
            }
        }

        return dersler
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static object? EnYuksekOranAl(object? value, params string[] propertyNames)
    {
        if (value == null || value is string)
            return null;

        if (value is not IEnumerable enumerable)
            return null;

        decimal? max = null;

        foreach (var item in enumerable)
        {
            if (item == null)
                continue;

            foreach (var propertyName in propertyNames)
            {
                var rawValue = GetObject(item, propertyName);

                if (rawValue == null)
                    continue;

                var oran = ConvertToDecimal(rawValue);

                if (!max.HasValue || oran > max.Value)
                {
                    max = oran;
                }
            }
        }

        return max;
    }

    private static string YuzdeFormatla(object? value)
    {
        if (value == null)
            return "%0";

        if (value is string stringValue)
        {
            stringValue = stringValue.Trim();

            if (string.IsNullOrWhiteSpace(stringValue))
                return "%0";

            if (stringValue.StartsWith("%"))
                return stringValue;

            var parsedString = ConvertToDecimal(stringValue);

            if (parsedString <= 0)
                return "%0";

            if (parsedString > 0 && parsedString <= 1)
                parsedString *= 100;

            return $"%{parsedString:0}";
        }

        var numeric = ConvertToDecimal(value);

        if (numeric > 0 && numeric <= 1)
            numeric *= 100;

        return $"%{numeric:0}";
    }
}
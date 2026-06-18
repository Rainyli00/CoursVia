using System.Diagnostics;
using System.Text;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Options;

namespace CoursVia.Services.Ai;

// Google Gemini API üzerinden AI analiz cevabı üreten servis.
public class GeminiAiService
{
    private readonly AiSettings _settings;
    private readonly AiCiktiGuvenlikFiltresi _guvenlikFiltresi;

    public GeminiAiService(
        IOptions<AiSettings> settings,
        AiCiktiGuvenlikFiltresi guvenlikFiltresi)
    {
        _settings = settings.Value;
        _guvenlikFiltresi = guvenlikFiltresi;
    }

    // Verilen promptu Gemini modeline gönderir, ham cevabı temizler ve standart analiz sonucu döndürür.
    public async Task<AiAnalizSonucu> CevapUretAsync(
        string prompt,
        AiIstekTipi istekTipi,
        CancellationToken cancellationToken = default)
    {
        // UI'da/karşılaştırmada gösterilebilmesi için model çağrı süresi ölçülür.
        var stopwatch = Stopwatch.StartNew();

        // appsettings boşsa varsayılan Gemini modeli kullanılır.
        var model = string.IsNullOrWhiteSpace(_settings.Gemini.Model)
            ? "gemini-3-flash-preview"
            : _settings.Gemini.Model;

        try
        {
            // API key yoksa dış servise çıkmadan kontrollü hata döndürülür.
            if (string.IsNullOrWhiteSpace(_settings.Gemini.ApiKey))
            {
                stopwatch.Stop();

                return AiAnalizSonucu.Hatali(
                    AiModelTipi.Gemini,
                    model,
                    "Gemini API key bulunamadı.",
                    stopwatch.ElapsedMilliseconds);
            }

            var client = new Client(apiKey: _settings.Gemini.ApiKey);

            // Düşük sıcaklık, eğitim önerilerinde daha tutarlı ve az yaratıcı çıktı üretmek için seçildi.
            var config = new GenerateContentConfig
            {
                Temperature = 0.1,
                // top_p=0.75, modelin olası kelime dağılımını daraltır ve daha tutarlı çıktılar üretir.
                TopP = 0.75,
 
                MaxOutputTokens = 1500,

                // Python testlerinde thinking_budget=0 ile tam çıktı aldık.
                // Gemini 3 tarafında minimal düşünme davranışı hedefleniyor.
                ThinkingConfig = new ThinkingConfig
                {
                    ThinkingBudget = 0
                }
            };

            // Prompt doğrudan Gemini içerik üretme endpointine gönderilir.
            var response = await client.Models.GenerateContentAsync(
                model: model,
                contents: prompt,
                config: config,
                cancellationToken: cancellationToken);

            stopwatch.Stop();

            // Gemini cevabı candidate/part yapısından düz metne çevrilir.
            var hamCikti = GeminiCevabiniOku(response);

            if (string.IsNullOrWhiteSpace(hamCikti))
            {
                return AiAnalizSonucu.Hatali(
                    AiModelTipi.Gemini,
                    model,
                    "Gemini boş çıktı döndürdü.",
                stopwatch.ElapsedMilliseconds);
            }

            // Kullanıcıya dönmeden önce teknik tokenlar ve gereksiz biçim temizlenir.
            var filtreSonucu = _guvenlikFiltresi.Temizle(hamCikti, istekTipi);

            return AiAnalizSonucu.Basarili(
                AiModelTipi.Gemini,
                model,
                hamCikti,
                filtreSonucu.TemizCikti,
                stopwatch.ElapsedMilliseconds,
                filtreSonucu.GuvenlikFiltresiUygulandiMi);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Dış API hataları uygulamayı düşürmeden standart hata sonucuna çevrilir.
            return AiAnalizSonucu.Hatali(
                AiModelTipi.Gemini,
                model,
                GeminiHataMesajiTemizle(ex),
                stopwatch.ElapsedMilliseconds);
        }
    }

    // Gemini response içindeki tüm text part'larını birleştirerek tek çıktı metni üretir.
    private static string GeminiCevabiniOku(GenerateContentResponse response)
    {
        var builder = new StringBuilder();

        // Candidate yoksa model cevap üretmemiş kabul edilir.
        if (response.Candidates == null || response.Candidates.Count == 0)
            return string.Empty;

        foreach (var candidate in response.Candidates)
        {
            // Bazı hata/güvenlik durumlarında Content veya Parts boş gelebilir.
            if (candidate.Content?.Parts == null)
                continue;

            foreach (var part in candidate.Content.Parts)
            {
                if (!string.IsNullOrWhiteSpace(part.Text))
                {
                    builder.AppendLine(part.Text.Trim());
                }
            }
        }
        // Builder ile birleştirilen metin, baştaki ve sondaki boşluklardan arındırılır ve döndürülür.
        return builder.ToString().Trim();
    }

    // Gemini exception mesajını kullanıcı/log ekranı için makul uzunlukta tutar.
    private static string GeminiHataMesajiTemizle(Exception ex)
    {
        var message = ex.Message;

        if (string.IsNullOrWhiteSpace(message))
            return "Gemini API çağrısında bilinmeyen hata oluştu.";

        if (message.Length > 1000)
            return message[..1000] + "...";

        return message;
    }
}

using System.Diagnostics;
using System.Text;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Options;

namespace CoursVia.Services.Ai;

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

    public async Task<AiAnalizSonucu> CevapUretAsync(
        string prompt,
        AiIstekTipi istekTipi,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var model = string.IsNullOrWhiteSpace(_settings.Gemini.Model)
            ? "gemini-3-flash-preview"
            : _settings.Gemini.Model;

        try
        {
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

            var config = new GenerateContentConfig
            {
                Temperature = 0.1,
                TopP = 0.75,
                MaxOutputTokens = 1500,

                // Python testlerinde thinking_budget=0 ile tam çıktı aldık.
                // Gemini 3 tarafında minimal düşünme davranışı hedefleniyor.
                ThinkingConfig = new ThinkingConfig
                {
                    ThinkingBudget = 0
                }
            };

            var response = await client.Models.GenerateContentAsync(
                model: model,
                contents: prompt,
                config: config,
                cancellationToken: cancellationToken);

            stopwatch.Stop();

            var hamCikti = GeminiCevabiniOku(response);

            if (string.IsNullOrWhiteSpace(hamCikti))
            {
                return AiAnalizSonucu.Hatali(
                    AiModelTipi.Gemini,
                    model,
                    "Gemini boş çıktı döndürdü.",
                    stopwatch.ElapsedMilliseconds);
            }

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

            return AiAnalizSonucu.Hatali(
                AiModelTipi.Gemini,
                model,
                GeminiHataMesajiTemizle(ex),
                stopwatch.ElapsedMilliseconds);
        }
    }

    private static string GeminiCevabiniOku(GenerateContentResponse response)
    {
        var builder = new StringBuilder();

        if (response.Candidates == null || response.Candidates.Count == 0)
            return string.Empty;

        foreach (var candidate in response.Candidates)
        {
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

        return builder.ToString().Trim();
    }

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
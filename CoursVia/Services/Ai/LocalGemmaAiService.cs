using System.ClientModel;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace CoursVia.Services.Ai;

public class LocalGemmaAiService
{
    private readonly AiSettings _settings;
    private readonly AiCiktiGuvenlikFiltresi _guvenlikFiltresi;

    public LocalGemmaAiService(
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

        var model = string.IsNullOrWhiteSpace(_settings.LocalGemma.Model)
            ? "gemma-3-12b-it"
            : _settings.LocalGemma.Model;

        try
        {
            if (string.IsNullOrWhiteSpace(_settings.LocalGemma.BaseUrl))
            {
                stopwatch.Stop();

                return AiAnalizSonucu.Hatali(
                    AiModelTipi.LocalGemma,
                    model,
                    "Local Gemma BaseUrl bulunamadı.",
                    stopwatch.ElapsedMilliseconds);
            }

            var endpoint = _settings.LocalGemma.BaseUrl.TrimEnd('/');

            var chatClient = new ChatClient(
                model: model,
                credential: new ApiKeyCredential("lm-studio"),
                options: new OpenAIClientOptions
                {
                    Endpoint = new Uri(endpoint)
                });

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(
                    "Sen CoursVia için çalışan Türkçe bir eğitim analizi asistanısın. " +
                    "Kurallara sıkı uy, yeni sayı/süre uydurma, sadece istenen başlıkları kullan."),
                new UserChatMessage(prompt)
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0.1f,
                TopP = 0.75f,
                MaxOutputTokenCount = 700
            };

            var response = await chatClient.CompleteChatAsync(
                messages,
                options,
                cancellationToken);

            stopwatch.Stop();

            var completion = response.Value;

            var hamCikti = completion.Content.Count > 0
                ? completion.Content[0].Text?.Trim() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(hamCikti))
            {
                return AiAnalizSonucu.Hatali(
                    AiModelTipi.LocalGemma,
                    model,
                    "Local Gemma boş çıktı döndürdü.",
                    stopwatch.ElapsedMilliseconds);
            }

            var filtreSonucu = _guvenlikFiltresi.Temizle(hamCikti, istekTipi);

            return AiAnalizSonucu.Basarili(
                AiModelTipi.LocalGemma,
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
                AiModelTipi.LocalGemma,
                model,
                LocalGemmaHataMesajiTemizle(ex),
                stopwatch.ElapsedMilliseconds);
        }
    }

    private static string LocalGemmaHataMesajiTemizle(Exception ex)
    {
        var message = ex.Message;

        if (string.IsNullOrWhiteSpace(message))
            return "Local Gemma çağrısında bilinmeyen hata oluştu.";

        if (message.Length > 1000)
            return message[..1000] + "...";

        return message;
    }
}
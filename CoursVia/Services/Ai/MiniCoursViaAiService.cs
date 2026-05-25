using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace CoursVia.Services.Ai;

public class MiniCoursViaAiService
{
    private readonly AiSettings _aiSettings;
    private readonly AiCiktiGuvenlikFiltresi _guvenlikFiltresi;

    public MiniCoursViaAiService(
        IOptions<AiSettings> settings,
        AiCiktiGuvenlikFiltresi guvenlikFiltresi)
    {
        _aiSettings = settings.Value;
        _guvenlikFiltresi = guvenlikFiltresi;
    }

    public async Task<AiAnalizSonucu> CevapUretAsync(
        string jsonVeri,
        AiIstekTipi istekTipi,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        const string modelAdi = "MiniCoursViaLLM";

        try
        {
            if (string.IsNullOrWhiteSpace(jsonVeri))
            {
                stopwatch.Stop();

                return AiAnalizSonucu.Hatali(
                    AiModelTipi.MiniCoursVia,
                    modelAdi,
                    "MiniCoursVia için gönderilecek JSON veri boş olamaz.",
                    stopwatch.ElapsedMilliseconds);
            }

            var temizJsonVeri = jsonVeri.Trim();

            if (!temizJsonVeri.StartsWith("{"))
            {
                stopwatch.Stop();

                return AiAnalizSonucu.Hatali(
                    AiModelTipi.MiniCoursVia,
                    modelAdi,
                    "MiniCoursVia'ya JSON yerine farklı bir metin gönderildi. AiAnalizService içinde MiniCoursVia için prompt değil JSON gönderilmelidir.",
                    stopwatch.ElapsedMilliseconds);
            }

            var pythonExePath = string.IsNullOrWhiteSpace(_aiSettings.MiniCoursVia.PythonExePath)
                ? "python"
                : _aiSettings.MiniCoursVia.PythonExePath;

            var scriptPath = _aiSettings.MiniCoursVia.ScriptPath;

            if (string.IsNullOrWhiteSpace(scriptPath))
            {
                stopwatch.Stop();

                return AiAnalizSonucu.Hatali(
                    AiModelTipi.MiniCoursVia,
                    modelAdi,
                    "MiniCoursVia script path appsettings.json içinde bulunamadı.",
                    stopwatch.ElapsedMilliseconds);
            }

            if (!File.Exists(scriptPath))
            {
                stopwatch.Stop();

                return AiAnalizSonucu.Hatali(
                    AiModelTipi.MiniCoursVia,
                    modelAdi,
                    $"MiniCoursVia script dosyası bulunamadı: {scriptPath}",
                    stopwatch.ElapsedMilliseconds);
            }

            var mode = IstekTipindenModeGetir(istekTipi);

            var workingDirectory = string.IsNullOrWhiteSpace(_aiSettings.MiniCoursVia.WorkingDirectory)
                ? Path.GetDirectoryName(scriptPath)
                : _aiSettings.MiniCoursVia.WorkingDirectory;

            if (string.IsNullOrWhiteSpace(workingDirectory))
            {
                workingDirectory = Directory.GetCurrentDirectory();
            }

            var timeoutSeconds = _aiSettings.MiniCoursVia.TimeoutSeconds <= 0
                ? 180
                : _aiSettings.MiniCoursVia.TimeoutSeconds;

            var processStartInfo = new ProcessStartInfo
            {
                FileName = pythonExePath,
                Arguments = $"\"{scriptPath}\" --mode {mode}",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardInputEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            processStartInfo.Environment["PYTHONIOENCODING"] = "utf-8";
            processStartInfo.Environment["PYTHONUTF8"] = "1";

            using var process = new Process
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = false
            };

            if (!process.Start())
            {
                stopwatch.Stop();

                return AiAnalizSonucu.Hatali(
                    AiModelTipi.MiniCoursVia,
                    modelAdi,
                    "MiniCoursVia Python process başlatılamadı.",
                    stopwatch.ElapsedMilliseconds);
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.StandardInput.WriteAsync(temizJsonVeri.AsMemory(), cancellationToken);
            await process.StandardInput.FlushAsync(cancellationToken);
            process.StandardInput.Close();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Process kapanırken ekstra hata fırlatmasın.
                }

                stopwatch.Stop();

                return AiAnalizSonucu.Hatali(
                    AiModelTipi.MiniCoursVia,
                    modelAdi,
                    $"MiniCoursVia işlem zaman aşımına uğradı. Süre: {timeoutSeconds} saniye.",
                    stopwatch.ElapsedMilliseconds);
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            stopwatch.Stop();

            if (process.ExitCode != 0)
            {
                var hata = string.IsNullOrWhiteSpace(stderr)
                    ? $"MiniCoursVia process hata kodu döndürdü: {process.ExitCode}"
                    : stderr;

                return AiAnalizSonucu.Hatali(
                    AiModelTipi.MiniCoursVia,
                    modelAdi,
                    HataMesajiTemizle(hata),
                    stopwatch.ElapsedMilliseconds);
            }

            if (string.IsNullOrWhiteSpace(stdout))
            {
                var hata = string.IsNullOrWhiteSpace(stderr)
                    ? "MiniCoursVia boş çıktı döndürdü."
                    : stderr;

                return AiAnalizSonucu.Hatali(
                    AiModelTipi.MiniCoursVia,
                    modelAdi,
                    HataMesajiTemizle(hata),
                    stopwatch.ElapsedMilliseconds);
            }

            var pythonCevabi = PythonJsonCevabiniOku(stdout);

            if (pythonCevabi == null)
            {
                var detay = string.IsNullOrWhiteSpace(stderr)
                    ? stdout
                    : $"{stdout}\nSTDERR:\n{stderr}";

                return AiAnalizSonucu.Hatali(
                    AiModelTipi.MiniCoursVia,
                    modelAdi,
                    "MiniCoursVia JSON çıktısı okunamadı. Çıktı: " + HataMesajiTemizle(detay),
                    stopwatch.ElapsedMilliseconds);
            }

            if (!pythonCevabi.Success)
            {
                var hata = pythonCevabi.Errors != null && pythonCevabi.Errors.Any()
                    ? string.Join(" | ", pythonCevabi.Errors)
                    : "MiniCoursVia başarısız çıktı döndürdü.";

                return AiAnalizSonucu.Hatali(
                    AiModelTipi.MiniCoursVia,
                    pythonCevabi.Model ?? modelAdi,
                    HataMesajiTemizle(hata),
                    stopwatch.ElapsedMilliseconds);
            }

            if (string.IsNullOrWhiteSpace(pythonCevabi.Output))
            {
                return AiAnalizSonucu.Hatali(
                    AiModelTipi.MiniCoursVia,
                    pythonCevabi.Model ?? modelAdi,
                    "MiniCoursVia output alanı boş döndü.",
                    stopwatch.ElapsedMilliseconds);
            }

            var filtreSonucu = _guvenlikFiltresi.Temizle(
                pythonCevabi.Output,
                istekTipi);

            var hamCikti = string.IsNullOrWhiteSpace(pythonCevabi.RawOutput)
                ? pythonCevabi.Output
                : pythonCevabi.RawOutput;

            return AiAnalizSonucu.Basarili(
                AiModelTipi.MiniCoursVia,
                pythonCevabi.Model ?? modelAdi,
                hamCikti,
                filtreSonucu.TemizCikti,
                stopwatch.ElapsedMilliseconds,
                filtreSonucu.GuvenlikFiltresiUygulandiMi || pythonCevabi.FallbackUsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return AiAnalizSonucu.Hatali(
                AiModelTipi.MiniCoursVia,
                modelAdi,
                HataMesajiTemizle(ex.ToString()),
                stopwatch.ElapsedMilliseconds);
        }
    }

    private static string IstekTipindenModeGetir(AiIstekTipi istekTipi)
    {
        return istekTipi switch
        {
            AiIstekTipi.EgitmenKursAnalizi => "egitmen",
            AiIstekTipi.OgrenciCalismaOnerisi => "ogrenci",
            _ => throw new InvalidOperationException($"MiniCoursVia için desteklenmeyen istek tipi: {istekTipi}")
        };
    }

    private static MiniCoursViaPythonResponse? PythonJsonCevabiniOku(string stdout)
    {
        try
        {
            var temiz = stdout.Trim();

            var ilkJsonIndex = temiz.IndexOf('{');
            var sonJsonIndex = temiz.LastIndexOf('}');

            if (ilkJsonIndex < 0 || sonJsonIndex <= ilkJsonIndex)
            {
                return null;
            }

            temiz = temiz[ilkJsonIndex..(sonJsonIndex + 1)];

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<MiniCoursViaPythonResponse>(temiz, options);
        }
        catch
        {
            return null;
        }
    }

    private static string HataMesajiTemizle(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "MiniCoursVia işleminde bilinmeyen hata oluştu.";

        message = message.Trim();

        if (message.Length > 1500)
            return message[..1500] + "...";

        return message;
    }

    private sealed class MiniCoursViaPythonResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("output")]
        public string Output { get; set; } = string.Empty;

        [JsonPropertyName("fallback_used")]
        public bool FallbackUsed { get; set; }

        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new();

        [JsonPropertyName("raw_output")]
        public string RawOutput { get; set; } = string.Empty;
    }
}
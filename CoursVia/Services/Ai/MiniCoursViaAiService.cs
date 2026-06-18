using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace CoursVia.Services.Ai;

// Python script'i üzerinden çalışan özel MiniCoursVia modelini çağıran servis.
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

    // MiniCoursVia'ya JSON veri gönderir, Python çıktısını okur ve standart analiz sonucuna çevirir.
    public async Task<AiAnalizSonucu> CevapUretAsync(
        string jsonVeri,
        AiIstekTipi istekTipi,
        CancellationToken cancellationToken = default)
    {
        // Diğer AI servisleriyle aynı formatta süre bilgisi döndürmek için ölçüm yapılır.
        var stopwatch = Stopwatch.StartNew();

        const string modelAdi = "MiniCoursViaLLM";

        try
        {
            // MiniCoursVia prompt değil JSON bekler; boş veri doğrudan hatadır.
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

            // Yanlışlıkla doğal dil promptu gönderilirse Python tarafına gitmeden yakalanır.
            if (!temizJsonVeri.StartsWith("{"))
            {
                stopwatch.Stop();

                return AiAnalizSonucu.Hatali(
                    AiModelTipi.MiniCoursVia,
                    modelAdi,
                    "MiniCoursVia'ya JSON yerine farklı bir metin gönderildi. AiAnalizService içinde MiniCoursVia için prompt değil JSON gönderilmelidir.",
                    stopwatch.ElapsedMilliseconds);
            }

            // Python yolu ayarlanmamışsa sistem PATH içindeki python kullanılır.
            var pythonExePath = string.IsNullOrWhiteSpace(_aiSettings.MiniCoursVia.PythonExePath)
                ? "python"
                : _aiSettings.MiniCoursVia.PythonExePath;

            var scriptPath = _aiSettings.MiniCoursVia.ScriptPath;

            // Script yolu appsettings içinde zorunludur.
            if (string.IsNullOrWhiteSpace(scriptPath))
            {
                stopwatch.Stop();

                return AiAnalizSonucu.Hatali(
                    AiModelTipi.MiniCoursVia,
                    modelAdi,
                    "MiniCoursVia script path appsettings.json içinde bulunamadı.",
                    stopwatch.ElapsedMilliseconds);
            }

            // Yanlış path durumunda Process başlatmadan anlaşılır hata döndürülür.
            if (!File.Exists(scriptPath))
            {
                stopwatch.Stop();

                return AiAnalizSonucu.Hatali(
                    AiModelTipi.MiniCoursVia,
                    modelAdi,
                    $"MiniCoursVia script dosyası bulunamadı: {scriptPath}",
                    stopwatch.ElapsedMilliseconds);
            }

            // İstek tipi Python script'inin --mode parametresine çevrilir.
            var mode = IstekTipindenModeGetir(istekTipi);

            // WorkingDirectory verilmezse script'in bulunduğu klasör kullanılır.
            var workingDirectory = string.IsNullOrWhiteSpace(_aiSettings.MiniCoursVia.WorkingDirectory)
                ? Path.GetDirectoryName(scriptPath)
                : _aiSettings.MiniCoursVia.WorkingDirectory;

            if (string.IsNullOrWhiteSpace(workingDirectory))
            {
                workingDirectory = Directory.GetCurrentDirectory();
            }

            // Yanlış veya sıfır timeout ayarı gelirse güvenli varsayılan süre kullanılır.
            var timeoutSeconds = _aiSettings.MiniCoursVia.TimeoutSeconds <= 0
                ? 180
                : _aiSettings.MiniCoursVia.TimeoutSeconds;

            // JSON veri stdin üzerinden verilecek, model cevabı stdout/stderr üzerinden okunacak.
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

            // Python tarafında Türkçe karakterlerin bozulmaması için UTF-8 zorlanır.
            processStartInfo.Environment["PYTHONIOENCODING"] = "utf-8";
            processStartInfo.Environment["PYTHONUTF8"] = "1";

            // Process tek seferlik çağrı için oluşturulur ve çağrı sonunda dispose edilir.
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

            // stdout/stderr okumaları beklemeden önce başlatılır ki buffer dolup process kilitlenmesin.
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            // MiniCoursVia input verisini stdin'den okur; yazma bitince input kapatılır.
            await process.StandardInput.WriteAsync(temizJsonVeri.AsMemory(), cancellationToken);
            await process.StandardInput.FlushAsync(cancellationToken);
            process.StandardInput.Close();

            // Kullanıcı iptali ve servis timeout'u birlikte izlenir.
            // Timeout durumunda process'in hala çalışıyor olması beklenir, bu yüzden timeout ve iptal token'ları birleştirilir.
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            // Process'in belirtilen süre içinde bitmesi beklenir; timeout veya iptal durumunda OperationCanceledException fırlatılır.
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

                // Timeout durumunda süreç öldürülür ve çağrı standart hata sonucu olarak döner.
                return AiAnalizSonucu.Hatali(
                    AiModelTipi.MiniCoursVia,
                    modelAdi,
                    $"MiniCoursVia işlem zaman aşımına uğradı. Süre: {timeoutSeconds} saniye.",
                    stopwatch.ElapsedMilliseconds);
            }

            // Process tamamlandıktan sonra stdout ve stderr okunur.
            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            stopwatch.Stop();

            // Python process hata koduyla biterse stderr öncelikli hata kaynağıdır.
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

            // Process başarılı bitse bile stdout boşsa model cevabı kullanılamaz.
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

            // Python çıktısı JSON zarfı olarak beklenir.
            var pythonCevabi = PythonJsonCevabiniOku(stdout);

            if (pythonCevabi == null)
            {
                // JSON parse edilemezse stdout ve varsa stderr kısaltılarak hata detayına eklenir.
                var detay = string.IsNullOrWhiteSpace(stderr)
                    ? stdout
                    : $"{stdout}\nSTDERR:\n{stderr}";

                return AiAnalizSonucu.Hatali(
                    AiModelTipi.MiniCoursVia,
                    modelAdi,
                    "MiniCoursVia JSON çıktısı okunamadı. Çıktı: " + HataMesajiTemizle(detay),
                    stopwatch.ElapsedMilliseconds);
            }

            // Python modeli kendi içinde başarısız olduysa errors alanı kullanıcıya/loga taşınır.
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

            // success=true olsa bile output boşsa kullanıcıya gösterilecek anlamlı metin yoktur.
            if (string.IsNullOrWhiteSpace(pythonCevabi.Output))
            {
                return AiAnalizSonucu.Hatali(
                    AiModelTipi.MiniCoursVia,
                    pythonCevabi.Model ?? modelAdi,
                    "MiniCoursVia output alanı boş döndü.",
                    stopwatch.ElapsedMilliseconds);
            }

            // Python output'u da ortak AI güvenlik filtresinden geçirilir.
            var filtreSonucu = _guvenlikFiltresi.Temizle(
                pythonCevabi.Output,
                istekTipi);

            // raw_output varsa ham çıktı olarak o saklanır, yoksa temizlenmeden önceki output kullanılır.
            var hamCikti = string.IsNullOrWhiteSpace(pythonCevabi.RawOutput)
                ? pythonCevabi.Output
                : pythonCevabi.RawOutput;

            return AiAnalizSonucu.Basarili(
                AiModelTipi.MiniCoursVia,
                pythonCevabi.Model ?? modelAdi,
                hamCikti,
                filtreSonucu.TemizCikti,
                // hamCikti, // Test için filtre sonucunu devre dışı bırakıp ham çıktıyı vermek için bu satırı açabilirsin
                stopwatch.ElapsedMilliseconds,
                filtreSonucu.GuvenlikFiltresiUygulandiMi || pythonCevabi.FallbackUsed);
                // false);   // Test için Temizlendi rozetini gizlemek istersen bu satırı açabilirsin
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Beklenmeyen .NET/process hataları da standart analiz hata sonucuna çevrilir.
            return AiAnalizSonucu.Hatali(
                AiModelTipi.MiniCoursVia,
                modelAdi,
                HataMesajiTemizle(ex.ToString()),
                stopwatch.ElapsedMilliseconds);
        }
    }

    // C# tarafındaki istek tipini Python script'in beklediği mode değerine çevirir.
    private static string IstekTipindenModeGetir(AiIstekTipi istekTipi)
    {
        return istekTipi switch
        {
            AiIstekTipi.EgitmenKursAnalizi => "egitmen",
            AiIstekTipi.OgrenciCalismaOnerisi => "ogrenci",
            _ => throw new InvalidOperationException($"MiniCoursVia için desteklenmeyen istek tipi: {istekTipi}")
        };
    }

    // Python stdout içinden JSON zarfını bulur ve MiniCoursViaPythonResponse modeline deserialize eder.
    private static MiniCoursViaPythonResponse? PythonJsonCevabiniOku(string stdout)
    {
        try
        {
            var temiz = stdout.Trim();

            // Python bazen JSON dışında log da yazarsa ilk ve son süslü parantez arası alınır.
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

            // Python tarafındaki snake_case property'ler JsonPropertyName ile eşlenir.
            return JsonSerializer.Deserialize<MiniCoursViaPythonResponse>(temiz, options);
        }
        catch
        {
            // Parse hatası çağıran tarafta okunabilir hata mesajına dönüştürülür.
            return null;
        }
    }

    // Uzun process/stdout hata mesajlarını ekranda gösterilebilir uzunluğa indirir.
    private static string HataMesajiTemizle(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "MiniCoursVia işleminde bilinmeyen hata oluştu.";

        message = message.Trim();

        if (message.Length > 1500)
            return message[..1500] + "...";

        return message;
    }

    // Python script'inin stdout üzerinden döndürdüğü JSON cevabın C# karşılığı.
    private sealed class MiniCoursViaPythonResponse
    {
        // Python tarafı işlemi başarılı tamamladı mı?
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        // Python tarafının raporladığı model adı.
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        // Kullanıcıya gösterilecek ana model çıktısı.
        [JsonPropertyName("output")]
        public string Output { get; set; } = string.Empty;

        // Python tarafında fallback cevap kullanıldı mı?
        [JsonPropertyName("fallback_used")]
        public bool FallbackUsed { get; set; }

        // Başarısız durumda Python tarafının döndürdüğü hata listesi.
        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new();

        // Varsa modelden gelen ham çıktı.
        [JsonPropertyName("raw_output")]
        public string RawOutput { get; set; } = string.Empty;
    }
}

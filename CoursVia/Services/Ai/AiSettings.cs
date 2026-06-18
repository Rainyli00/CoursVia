namespace CoursVia.Services.Ai;

// appsettings.json içindeki AI servis ayarlarını tek model altında toplar.
public class AiSettings
{
    // Google Gemini API bağlantı ayarları.
    public GeminiSettings Gemini { get; set; } = new();

    // Lokal OpenAI uyumlu Gemma/LM Studio bağlantı ayarları.
    public LocalGemmaSettings LocalGemma { get; set; } = new();

    // Python tabanlı MiniCoursVia modelinin çalışma ayarları.
    public MiniCoursViaSettings MiniCoursVia { get; set; } = new();
}

// Gemini için API anahtarı ve kullanılacak model adını tutar.
public class GeminiSettings
{
    // Google GenAI API anahtarı.
    public string ApiKey { get; set; } = string.Empty;

    // Ayar girilmezse servis tarafında varsayılan model kullanılır.
    public string Model { get; set; } = "gemini-3-flash-preview";
}

// Lokal çalışan OpenAI uyumlu model sunucusu için bağlantı bilgilerini tutar.
public class LocalGemmaSettings
{
    // LM Studio veya benzeri OpenAI compatible endpoint adresi.
    public string BaseUrl { get; set; } = "http://localhost:1234/v1";

    // Lokal servislerde çoğunlukla sembolik API key kullanılır.
    public string ApiKey { get; set; } = "lm-studio";

    // Lokal sunucuda yüklü model adı.
    public string Model { get; set; } = "gemma-3-12b-it";
}

// Python script'i ile çalışan özel MiniCoursVia modelinin ayarlarını tutar.
public class MiniCoursViaSettings
{
    // Python executable yolu; boşsa "python" varsayılır.
    public string PythonExePath { get; set; } = "python";

    // Çalıştırılacak MiniCoursVia Python script dosyasının yolu.
    public string ScriptPath { get; set; } = string.Empty;

    // Script'in çalışacağı klasör; boşsa script klasörü kullanılır.
    public string WorkingDirectory { get; set; } = string.Empty;

    // Python process için maksimum bekleme süresi.
    public int TimeoutSeconds { get; set; } = 180;
}

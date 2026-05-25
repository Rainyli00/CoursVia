namespace CoursVia.Services.Ai;

public class AiSettings
{
    public GeminiSettings Gemini { get; set; } = new();

    public LocalGemmaSettings LocalGemma { get; set; } = new();

    public MiniCoursViaSettings MiniCoursVia { get; set; } = new();
}

public class GeminiSettings
{
    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gemini-3-flash-preview";
}

public class LocalGemmaSettings
{
    public string BaseUrl { get; set; } = "http://localhost:1234/v1";

    public string ApiKey { get; set; } = "lm-studio";

    public string Model { get; set; } = "gemma-3-12b-it";
}

public class MiniCoursViaSettings
{
    public string PythonExePath { get; set; } = "python";

    public string ScriptPath { get; set; } = string.Empty;

    public string WorkingDirectory { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 180;
}
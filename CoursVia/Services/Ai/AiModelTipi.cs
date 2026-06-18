namespace CoursVia.Services.Ai;

// AI analizinde hangi modelin çalıştırılacağını belirler.
public enum AiModelTipi
{
    // Google Gemini API üzerinden cevap üretir.
    Gemini = 1,
    // Lokal OpenAI uyumlu Gemma modelinden cevap üretir.
    LocalGemma = 2,
    // Python script'i ile çalışan MiniCoursVia modelinden cevap üretir.
    MiniCoursVia = 3,
    // Karşılaştırma için tüm modelleri sırayla çalıştırır.
    Hepsi = 4
}

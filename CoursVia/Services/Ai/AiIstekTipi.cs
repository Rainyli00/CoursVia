namespace CoursVia.Services.Ai;

// Prompt ve çıktı filtresinin hangi senaryoya göre davranacağını belirtir.
public enum AiIstekTipi
{
    // Eğitmene kurs geliştirme önerisi üretilecek analiz tipi.
    EgitmenKursAnalizi = 1,

    OgrenciCalismaOnerisi = 2
}

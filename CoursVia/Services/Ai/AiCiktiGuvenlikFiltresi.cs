using System.Text;
using System.Text.RegularExpressions;

namespace CoursVia.Services.Ai;

public class AiCiktiGuvenlikFiltresi
{
    private static readonly string[] OrtakYasakliIfadeler =
    {
        "<|system|>",
        "<|user|>",
        "<|assistant|>",
        "GÖREV_TİPİ:",
        "İstenen çıktı:",
        "Kesin kurallar:",
        "Senden istenen:"
    };

    public AiFiltreSonucu Temizle(string? hamCikti, AiIstekTipi istekTipi)
    {
        if (string.IsNullOrWhiteSpace(hamCikti))
        {
            return new AiFiltreSonucu
            {
                TemizCikti = "AI çıktısı boş döndü.",
                GuvenlikFiltresiUygulandiMi = true
            };
        }

        var onceki = hamCikti.Trim();
        var temiz = onceki;

        temiz = TeknikTokenlariTemizle(temiz);
        temiz = MarkdownGurultusunuAzalt(temiz);
        temiz = GereksizBosluklariTemizle(temiz);

        if (string.IsNullOrWhiteSpace(temiz))
        {
            temiz = istekTipi == AiIstekTipi.OgrenciCalismaOnerisi
                ? OgrenciGuvenliFallback()
                : EgitmenGuvenliFallback();
        }

        return new AiFiltreSonucu
        {
            TemizCikti = temiz,
            GuvenlikFiltresiUygulandiMi = !string.Equals(onceki, temiz, StringComparison.Ordinal)
        };
    }

    private static string TeknikTokenlariTemizle(string text)
    {
        var temiz = text;

        foreach (var ifade in OrtakYasakliIfadeler)
        {
            temiz = temiz.Replace(ifade, string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return temiz;
    }

    private static string MarkdownGurultusunuAzalt(string text)
    {
        var temiz = text;

        temiz = temiz.Replace("```", string.Empty);
        temiz = Regex.Replace(temiz, @"^\s*#{1,6}\s*", string.Empty, RegexOptions.Multiline);
        temiz = Regex.Replace(temiz, @"^\s*[-*]\s+", "- ", RegexOptions.Multiline);

        return temiz.Trim();
    }

    private static string GereksizBosluklariTemizle(string text)
    {
        var satirlar = text
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Split('\n');

        var builder = new StringBuilder();
        var oncekiBosMu = false;

        foreach (var rawSatir in satirlar)
        {
            var satir = rawSatir.TrimEnd();

            if (string.IsNullOrWhiteSpace(satir))
            {
                if (!oncekiBosMu)
                {
                    builder.AppendLine();
                }

                oncekiBosMu = true;
                continue;
            }

            builder.AppendLine(satir);
            oncekiBosMu = false;
        }

        return builder.ToString().Trim();
    }

    private static string OgrenciGuvenliFallback()
    {
        return """
Genel durum

Sınav sonucuna göre bazı temel konuları tekrar gözden geçirmen gerekiyor.

Zorlandığın dersler

Yanlışlarının yoğunlaştığı derslerde konu mantığını ve işlem adımlarını yeniden incelemelisin.

Tekrar etmen gereken bölüm

Yanlışlarının yoğunlaştığı bölüme odaklanarak temel kavramları tekrar etmelisin.

Öncelikli çalışma planı

Önce konu anlatımlarını tekrar incele. Ardından işlem adımlarını gözden geçir. Daha sonra CoursVia içindeki ilgili ders kaynakları üzerinden pratik yaparak önceki hatalarını analiz et.
""";
    }

    private static string EgitmenGuvenliFallback()
    {
        return """
Genel kurs yorumu

Kurs genel olarak değerlendirildiğinde bazı derslerde öğrencilerin zorlandığı görülmektedir.

Zorlanılan bölüm yorumu

Zorlanılan bölümdeki anlatımın daha sade, örnekli ve adım adım ilerlemesi faydalı olacaktır.

Zorlanılan dersler için geliştirme önerisi

Zorlanılan derslerde kavramların daha açık anlatılması, görsel desteklerin artırılması ve mevcut içeriklerin daha anlaşılır hale getirilmesi önerilir.

Eğitmen için öncelikli aksiyon planı

Öncelikle zorlanılan dersleri gözden geçir. Ardından anlatımı destekleyecek özet materyaller hazırla. Son olarak öğrencilerin hata yaptığı noktaları daha açık şekilde açıklayan ek içerikler ekle.
""";
    }
}

public class AiFiltreSonucu
{
    public string TemizCikti { get; set; } = string.Empty;

    public bool GuvenlikFiltresiUygulandiMi { get; set; }
}
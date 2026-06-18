using System.Text;
using System.Text.RegularExpressions;

namespace CoursVia.Services.Ai;

// AI çıktısını kullanıcıya göstermeden önce teknik tokenlardan ve gereksiz biçimden temizler.
public class AiCiktiGuvenlikFiltresi
{
    // Prompt sızıntısı veya modelin konuşma rol tokenlarını döndürmesi durumunda temizlenecek ifadeler.
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

    // Ham model çıktısını senaryoya uygun, mobil/web ekranda gösterilebilir metne dönüştürür.
    public AiFiltreSonucu Temizle(string? hamCikti, AiIstekTipi istekTipi)
    {
        // Boş çıktı kullanıcıya ham hata gibi gitmesin diye güvenli fallback metni döndürülür.
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

        // Temizlik adımları sırayla uygulanır: teknik tokenlar, markdown gürültüsü ve boşluklar.
        temiz = TeknikTokenlariTemizle(temiz);
        temiz = MarkdownGurultusunuAzalt(temiz);
        temiz = GereksizBosluklariTemizle(temiz);

        // Tüm temizlikten sonra metin tamamen boşaldıysa istek tipine uygun güvenli cevap kullanılır.
        if (string.IsNullOrWhiteSpace(temiz))
        {
            temiz = istekTipi == AiIstekTipi.OgrenciCalismaOnerisi
                ? OgrenciGuvenliFallback()
                : EgitmenGuvenliFallback();
        }

        return new AiFiltreSonucu
        {
            // ham çıktıyıda görmek için temizcikti = hamCikti.Trim() yapılabilir.
            // temizlendi ibaresini kaldırmak içinde GuvenlikFiltresiUygulandiMi = false yapılabilir.
            TemizCikti = temiz,
            GuvenlikFiltresiUygulandiMi = !string.Equals(onceki, temiz, StringComparison.Ordinal)
        };
    }

    // Modelin çıktı içinde geri basabileceği sistem/user/assistant tokenlarını siler.
    private static string TeknikTokenlariTemizle(string text)
    {
        var temiz = text;

        // Liste küçük olduğu için basit Replace yeterli ve okunabilir.
        foreach (var ifade in OrtakYasakliIfadeler)
        {
            temiz = temiz.Replace(ifade, string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return temiz;
    }

    // Promptta istenmemesine rağmen gelen markdown işaretlerini azaltır.
    private static string MarkdownGurultusunuAzalt(string text)
    {
        var temiz = text;

        // Kod bloğu ve başlık işaretleri temizlenir; liste satırları sade tire formatına indirilir.
        temiz = temiz.Replace("```", string.Empty);
        temiz = Regex.Replace(temiz, @"^\s*#{1,6}\s*", string.Empty, RegexOptions.Multiline);
        temiz = Regex.Replace(temiz, @"^\s*[-*]\s+", "- ", RegexOptions.Multiline);

        return temiz.Trim();
    }

    // Satır sonlarını normalize eder ve art arda gelen boş satırları tek boş satıra düşürür.
    private static string GereksizBosluklariTemizle(string text)
    {
        var satirlar = text
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Split('\n');

        var builder = new StringBuilder();
        var oncekiBosMu = false;

        // Metin akışını korurken sadece gereksiz boşluk şişmesini azaltır.
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

    // Öğrenci önerisi tamamen temizlenirse kullanılacak güvenli varsayılan metin.
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

    // Eğitmen kurs analizi tamamen temizlenirse kullanılacak güvenli varsayılan metin.
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

// Temizlenmiş metinle birlikte filtrenin değişiklik yapıp yapmadığını taşır.
public class AiFiltreSonucu
{
    // Kullanıcıya gösterilecek son metin.
    public string TemizCikti { get; set; } = string.Empty;

    // Ham çıktı ile temiz çıktı farklıysa true olur.
    public bool GuvenlikFiltresiUygulandiMi { get; set; }
}

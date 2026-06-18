namespace CoursVia.Services.Ai;

// Her AI model çağrısının sonucunu tek formatta taşır.
public class AiAnalizSonucu
{
   
    public AiModelTipi ModelTipi { get; set; }

    // Ekranda/logda gösterilecek model adı.
    public string ModelAdi { get; set; } = string.Empty;

    // Model çağrısı teknik olarak başarılı oldu mu?
    public bool BasariliMi { get; set; }

  
    public string? HamCikti { get; set; }

    // Kullanıcıya gösterilmeye uygun hale getirilmiş metin.
    public string? TemizCikti { get; set; }


    public string? HataMesaji { get; set; }

    // Model çağrısının yaklaşık çalışma süresi.
    public long SureMs { get; set; }

    // Çıktı üzerinde güvenlik/temizlik filtresi değişiklik yaptı mı?
    public bool GuvenlikFiltresiUygulandiMi { get; set; }

  
    public static AiAnalizSonucu Basarili(
        AiModelTipi modelTipi,
        string modelAdi,
        string hamCikti,
        string temizCikti,
        long sureMs,
        bool guvenlikFiltresiUygulandiMi)
    {
        return new AiAnalizSonucu
        {
            ModelTipi = modelTipi,
            ModelAdi = modelAdi,
            BasariliMi = true,
            HamCikti = hamCikti,
            TemizCikti = temizCikti,
            SureMs = sureMs,
            GuvenlikFiltresiUygulandiMi = guvenlikFiltresiUygulandiMi
        };
    }

    // Hata durumlarını da aynı sonuç listesinde taşımak için standart hata nesnesi üretir.
    public static AiAnalizSonucu Hatali(
        AiModelTipi modelTipi,
        string modelAdi,
        string hataMesaji,
        long sureMs = 0)
    {
        return new AiAnalizSonucu
        {
            ModelTipi = modelTipi,
            ModelAdi = modelAdi,
            BasariliMi = false,
            HataMesaji = hataMesaji,
            SureMs = sureMs
        };
    }
}

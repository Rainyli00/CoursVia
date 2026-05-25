namespace CoursVia.ViewModels.Admin;

public class KullaniciYonetimiViewModel
{
    public string? Arama { get; set; }

    public string Rol { get; set; } = "tum";

    public string Durum { get; set; } = "tum";

    public List<KullaniciListeItemViewModel> Kullanicilar { get; set; } = new();

    public int ToplamKullaniciSayisi { get; set; }

    public int OnlineKullaniciSayisi { get; set; }

    public int AktifKullaniciSayisi { get; set; }

    public int PasifKullaniciSayisi { get; set; }

    public int AdminSayisi { get; set; }

    public int EgitmenSayisi { get; set; }

    public int OgrenciSayisi { get; set; }

    public int ToplamKayit { get; set; }

    public int Sayfa { get; set; } = 1;

    public int ToplamSayfa { get; set; } = 1;

    public int SayfaBasinaKayit { get; set; } = 10;

    public bool OncekiSayfaVar => Sayfa > 1;

    public bool SonrakiSayfaVar => Sayfa < ToplamSayfa;
}

public class KullaniciListeItemViewModel
{
    public int KullaniciId { get; set; }

    public string AdSoyad { get; set; } = string.Empty;

    public string Eposta { get; set; } = string.Empty;

    public string? ProfilFotoUrl { get; set; }

    public int DurumId { get; set; }

    public string DurumAdi { get; set; } = string.Empty;

    public bool OnlineMi { get; set; }

    public string? SonIpAdresi { get; set; }

    public DateTime KayitTarihi { get; set; }

    public DateTime? SonGirisTarihi { get; set; }

    public List<string> Roller { get; set; } = new();

    public bool EgitmenMi { get; set; }

    public string? UzmanlikAlani { get; set; }

    public List<string> Branslar { get; set; } = new();
}

public class KullaniciDetayViewModel
{
    public int KullaniciId { get; set; }

    public string Ad { get; set; } = string.Empty;

    public string Soyad { get; set; } = string.Empty;

    public string AdSoyad => $"{Ad} {Soyad}".Trim();

    public string Eposta { get; set; } = string.Empty;

    public string? Telefon { get; set; }

    public string? ProfilFotoUrl { get; set; }

    public int DurumId { get; set; }

    public string DurumAdi { get; set; } = string.Empty;

    public bool OnlineMi { get; set; }

    public string? SonIpAdresi { get; set; }

    public DateTime KayitTarihi { get; set; }

    public DateTime? SonGirisTarihi { get; set; }

    public List<string> Roller { get; set; } = new();

    public bool AdminMi => Roller.Any(x => x == "Admin");

    public bool EgitmenMi => Roller.Any(x => x == "Eğitmen");

    public bool OgrenciMi => Roller.Any(x => x == "Öğrenci");

    public KullaniciEgitmenDetayViewModel? EgitmenDetay { get; set; }

    public List<KullaniciKayitliKursViewModel> KayitliKurslar { get; set; } = new();

    public List<KullaniciVerdigiKursViewModel> VerdigiKurslar { get; set; } = new();

    public bool KursIliskisiVar => KayitliKurslar.Any() || VerdigiKurslar.Any();
}

public class KullaniciEgitmenDetayViewModel
{
    public int EgitmenProfilId { get; set; }

    public int DurumId { get; set; }

    public string DurumAdi { get; set; } = string.Empty;

    public string? UzmanlikAlani { get; set; }

    public string? Biyografi { get; set; }

    public int? DeneyimYili { get; set; }

    public string? WebsiteUrl { get; set; }

    public List<string> Branslar { get; set; } = new();
}

public class KullaniciKayitliKursViewModel
{
    public int KursKayitId { get; set; }

    public int KursId { get; set; }

    public string KursAdi { get; set; } = string.Empty;

    public DateTime KayitTarihi { get; set; }

    public bool AktifMi { get; set; }

    public bool TamamlandiMi { get; set; }

    public int ToplamDersSayisi { get; set; }

    public int TamamlananDersSayisi { get; set; }

    public int IlerlemeYuzdesi { get; set; }

    public int SinavGirisSayisi { get; set; }

    public double? SonSinavPuani { get; set; }

    public bool? SinavdanGectiMi { get; set; }

    public DateTime? SonSinavTarihi { get; set; }

    public string SinavDurumu { get; set; } = string.Empty;

    public string KayitDurumu
    {
        get
        {
            if (!AktifMi)
            {
                return "Pasif";
            }

            return TamamlandiMi
                ? "Tamamlandı"
                : "Aktif";
        }
    }
}

public class KullaniciVerdigiKursViewModel
{
    public int KursId { get; set; }

    public string KursAdi { get; set; } = string.Empty;

    public string? KapakGorselUrl { get; set; }

    public int DurumId { get; set; }

    public string DurumAdi { get; set; } = string.Empty;

    public DateTime OlusturmaTarihi { get; set; }

    public DateTime? GuncellemeTarihi { get; set; }

    public int DersSayisi { get; set; }

    public int OgrenciSayisi { get; set; }

    public List<string> Kategoriler { get; set; } = new();
}

public class KullaniciDuzenleViewModel
{
    public int KullaniciId { get; set; }

    public string Ad { get; set; } = string.Empty;

    public string Soyad { get; set; } = string.Empty;

    public string Eposta { get; set; } = string.Empty;

    public string? Telefon { get; set; }

    public int DurumId { get; set; }

    public List<string> Roller { get; set; } = new();

    public bool AdminYetkisiVarMi { get; set; }

    public bool EgitmenRoluVarMi { get; set; }

    public bool OgrenciRoluVarMi { get; set; }

    public bool EgitmenMi { get; set; }

    public int? EgitmenProfilId { get; set; }

    public int? EgitmenDurumId { get; set; }

    public string? UzmanlikAlani { get; set; }

    public string? Biyografi { get; set; }

    public int? DeneyimYili { get; set; }

    public string? WebsiteUrl { get; set; }

    public List<int> SeciliBransIdleri { get; set; } = new();

    public List<KullaniciBransSecimViewModel> BransSecenekleri { get; set; } = new();

    public List<KullaniciDurumSecimViewModel> DurumSecenekleri { get; set; } = new();
}

public class KullaniciBransSecimViewModel
{
    public int KategoriId { get; set; }

    public string KategoriAdi { get; set; } = string.Empty;

    public bool SeciliMi { get; set; }
}

public class KullaniciDurumSecimViewModel
{
    public int DurumId { get; set; }

    public string DurumAdi { get; set; } = string.Empty;
}

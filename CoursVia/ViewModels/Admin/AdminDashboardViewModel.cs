namespace CoursVia.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public string AdminAdSoyad { get; set; } = string.Empty;
    public int ToplamKullaniciSayisi { get; set; }

    public int OnlineKullaniciSayisi { get; set; }

    public int OnayBekleyenEgitmenSayisi { get; set; }

    public int OnayBekleyenKursSayisi { get; set; }

    public int YayindakiKursSayisi { get; set; }

    public int ReddedilenKursSayisi { get; set; }

    public List<AdminSonIslemViewModel> SonIslemler { get; set; } = new();
}

public class AdminSonIslemViewModel
{
    public string AdminAdSoyad { get; set; } = string.Empty;

    public string IslemTipi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    public DateTime IslemTarihi { get; set; }
}

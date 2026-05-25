namespace CoursVia.Models;

public class AdminLog
{
    public int AdminLogId { get; set; }

    public int? AdminId { get; set; }
    public int IslemTipId { get; set; }

    public string? Aciklama { get; set; }
    public string? IpAdresi { get; set; }

    public DateTime IslemTarihi { get; set; }

    // Navigation Properties
    public Kullanici? Admin { get; set; }
    public IslemTipi IslemTipi { get; set; } = null!;
}

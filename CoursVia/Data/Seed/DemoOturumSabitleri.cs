namespace CoursVia.Data.Seed;

public static class DemoOturumSabitleri
{
    public static readonly IReadOnlyList<DemoOturumBilgisi> Oturumlar =
    [
        // =========================================================
        // ADMINLER
        // =========================================================
        new(
            Eposta: "admin1@coursvia.com",
            SonIpAdresi: "192.168.1.10",
            OnlineMi: true,
            SonGirisGunOnce: 0,
            SonGirisSaatOnce: 1
        ),
        new(
            Eposta: "admin2@coursvia.com",
            SonIpAdresi: "192.168.1.11",
            OnlineMi: true,
            SonGirisGunOnce: 0,
            SonGirisSaatOnce: 2
        ),
        new(
            Eposta: "admin3@coursvia.com",
            SonIpAdresi: "192.168.1.12",
            OnlineMi: false,
            SonGirisGunOnce: 1,
            SonGirisSaatOnce: 4
        ),
        new(
            Eposta: "admin4@coursvia.com",
            SonIpAdresi: "192.168.1.13",
            OnlineMi: false,
            SonGirisGunOnce: 2,
            SonGirisSaatOnce: 3
        ),
        new(
            Eposta: "admin5@coursvia.com",
            SonIpAdresi: "192.168.1.14",
            OnlineMi: false,
            SonGirisGunOnce: 3,
            SonGirisSaatOnce: 5
        ),

        // =========================================================
        // EĞİTMENLER
        // =========================================================
        new(
            Eposta: "egitmen1@coursvia.com",
            SonIpAdresi: "192.168.1.21",
            OnlineMi: true,
            SonGirisGunOnce: 0,
            SonGirisSaatOnce: 1
        ),
        new(
            Eposta: "egitmen2@coursvia.com",
            SonIpAdresi: "192.168.1.22",
            OnlineMi: false,
            SonGirisGunOnce: 0,
            SonGirisSaatOnce: 7
        ),
        new(
            Eposta: "egitmen3@coursvia.com",
            SonIpAdresi: "192.168.1.23",
            OnlineMi: true,
            SonGirisGunOnce: 0,
            SonGirisSaatOnce: 3
        ),
        new(
            Eposta: "egitmen4@coursvia.com",
            SonIpAdresi: "192.168.1.24",
            OnlineMi: false,
            SonGirisGunOnce: 2,
            SonGirisSaatOnce: 6
        ),
        new(
            Eposta: "egitmen5@coursvia.com",
            SonIpAdresi: "192.168.1.25",
            OnlineMi: false,
            SonGirisGunOnce: 1,
            SonGirisSaatOnce: 9
        ),

        // =========================================================
        // ÖĞRENCİLER
        // =========================================================
        new(
            Eposta: "ogrenci1@coursvia.com",
            SonIpAdresi: "192.168.1.31",
            OnlineMi: true,
            SonGirisGunOnce: 0,
            SonGirisSaatOnce: 1
        ),
        new(
            Eposta: "ogrenci2@coursvia.com",
            SonIpAdresi: "192.168.1.32",
            OnlineMi: false,
            SonGirisGunOnce: 0,
            SonGirisSaatOnce: 5
        ),
        new(
            Eposta: "ogrenci3@coursvia.com",
            SonIpAdresi: "192.168.1.33",
            OnlineMi: true,
            SonGirisGunOnce: 0,
            SonGirisSaatOnce: 2
        ),
        new(
            Eposta: "ogrenci4@coursvia.com",
            SonIpAdresi: "192.168.1.34",
            OnlineMi: false,
            SonGirisGunOnce: 1,
            SonGirisSaatOnce: 1
        ),
        new(
            Eposta: "ogrenci5@coursvia.com",
            SonIpAdresi: "192.168.1.35",
            OnlineMi: false,
            SonGirisGunOnce: 1,
            SonGirisSaatOnce: 8
        ),
        new(
            Eposta: "ogrenci6@coursvia.com",
            SonIpAdresi: "192.168.1.36",
            OnlineMi: false,
            SonGirisGunOnce: 2,
            SonGirisSaatOnce: 4
        ),
        new(
            Eposta: "ogrenci7@coursvia.com",
            SonIpAdresi: "192.168.1.37",
            OnlineMi: true,
            SonGirisGunOnce: 0,
            SonGirisSaatOnce: 4
        ),
        new(
            Eposta: "ogrenci8@coursvia.com",
            SonIpAdresi: "192.168.1.38",
            OnlineMi: false,
            SonGirisGunOnce: 3,
            SonGirisSaatOnce: 2
        ),
        new(
            Eposta: "ogrenci9@coursvia.com",
            SonIpAdresi: "192.168.1.39",
            OnlineMi: false,
            SonGirisGunOnce: 2,
            SonGirisSaatOnce: 10
        ),
        new(
            Eposta: "ogrenci10@coursvia.com",
            SonIpAdresi: "192.168.1.40",
            OnlineMi: true,
            SonGirisGunOnce: 0,
            SonGirisSaatOnce: 6
        )
    ];

    public sealed record DemoOturumBilgisi(
        string Eposta,
        string SonIpAdresi,
        bool OnlineMi,
        int SonGirisGunOnce,
        int SonGirisSaatOnce
    );
}
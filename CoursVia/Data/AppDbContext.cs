using CoursVia.Models;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Durum> Durumlar { get; set; }
    public DbSet<Rol> Roller { get; set; }
    public DbSet<Kullanici> Kullanicilar { get; set; }
    public DbSet<KullaniciRol> KullaniciRolleri { get; set; }
    public DbSet<MobilOturum> MobilOturumlari { get; set; }
    public DbSet<SifreSifirlama> SifreSifirlamalari { get; set; }
    public DbSet<EgitmenProfili> EgitmenProfilleri { get; set; }
    public DbSet<EgitmenBransi> EgitmenBranslari { get; set; }
    public DbSet<Kategori> Kategoriler { get; set; }
    public DbSet<KursKategorisi> KursKategorileri { get; set; }
    public DbSet<Kurs> Kurslar { get; set; }
    public DbSet<Bolum> Bolumler { get; set; }
    public DbSet<Ders> Dersler { get; set; }
    public DbSet<MateryalTipi> MateryalTipleri { get; set; }
    public DbSet<DersMateryali> DersMateryalleri { get; set; }
    public DbSet<KursKaydi> KursKayitlari { get; set; }
    public DbSet<DersIlerlemesi> DersIlerlemeleri { get; set; }
    public DbSet<Sinav> Sinavlar { get; set; }
    public DbSet<Soru> Sorular { get; set; }
    public DbSet<SoruDersi> SoruDersleri { get; set; }
    public DbSet<SoruSecenegi> SoruSecenekleri { get; set; }
    public DbSet<SinavKatilimi> SinavKatilimlari { get; set; }
    public DbSet<OgrenciCevabi> OgrenciCevaplari { get; set; }
    public DbSet<KursDegerlendirmesi> KursDegerlendirmeleri { get; set; }
    public DbSet<Favori> Favoriler { get; set; }
    public DbSet<OneriTipi> OneriTipleri { get; set; }
    public DbSet<Oneri> Oneriler { get; set; }
    public DbSet<BildirimTipi> BildirimTipleri { get; set; }
    public DbSet<Bildirim> Bildirimler { get; set; }
    public DbSet<Sertifika> Sertifikalar { get; set; }
    public DbSet<EgitmenOnayi> EgitmenOnaylari { get; set; }
    public DbSet<KursOnayi> KursOnaylari { get; set; }
    public DbSet<IslemTipi> IslemTipleri { get; set; }
    public DbSet<AdminLog> AdminLoglari { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Durumlar
        modelBuilder.Entity<Durum>(entity =>
        {
            entity.ToTable("Durumlar");
            entity.HasKey(x => x.DurumId);

            entity.Property(x => x.DurumAdi)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(x => x.DurumAdi).IsUnique();
        });

        // Roller
        modelBuilder.Entity<Rol>(entity =>
        {
            entity.ToTable("Roller");
            entity.HasKey(x => x.RolId);

            entity.Property(x => x.RolAdi)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(x => x.RolAdi).IsUnique();
        });

        // Kullanicilar
        modelBuilder.Entity<Kullanici>(entity =>
        {
            entity.ToTable("Kullanicilar");
            entity.HasKey(x => x.KullaniciId);

            entity.Property(x => x.Ad).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Soyad).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Eposta).HasMaxLength(150).IsRequired();
            entity.Property(x => x.SifreHash).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Telefon).HasMaxLength(20);
            entity.Property(x => x.SonIpAdresi)
    .HasMaxLength(45);
            entity.Property(x => x.OnlineMi).HasDefaultValue(false);
            entity.Property(x => x.KayitTarihi).HasDefaultValueSql("SYSDATETIME()");

            entity.HasIndex(x => x.Eposta).IsUnique();

            entity.HasOne(x => x.Durum)
                .WithMany(x => x.Kullanicilar)
                .HasForeignKey(x => x.DurumId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // KullaniciRolleri
        modelBuilder.Entity<KullaniciRol>(entity =>
        {
            entity.ToTable("KullaniciRolleri");
            entity.HasKey(x => x.KullaniciRolId);

            entity.HasIndex(x => new { x.KullaniciId, x.RolId })
                .IsUnique();

            entity.HasOne(x => x.Kullanici)
                .WithMany(x => x.KullaniciRolleri)
                .HasForeignKey(x => x.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Rol)
                .WithMany(x => x.KullaniciRolleri)
                .HasForeignKey(x => x.RolId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // SifreSifirlamalari
        modelBuilder.Entity<SifreSifirlama>(entity =>
        {
            entity.ToTable("SifreSifirlamalari");
            entity.HasKey(x => x.SifreSifirlamaId);

            entity.Property(x => x.Kod)
                .HasMaxLength(6)
                .IsRequired();

            entity.Property(x => x.OlusturmaTarihi)
                .HasDefaultValueSql("SYSDATETIME()");

            entity.Property(x => x.KullanildiMi)
                .HasDefaultValue(false);

            entity.HasIndex(x => new { x.KullaniciId, x.Kod });

            entity.HasOne(x => x.Kullanici)
                .WithMany(x => x.SifreSifirlamalari)
                .HasForeignKey(x => x.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // MobilOturumlari 
        modelBuilder.Entity<MobilOturum>(entity =>
        {
            entity.HasKey(x => x.MobilOturumId);

            entity.Property(x => x.RefreshTokenHash)
                .IsRequired()
                .HasMaxLength(128);

            entity.Property(x => x.RefreshTokenBitisTarihi)
                .IsRequired();

            entity.Property(x => x.OlusturmaTarihi)
                .IsRequired();

            entity.Property(x => x.AktifMi)
                .IsRequired();

            entity.HasOne(x => x.Kullanici)
                .WithMany(x => x.MobilOturumlari)
                .HasForeignKey(x => x.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.KullaniciId);

            entity.HasIndex(x => x.RefreshTokenHash)
                .IsUnique();
        });

        // EgitmenProfilleri
        modelBuilder.Entity<EgitmenProfili>(entity =>
        {
            entity.ToTable("EgitmenProfilleri");
            entity.HasKey(x => x.EgitmenProfilId);

            entity.Property(x => x.UzmanlikAlani)
                .HasMaxLength(300);

            entity.HasIndex(x => x.KullaniciId)
                .IsUnique();

            entity.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_EgitmenProfilleri_DeneyimYili",
                    "[DeneyimYili] IS NULL OR [DeneyimYili] >= 0"
                );
            });

            entity.HasOne(x => x.Kullanici)
                .WithOne(x => x.EgitmenProfili)
                .HasForeignKey<EgitmenProfili>(x => x.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Durum)
                .WithMany()
                .HasForeignKey(x => x.DurumId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // EgitmenBranslari
        modelBuilder.Entity<EgitmenBransi>(entity =>
        {
            entity.ToTable("EgitmenBranslari");
            entity.HasKey(x => x.EgitmenBransId);

            entity.HasIndex(x => new { x.EgitmenProfilId, x.KategoriId })
                .IsUnique();

            entity.HasOne(x => x.EgitmenProfili)
                .WithMany(x => x.EgitmenBranslari)
                .HasForeignKey(x => x.EgitmenProfilId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Kategori)
                .WithMany(x => x.EgitmenBranslari)
                .HasForeignKey(x => x.KategoriId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Kategoriler
        modelBuilder.Entity<Kategori>(entity =>
        {
            entity.ToTable("Kategoriler");
            entity.HasKey(x => x.KategoriId);

            entity.Property(x => x.KategoriAdi)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(x => x.KategoriAdi).IsUnique();
        });

        // Kurslar
        modelBuilder.Entity<Kurs>(entity =>
        {
            entity.ToTable("Kurslar");
            entity.HasKey(x => x.KursId);

            entity.Property(x => x.KursAdi)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.KapakGorselUrl)
                .IsRequired();

            entity.Property(x => x.OlusturmaTarihi)
                .HasDefaultValueSql("SYSDATETIME()");

            entity.HasOne(x => x.Egitmen)
                .WithMany(x => x.EgitmenKurslari)
                .HasForeignKey(x => x.EgitmenId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Durum)
                .WithMany(x => x.Kurslar)
                .HasForeignKey(x => x.DurumId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        // KursKategorileri
        modelBuilder.Entity<KursKategorisi>(entity =>
        {
            entity.ToTable("KursKategorileri");
            entity.HasKey(x => x.KursKategoriId);

            entity.HasIndex(x => new { x.KursId, x.KategoriId })
                .IsUnique();

            entity.HasOne(x => x.Kurs)
                .WithMany(x => x.KursKategorileri)
                .HasForeignKey(x => x.KursId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Kategori)
                .WithMany(x => x.KursKategorileri)
                .HasForeignKey(x => x.KategoriId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Bolumler
        modelBuilder.Entity<Bolum>(entity =>
        {
            entity.ToTable("Bolumler");
            entity.HasKey(x => x.BolumId);

            entity.Property(x => x.BolumAdi)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(x => new { x.KursId, x.SiraNo }).IsUnique();
            entity.HasIndex(x => new { x.KursId, x.BolumAdi }).IsUnique();

            entity.HasOne(x => x.Kurs)
                .WithMany(x => x.Bolumler)
                .HasForeignKey(x => x.KursId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Dersler          
        modelBuilder.Entity<Ders>(entity =>
        {
            entity.ToTable("Dersler");
            entity.HasKey(x => x.DersId);

            entity.Property(x => x.DersAdi)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.VideoUrl)
                .IsRequired();

            entity.Property(x => x.AktifMi)
                .HasDefaultValue(true);

            entity.Property(x => x.SistemDersiMi)
                .HasDefaultValue(false);

            entity.Property(x => x.OlusturmaTarihi)
                .HasDefaultValueSql("SYSDATETIME()");

            entity.HasIndex(x => new { x.KursId, x.SiraNo })
                .IsUnique();

            entity.HasOne(x => x.Kurs)
                .WithMany(x => x.Dersler)
                .HasForeignKey(x => x.KursId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Bolum)
                .WithMany(x => x.Dersler)
                .HasForeignKey(x => x.BolumId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // MateryalTipleri
        modelBuilder.Entity<MateryalTipi>(entity =>
        {
            entity.ToTable("MateryalTipleri");
            entity.HasKey(x => x.MateryalTipId);

            entity.Property(x => x.MateryalTipAdi)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(x => x.MateryalTipAdi).IsUnique();
        });

        // DersMateryalleri
        modelBuilder.Entity<DersMateryali>(entity =>
        {
            entity.ToTable("DersMateryalleri");
            entity.HasKey(x => x.MateryalId);

            entity.Property(x => x.Baslik).HasMaxLength(200).IsRequired();
            entity.Property(x => x.MateryalUrl).IsRequired();
            entity.Property(x => x.YuklenmeTarihi).HasDefaultValueSql("SYSDATETIME()");

            entity.HasOne(x => x.Ders)
                .WithMany(x => x.DersMateryalleri)
                .HasForeignKey(x => x.DersId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.MateryalTipi)
                .WithMany()
                .HasForeignKey(x => x.MateryalTipId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // KursKayitlari
        modelBuilder.Entity<KursKaydi>(entity =>
        {
            entity.ToTable("KursKayitlari");
            entity.HasKey(x => x.KursKayitId);

            entity.Property(x => x.KayitTarihi).HasDefaultValueSql("SYSDATETIME()");

            entity.HasOne(x => x.Kullanici)
                .WithMany(x => x.KursKayitlari)
                .HasForeignKey(x => x.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Kurs)
                .WithMany()
                .HasForeignKey(x => x.KursId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // DersIlerlemeleri
        modelBuilder.Entity<DersIlerlemesi>(entity =>
        {
            entity.ToTable("DersIlerlemeleri");
            entity.HasKey(x => x.DersIlerlemeId);

            entity.HasIndex(x => new { x.KursKayitId, x.DersId }).IsUnique();

            entity.HasOne(x => x.KursKaydi)
                .WithMany(x => x.DersIlerlemeleri)
                .HasForeignKey(x => x.KursKayitId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Ders)
                .WithMany()
                .HasForeignKey(x => x.DersId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Sinavlar
        modelBuilder.Entity<Sinav>(entity =>
        {
            entity.ToTable("Sinavlar", t =>
            {
                t.HasCheckConstraint("CK_Sinavlar_GecmeNotu", "[GecmeNotu] BETWEEN 1 AND 100");
                t.HasCheckConstraint("CK_Sinavlar_SureDakika", "[SureDakika] > 0");
                t.HasCheckConstraint("CK_Sinavlar_SoruSayisi", "[SoruSayisi] > 0");
            });

            entity.HasKey(x => x.SinavId);

            entity.Property(x => x.SinavAdi)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.GecmeNotu)
                .IsRequired();

            entity.Property(x => x.SureDakika)
                .IsRequired();

            entity.Property(x => x.SoruSayisi)
                .IsRequired();

            entity.Property(x => x.OlusturmaTarihi)
                .HasDefaultValueSql("SYSDATETIME()");

            // 1 Kurs = 1 Sınav
            entity.HasIndex(x => x.KursId)
                .IsUnique();

            entity.HasOne(x => x.Kurs)
                .WithOne(x => x.Sinav)
                .HasForeignKey<Sinav>(x => x.KursId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Sorular
        modelBuilder.Entity<Soru>(entity =>
        {
            entity.ToTable("Sorular");
            entity.HasKey(x => x.SoruId);

            entity.Property(x => x.SoruMetni).IsRequired();

            entity.Property(x => x.AktifMi)
                .HasDefaultValue(true);

            entity.HasOne(x => x.Sinav)
                .WithMany(x => x.Sorular)
                .HasForeignKey(x => x.SinavId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // SoruDersleri
        modelBuilder.Entity<SoruDersi>(entity =>
        {
            entity.ToTable("SoruDersleri");
            entity.HasKey(x => x.SoruDersId);

            entity.HasIndex(x => new { x.SoruId, x.DersId }).IsUnique();

            entity.HasOne(x => x.Soru)
                .WithMany(x => x.SoruDersleri)
                .HasForeignKey(x => x.SoruId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Ders)
                .WithMany()
                .HasForeignKey(x => x.DersId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // SoruSecenekleri
        modelBuilder.Entity<SoruSecenegi>(entity =>
        {
            entity.ToTable("SoruSecenekleri");
            entity.HasKey(x => x.SecenekId);

            entity.Property(x => x.SecenekMetni).IsRequired();
            entity.Property(x => x.AktifMi).HasDefaultValue(true);

            entity.HasIndex(x => x.SoruId)
                .IsUnique()
                .HasFilter("[DogruMu] = 1 AND [AktifMi] = 1")
                .HasDatabaseName("UX_SoruSecenekleri_TekDogru");

            entity.HasOne(x => x.Soru)
                .WithMany(x => x.SoruSecenekleri)
                .HasForeignKey(x => x.SoruId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // SinavKatilimlari
        modelBuilder.Entity<SinavKatilimi>(entity =>
        {
            entity.ToTable("SinavKatilimlari");
            entity.HasKey(x => x.SinavKatilimId);

            entity.Property(x => x.BaslamaTarihi).HasDefaultValueSql("SYSDATETIME()");

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_SinavKatilimlari_AlinanPuan", "[AlinanPuan] IS NULL OR ([AlinanPuan] BETWEEN 0 AND 100)");
            });

            entity.HasOne(x => x.KursKaydi)
                .WithMany()
                .HasForeignKey(x => x.KursKayitId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Sinav)
                .WithMany(x => x.SinavKatilimlari)
                .HasForeignKey(x => x.SinavId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // OgrenciCevaplari
        modelBuilder.Entity<OgrenciCevabi>(entity =>
        {
            entity.ToTable("OgrenciCevaplari");
            entity.HasKey(x => x.OgrenciCevapId);

            entity.Property(x => x.VerilmeTarihi).HasDefaultValueSql("SYSDATETIME()");

            entity.HasIndex(x => new { x.SinavKatilimId, x.SoruId }).IsUnique();

            entity.HasOne(x => x.SinavKatilimi)
                .WithMany(x => x.OgrenciCevaplari)
                .HasForeignKey(x => x.SinavKatilimId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Soru)
                .WithMany()
                .HasForeignKey(x => x.SoruId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.SoruSecenegi)
                .WithMany()
                .HasForeignKey(x => x.SecenekId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // KursDegerlendirmeleri
        modelBuilder.Entity<KursDegerlendirmesi>(entity =>
        {
            entity.ToTable("KursDegerlendirmeleri");
            entity.HasKey(x => x.DegerlendirmeId);

            entity.Property(x => x.DegerlendirmeTarihi).HasDefaultValueSql("SYSDATETIME()");

            entity.HasIndex(x => new { x.KullaniciId, x.KursId }).IsUnique();

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_KursDegerlendirmeleri_Puan", "[Puan] BETWEEN 1 AND 5");
            });

            entity.HasOne(x => x.Kullanici)
                .WithMany()
                .HasForeignKey(x => x.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Kurs)
                .WithMany(x => x.KursDegerlendirmeleri)
                .HasForeignKey(x => x.KursId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Favoriler
        modelBuilder.Entity<Favori>(entity =>
        {
            entity.ToTable("Favoriler");
            entity.HasKey(x => x.FavoriId);

            entity.Property(x => x.EklenmeTarihi).HasDefaultValueSql("SYSDATETIME()");

            entity.HasIndex(x => new { x.KullaniciId, x.KursId }).IsUnique();

            entity.HasOne(x => x.Kullanici)
                .WithMany()
                .HasForeignKey(x => x.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Kurs)
                .WithMany()
                .HasForeignKey(x => x.KursId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // OneriTipleri
        modelBuilder.Entity<OneriTipi>(entity =>
        {
            entity.ToTable("OneriTipleri");
            entity.HasKey(x => x.OneriTipId);

            entity.Property(x => x.OneriTipAdi).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.OneriTipAdi).IsUnique();
        });

        // Oneriler
        modelBuilder.Entity<Oneri>(entity =>
        {
            entity.ToTable("Oneriler");
            entity.HasKey(x => x.OneriId);

            entity.Property(x => x.OneriMetni).IsRequired();
            entity.Property(x => x.OlusturmaTarihi).HasDefaultValueSql("SYSDATETIME()");

            entity.HasOne(x => x.Kullanici)
                .WithMany()
                .HasForeignKey(x => x.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.OneriTipi)
                .WithMany()
                .HasForeignKey(x => x.OneriTipId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Kurs)
                .WithMany()
                .HasForeignKey(x => x.KursId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // BildirimTipleri
        modelBuilder.Entity<BildirimTipi>(entity =>
        {
            entity.ToTable("BildirimTipleri");
            entity.HasKey(x => x.BildirimTipId);

            entity.Property(x => x.BildirimTipAdi).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.BildirimTipAdi).IsUnique();
        });

        // Bildirimler
        modelBuilder.Entity<Bildirim>(entity =>
        {
            entity.ToTable("Bildirimler");
            entity.HasKey(x => x.BildirimId);

            entity.Property(x => x.Baslik).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Mesaj).IsRequired();
            entity.Property(x => x.OlusturmaTarihi).HasDefaultValueSql("SYSDATETIME()");
            entity.Property(x => x.OkunduMu).HasDefaultValue(false);

            entity.HasOne(x => x.Kullanici)
                .WithMany()
                .HasForeignKey(x => x.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.BildirimTipi)
                .WithMany()
                .HasForeignKey(x => x.BildirimTipId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Sertifikalar
        modelBuilder.Entity<Sertifika>(entity =>
        {
            entity.ToTable("Sertifikalar");
            entity.HasKey(x => x.SertifikaId);

            entity.Property(x => x.SertifikaKodu).HasMaxLength(100).IsRequired();
            entity.Property(x => x.VerilmeTarihi).HasDefaultValueSql("SYSDATETIME()");

            entity.HasIndex(x => x.SertifikaKodu).IsUnique();

            entity.HasOne(x => x.Kullanici)
                .WithMany()
                .HasForeignKey(x => x.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Kurs)
                .WithMany()
                .HasForeignKey(x => x.KursId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // EgitmenOnaylari
        modelBuilder.Entity<EgitmenOnayi>(entity =>
        {
            entity.ToTable("EgitmenOnaylari");
            entity.HasKey(x => x.EgitmenOnayId);

            entity.Property(x => x.IslemTarihi).HasDefaultValueSql("SYSDATETIME()");

            entity.HasOne(x => x.Kullanici)
                .WithMany()
                .HasForeignKey(x => x.KullaniciId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Admin)
                .WithMany()
                .HasForeignKey(x => x.AdminId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Durum)
                .WithMany()
                .HasForeignKey(x => x.DurumId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // KursOnaylari
        modelBuilder.Entity<KursOnayi>(entity =>
        {
            entity.ToTable("KursOnaylari");
            entity.HasKey(x => x.KursOnayId);

            entity.Property(x => x.IslemTarihi).HasDefaultValueSql("SYSDATETIME()");

            entity.HasOne(x => x.Kurs)
                .WithMany()
                .HasForeignKey(x => x.KursId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Admin)
                .WithMany()
                .HasForeignKey(x => x.AdminId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Durum)
                .WithMany()
                .HasForeignKey(x => x.DurumId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // IslemTipleri
        modelBuilder.Entity<IslemTipi>(entity =>
        {
            entity.ToTable("IslemTipleri");
            entity.HasKey(x => x.IslemTipId);

            entity.Property(x => x.IslemTipAdi).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.IslemTipAdi).IsUnique();
        });

        // AdminLog
        modelBuilder.Entity<AdminLog>(entity =>
        {
            entity.ToTable("AdminLog");
            entity.HasKey(x => x.AdminLogId);

            entity.Property(x => x.IpAdresi)
                .HasMaxLength(45);

            entity.Property(x => x.IslemTarihi).HasDefaultValueSql("SYSDATETIME()");

            entity.HasOne(x => x.Admin)
                .WithMany()
                .HasForeignKey(x => x.AdminId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.IslemTipi)
                .WithMany()
                .HasForeignKey(x => x.IslemTipId)
                .OnDelete(DeleteBehavior.Restrict);
        });



        // Seed Data - Roller
        modelBuilder.Entity<Rol>().HasData(
            new Rol { RolId = 1, RolAdi = "Admin" },
            new Rol { RolId = 2, RolAdi = "Eğitmen" },
            new Rol { RolId = 3, RolAdi = "Öğrenci" }
        );

        // Seed Data - Durumlar
        modelBuilder.Entity<Durum>().HasData(
            new Durum { DurumId = 1, DurumAdi = "Aktif" },
            new Durum { DurumId = 2, DurumAdi = "Pasif" },
            new Durum { DurumId = 3, DurumAdi = "Taslak" },
            new Durum { DurumId = 4, DurumAdi = "Onay Bekliyor" },
            new Durum { DurumId = 5, DurumAdi = "Yayında" },
            new Durum { DurumId = 6, DurumAdi = "Reddedildi" },
            new Durum { DurumId = 7, DurumAdi = "Düzeltme İsteniyor" },
            new Durum { DurumId = 8, DurumAdi = "Onaylandı" }
        );

        modelBuilder.Entity<MateryalTipi>().HasData(
            new MateryalTipi { MateryalTipId = 1, MateryalTipAdi = "Doküman" },
            new MateryalTipi { MateryalTipId = 2, MateryalTipAdi = "Görsel" },
            new MateryalTipi { MateryalTipId = 3, MateryalTipAdi = "Ses" },
            new MateryalTipi { MateryalTipId = 4, MateryalTipAdi = "Video" },
            new MateryalTipi { MateryalTipId = 5, MateryalTipAdi = "Kod" }
        );

        modelBuilder.Entity<BildirimTipi>().HasData(
            new BildirimTipi { BildirimTipId = 1, BildirimTipAdi = "Bilgilendirme" },
            new BildirimTipi { BildirimTipId = 2, BildirimTipAdi = "Uyarı" }
        );

        modelBuilder.Entity<OneriTipi>().HasData(
            new OneriTipi { OneriTipId = 1, OneriTipAdi = "Eğitmen Kurs Analizi" },
            new OneriTipi { OneriTipId = 2, OneriTipAdi = "Öğrenci Çalışma Önerisi" }
        );

        modelBuilder.Entity<IslemTipi>().HasData(
            new IslemTipi { IslemTipId = 1, IslemTipAdi = "Kullanıcı İşlemleri" },
            new IslemTipi { IslemTipId = 2, IslemTipAdi = "Kurs Onayları" },
            new IslemTipi { IslemTipId = 3, IslemTipAdi = "Eğitmen Başvuruları" },
            new IslemTipi { IslemTipId = 4, IslemTipAdi = "Kurs İşlemleri" },
            new IslemTipi { IslemTipId = 5, IslemTipAdi = "Sistem / Kullanıcı" }
        );

    }


}
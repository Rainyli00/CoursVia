using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoursVia.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BildirimTipleri",
                columns: table => new
                {
                    BildirimTipId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BildirimTipAdi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BildirimTipleri", x => x.BildirimTipId);
                });

            migrationBuilder.CreateTable(
                name: "Durumlar",
                columns: table => new
                {
                    DurumId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DurumAdi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Durumlar", x => x.DurumId);
                });

            migrationBuilder.CreateTable(
                name: "IslemTipleri",
                columns: table => new
                {
                    IslemTipId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IslemTipAdi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IslemTipleri", x => x.IslemTipId);
                });

            migrationBuilder.CreateTable(
                name: "Kategoriler",
                columns: table => new
                {
                    KategoriId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KategoriAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kategoriler", x => x.KategoriId);
                });

            migrationBuilder.CreateTable(
                name: "MateryalTipleri",
                columns: table => new
                {
                    MateryalTipId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MateryalTipAdi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MateryalTipleri", x => x.MateryalTipId);
                });

            migrationBuilder.CreateTable(
                name: "OneriTipleri",
                columns: table => new
                {
                    OneriTipId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OneriTipAdi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneriTipleri", x => x.OneriTipId);
                });

            migrationBuilder.CreateTable(
                name: "Roller",
                columns: table => new
                {
                    RolId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RolAdi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roller", x => x.RolId);
                });

            migrationBuilder.CreateTable(
                name: "Kullanicilar",
                columns: table => new
                {
                    KullaniciId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RolId = table.Column<int>(type: "int", nullable: false),
                    DurumId = table.Column<int>(type: "int", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Soyad = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Eposta = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SifreHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Telefon = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ProfilFotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KayitTarihi = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    SonGirisTarihi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kullanicilar", x => x.KullaniciId);
                    table.ForeignKey(
                        name: "FK_Kullanicilar_Durumlar_DurumId",
                        column: x => x.DurumId,
                        principalTable: "Durumlar",
                        principalColumn: "DurumId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Kullanicilar_Roller_RolId",
                        column: x => x.RolId,
                        principalTable: "Roller",
                        principalColumn: "RolId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdminLog",
                columns: table => new
                {
                    AdminLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminId = table.Column<int>(type: "int", nullable: false),
                    IslemTipId = table.Column<int>(type: "int", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IslemTarihi = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminLog", x => x.AdminLogId);
                    table.ForeignKey(
                        name: "FK_AdminLog_IslemTipleri_IslemTipId",
                        column: x => x.IslemTipId,
                        principalTable: "IslemTipleri",
                        principalColumn: "IslemTipId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdminLog_Kullanicilar_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Bildirimler",
                columns: table => new
                {
                    BildirimId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    BildirimTipId = table.Column<int>(type: "int", nullable: false),
                    Baslik = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Mesaj = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    OkunduMu = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bildirimler", x => x.BildirimId);
                    table.ForeignKey(
                        name: "FK_Bildirimler_BildirimTipleri_BildirimTipId",
                        column: x => x.BildirimTipId,
                        principalTable: "BildirimTipleri",
                        principalColumn: "BildirimTipId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bildirimler_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EgitmenOnaylari",
                columns: table => new
                {
                    EgitmenOnayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    AdminId = table.Column<int>(type: "int", nullable: false),
                    DurumId = table.Column<int>(type: "int", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IslemTarihi = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EgitmenOnaylari", x => x.EgitmenOnayId);
                    table.ForeignKey(
                        name: "FK_EgitmenOnaylari_Durumlar_DurumId",
                        column: x => x.DurumId,
                        principalTable: "Durumlar",
                        principalColumn: "DurumId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EgitmenOnaylari_Kullanicilar_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EgitmenOnaylari_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Kurslar",
                columns: table => new
                {
                    KursId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KategoriId = table.Column<int>(type: "int", nullable: false),
                    EgitmenId = table.Column<int>(type: "int", nullable: false),
                    DurumId = table.Column<int>(type: "int", nullable: false),
                    KursAdi = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KapakGorselUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kurslar", x => x.KursId);
                    table.ForeignKey(
                        name: "FK_Kurslar_Durumlar_DurumId",
                        column: x => x.DurumId,
                        principalTable: "Durumlar",
                        principalColumn: "DurumId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Kurslar_Kategoriler_KategoriId",
                        column: x => x.KategoriId,
                        principalTable: "Kategoriler",
                        principalColumn: "KategoriId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Kurslar_Kullanicilar_EgitmenId",
                        column: x => x.EgitmenId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Favoriler",
                columns: table => new
                {
                    FavoriId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    KursId = table.Column<int>(type: "int", nullable: false),
                    EklenmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favoriler", x => x.FavoriId);
                    table.ForeignKey(
                        name: "FK_Favoriler_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Favoriler_Kurslar_KursId",
                        column: x => x.KursId,
                        principalTable: "Kurslar",
                        principalColumn: "KursId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Konular",
                columns: table => new
                {
                    KonuId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KursId = table.Column<int>(type: "int", nullable: false),
                    KonuAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SiraNo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Konular", x => x.KonuId);
                    table.ForeignKey(
                        name: "FK_Konular_Kurslar_KursId",
                        column: x => x.KursId,
                        principalTable: "Kurslar",
                        principalColumn: "KursId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KursDegerlendirmeleri",
                columns: table => new
                {
                    DegerlendirmeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    KursId = table.Column<int>(type: "int", nullable: false),
                    Puan = table.Column<byte>(type: "tinyint", nullable: false),
                    YorumMetni = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DegerlendirmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KursDegerlendirmeleri", x => x.DegerlendirmeId);
                    table.CheckConstraint("CK_KursDegerlendirmeleri_Puan", "[Puan] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_KursDegerlendirmeleri_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KursDegerlendirmeleri_Kurslar_KursId",
                        column: x => x.KursId,
                        principalTable: "Kurslar",
                        principalColumn: "KursId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KursKayitlari",
                columns: table => new
                {
                    KursKayitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    KursId = table.Column<int>(type: "int", nullable: false),
                    KayitTarihi = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    TamamlandiMi = table.Column<bool>(type: "bit", nullable: false),
                    TamamlanmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KursKayitlari", x => x.KursKayitId);
                    table.ForeignKey(
                        name: "FK_KursKayitlari_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KursKayitlari_Kurslar_KursId",
                        column: x => x.KursId,
                        principalTable: "Kurslar",
                        principalColumn: "KursId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KursOnaylari",
                columns: table => new
                {
                    KursOnayId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KursId = table.Column<int>(type: "int", nullable: false),
                    AdminId = table.Column<int>(type: "int", nullable: false),
                    DurumId = table.Column<int>(type: "int", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IslemTarihi = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KursOnaylari", x => x.KursOnayId);
                    table.ForeignKey(
                        name: "FK_KursOnaylari_Durumlar_DurumId",
                        column: x => x.DurumId,
                        principalTable: "Durumlar",
                        principalColumn: "DurumId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KursOnaylari_Kullanicilar_AdminId",
                        column: x => x.AdminId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KursOnaylari_Kurslar_KursId",
                        column: x => x.KursId,
                        principalTable: "Kurslar",
                        principalColumn: "KursId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Oneriler",
                columns: table => new
                {
                    OneriId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    OneriTipId = table.Column<int>(type: "int", nullable: false),
                    KursId = table.Column<int>(type: "int", nullable: true),
                    OneriMetni = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    GorulduMu = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Oneriler", x => x.OneriId);
                    table.ForeignKey(
                        name: "FK_Oneriler_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Oneriler_Kurslar_KursId",
                        column: x => x.KursId,
                        principalTable: "Kurslar",
                        principalColumn: "KursId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Oneriler_OneriTipleri_OneriTipId",
                        column: x => x.OneriTipId,
                        principalTable: "OneriTipleri",
                        principalColumn: "OneriTipId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sertifikalar",
                columns: table => new
                {
                    SertifikaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    KursId = table.Column<int>(type: "int", nullable: false),
                    SertifikaKodu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VerilmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sertifikalar", x => x.SertifikaId);
                    table.ForeignKey(
                        name: "FK_Sertifikalar_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sertifikalar_Kurslar_KursId",
                        column: x => x.KursId,
                        principalTable: "Kurslar",
                        principalColumn: "KursId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sinavlar",
                columns: table => new
                {
                    SinavId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KursId = table.Column<int>(type: "int", nullable: false),
                    SinavAdi = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GecmeNotu = table.Column<int>(type: "int", nullable: false),
                    SureDakika = table.Column<int>(type: "int", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sinavlar", x => x.SinavId);
                    table.CheckConstraint("CK_Sinavlar_GecmeNotu", "[GecmeNotu] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_Sinavlar_SureDakika", "[SureDakika] > 0");
                    table.ForeignKey(
                        name: "FK_Sinavlar_Kurslar_KursId",
                        column: x => x.KursId,
                        principalTable: "Kurslar",
                        principalColumn: "KursId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Dersler",
                columns: table => new
                {
                    DersId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KursId = table.Column<int>(type: "int", nullable: false),
                    KonuId = table.Column<int>(type: "int", nullable: false),
                    DersAdi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VideoUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SiraNo = table.Column<int>(type: "int", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dersler", x => x.DersId);
                    table.ForeignKey(
                        name: "FK_Dersler_Konular_KonuId",
                        column: x => x.KonuId,
                        principalTable: "Konular",
                        principalColumn: "KonuId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dersler_Kurslar_KursId",
                        column: x => x.KursId,
                        principalTable: "Kurslar",
                        principalColumn: "KursId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SinavKatilimlari",
                columns: table => new
                {
                    SinavKatilimId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KursKayitId = table.Column<int>(type: "int", nullable: false),
                    SinavId = table.Column<int>(type: "int", nullable: false),
                    BaslamaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    BitisTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AlinanPuan = table.Column<int>(type: "int", nullable: true),
                    GectiMi = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SinavKatilimlari", x => x.SinavKatilimId);
                    table.CheckConstraint("CK_SinavKatilimlari_AlinanPuan", "[AlinanPuan] IS NULL OR ([AlinanPuan] BETWEEN 0 AND 100)");
                    table.ForeignKey(
                        name: "FK_SinavKatilimlari_KursKayitlari_KursKayitId",
                        column: x => x.KursKayitId,
                        principalTable: "KursKayitlari",
                        principalColumn: "KursKayitId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SinavKatilimlari_Sinavlar_SinavId",
                        column: x => x.SinavId,
                        principalTable: "Sinavlar",
                        principalColumn: "SinavId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sorular",
                columns: table => new
                {
                    SoruId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SinavId = table.Column<int>(type: "int", nullable: false),
                    SoruMetni = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sorular", x => x.SoruId);
                    table.ForeignKey(
                        name: "FK_Sorular_Sinavlar_SinavId",
                        column: x => x.SinavId,
                        principalTable: "Sinavlar",
                        principalColumn: "SinavId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DersIlerlemeleri",
                columns: table => new
                {
                    DersIlerlemeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KursKayitId = table.Column<int>(type: "int", nullable: false),
                    DersId = table.Column<int>(type: "int", nullable: false),
                    TamamlandiMi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DersIlerlemeleri", x => x.DersIlerlemeId);
                    table.ForeignKey(
                        name: "FK_DersIlerlemeleri_Dersler_DersId",
                        column: x => x.DersId,
                        principalTable: "Dersler",
                        principalColumn: "DersId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DersIlerlemeleri_KursKayitlari_KursKayitId",
                        column: x => x.KursKayitId,
                        principalTable: "KursKayitlari",
                        principalColumn: "KursKayitId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DersMateryalleri",
                columns: table => new
                {
                    MateryalId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DersId = table.Column<int>(type: "int", nullable: false),
                    MateryalTipId = table.Column<int>(type: "int", nullable: false),
                    Baslik = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MateryalUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    YuklenmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DersMateryalleri", x => x.MateryalId);
                    table.ForeignKey(
                        name: "FK_DersMateryalleri_Dersler_DersId",
                        column: x => x.DersId,
                        principalTable: "Dersler",
                        principalColumn: "DersId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DersMateryalleri_MateryalTipleri_MateryalTipId",
                        column: x => x.MateryalTipId,
                        principalTable: "MateryalTipleri",
                        principalColumn: "MateryalTipId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SoruDersleri",
                columns: table => new
                {
                    SoruDersId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoruId = table.Column<int>(type: "int", nullable: false),
                    DersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoruDersleri", x => x.SoruDersId);
                    table.ForeignKey(
                        name: "FK_SoruDersleri_Dersler_DersId",
                        column: x => x.DersId,
                        principalTable: "Dersler",
                        principalColumn: "DersId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SoruDersleri_Sorular_SoruId",
                        column: x => x.SoruId,
                        principalTable: "Sorular",
                        principalColumn: "SoruId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SoruSecenekleri",
                columns: table => new
                {
                    SecenekId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoruId = table.Column<int>(type: "int", nullable: false),
                    SecenekMetni = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DogruMu = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoruSecenekleri", x => x.SecenekId);
                    table.ForeignKey(
                        name: "FK_SoruSecenekleri_Sorular_SoruId",
                        column: x => x.SoruId,
                        principalTable: "Sorular",
                        principalColumn: "SoruId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OgrenciCevaplari",
                columns: table => new
                {
                    OgrenciCevapId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SinavKatilimId = table.Column<int>(type: "int", nullable: false),
                    SoruId = table.Column<int>(type: "int", nullable: false),
                    SecenekId = table.Column<int>(type: "int", nullable: true),
                    DogruMu = table.Column<bool>(type: "bit", nullable: false),
                    VerilmeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OgrenciCevaplari", x => x.OgrenciCevapId);
                    table.ForeignKey(
                        name: "FK_OgrenciCevaplari_SinavKatilimlari_SinavKatilimId",
                        column: x => x.SinavKatilimId,
                        principalTable: "SinavKatilimlari",
                        principalColumn: "SinavKatilimId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OgrenciCevaplari_SoruSecenekleri_SecenekId",
                        column: x => x.SecenekId,
                        principalTable: "SoruSecenekleri",
                        principalColumn: "SecenekId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OgrenciCevaplari_Sorular_SoruId",
                        column: x => x.SoruId,
                        principalTable: "Sorular",
                        principalColumn: "SoruId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminLog_AdminId",
                table: "AdminLog",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminLog_IslemTipId",
                table: "AdminLog",
                column: "IslemTipId");

            migrationBuilder.CreateIndex(
                name: "IX_Bildirimler_BildirimTipId",
                table: "Bildirimler",
                column: "BildirimTipId");

            migrationBuilder.CreateIndex(
                name: "IX_Bildirimler_KullaniciId",
                table: "Bildirimler",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_BildirimTipleri_BildirimTipAdi",
                table: "BildirimTipleri",
                column: "BildirimTipAdi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DersIlerlemeleri_DersId",
                table: "DersIlerlemeleri",
                column: "DersId");

            migrationBuilder.CreateIndex(
                name: "IX_DersIlerlemeleri_KursKayitId_DersId",
                table: "DersIlerlemeleri",
                columns: new[] { "KursKayitId", "DersId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dersler_KonuId",
                table: "Dersler",
                column: "KonuId");

            migrationBuilder.CreateIndex(
                name: "IX_Dersler_KursId_SiraNo",
                table: "Dersler",
                columns: new[] { "KursId", "SiraNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DersMateryalleri_DersId",
                table: "DersMateryalleri",
                column: "DersId");

            migrationBuilder.CreateIndex(
                name: "IX_DersMateryalleri_MateryalTipId",
                table: "DersMateryalleri",
                column: "MateryalTipId");

            migrationBuilder.CreateIndex(
                name: "IX_Durumlar_DurumAdi",
                table: "Durumlar",
                column: "DurumAdi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EgitmenOnaylari_AdminId",
                table: "EgitmenOnaylari",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_EgitmenOnaylari_DurumId",
                table: "EgitmenOnaylari",
                column: "DurumId");

            migrationBuilder.CreateIndex(
                name: "IX_EgitmenOnaylari_KullaniciId",
                table: "EgitmenOnaylari",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Favoriler_KullaniciId_KursId",
                table: "Favoriler",
                columns: new[] { "KullaniciId", "KursId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Favoriler_KursId",
                table: "Favoriler",
                column: "KursId");

            migrationBuilder.CreateIndex(
                name: "IX_IslemTipleri_IslemTipAdi",
                table: "IslemTipleri",
                column: "IslemTipAdi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kategoriler_KategoriAdi",
                table: "Kategoriler",
                column: "KategoriAdi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Konular_KursId_KonuAdi",
                table: "Konular",
                columns: new[] { "KursId", "KonuAdi" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Konular_KursId_SiraNo",
                table: "Konular",
                columns: new[] { "KursId", "SiraNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_DurumId",
                table: "Kullanicilar",
                column: "DurumId");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_Eposta",
                table: "Kullanicilar",
                column: "Eposta",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_RolId",
                table: "Kullanicilar",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_KursDegerlendirmeleri_KullaniciId_KursId",
                table: "KursDegerlendirmeleri",
                columns: new[] { "KullaniciId", "KursId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KursDegerlendirmeleri_KursId",
                table: "KursDegerlendirmeleri",
                column: "KursId");

            migrationBuilder.CreateIndex(
                name: "IX_KursKayitlari_KullaniciId",
                table: "KursKayitlari",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_KursKayitlari_KursId",
                table: "KursKayitlari",
                column: "KursId");

            migrationBuilder.CreateIndex(
                name: "IX_Kurslar_DurumId",
                table: "Kurslar",
                column: "DurumId");

            migrationBuilder.CreateIndex(
                name: "IX_Kurslar_EgitmenId",
                table: "Kurslar",
                column: "EgitmenId");

            migrationBuilder.CreateIndex(
                name: "IX_Kurslar_KategoriId",
                table: "Kurslar",
                column: "KategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_KursOnaylari_AdminId",
                table: "KursOnaylari",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_KursOnaylari_DurumId",
                table: "KursOnaylari",
                column: "DurumId");

            migrationBuilder.CreateIndex(
                name: "IX_KursOnaylari_KursId",
                table: "KursOnaylari",
                column: "KursId");

            migrationBuilder.CreateIndex(
                name: "IX_MateryalTipleri_MateryalTipAdi",
                table: "MateryalTipleri",
                column: "MateryalTipAdi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OgrenciCevaplari_SecenekId",
                table: "OgrenciCevaplari",
                column: "SecenekId");

            migrationBuilder.CreateIndex(
                name: "IX_OgrenciCevaplari_SinavKatilimId_SoruId",
                table: "OgrenciCevaplari",
                columns: new[] { "SinavKatilimId", "SoruId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OgrenciCevaplari_SoruId",
                table: "OgrenciCevaplari",
                column: "SoruId");

            migrationBuilder.CreateIndex(
                name: "IX_Oneriler_KullaniciId",
                table: "Oneriler",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Oneriler_KursId",
                table: "Oneriler",
                column: "KursId");

            migrationBuilder.CreateIndex(
                name: "IX_Oneriler_OneriTipId",
                table: "Oneriler",
                column: "OneriTipId");

            migrationBuilder.CreateIndex(
                name: "IX_OneriTipleri_OneriTipAdi",
                table: "OneriTipleri",
                column: "OneriTipAdi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roller_RolAdi",
                table: "Roller",
                column: "RolAdi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sertifikalar_KullaniciId",
                table: "Sertifikalar",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Sertifikalar_KursId",
                table: "Sertifikalar",
                column: "KursId");

            migrationBuilder.CreateIndex(
                name: "IX_Sertifikalar_SertifikaKodu",
                table: "Sertifikalar",
                column: "SertifikaKodu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SinavKatilimlari_KursKayitId",
                table: "SinavKatilimlari",
                column: "KursKayitId");

            migrationBuilder.CreateIndex(
                name: "IX_SinavKatilimlari_SinavId",
                table: "SinavKatilimlari",
                column: "SinavId");

            migrationBuilder.CreateIndex(
                name: "IX_Sinavlar_KursId",
                table: "Sinavlar",
                column: "KursId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoruDersleri_DersId",
                table: "SoruDersleri",
                column: "DersId");

            migrationBuilder.CreateIndex(
                name: "IX_SoruDersleri_SoruId_DersId",
                table: "SoruDersleri",
                columns: new[] { "SoruId", "DersId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sorular_SinavId",
                table: "Sorular",
                column: "SinavId");

            migrationBuilder.CreateIndex(
                name: "UX_SoruSecenekleri_TekDogru",
                table: "SoruSecenekleri",
                column: "SoruId",
                unique: true,
                filter: "[DogruMu] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminLog");

            migrationBuilder.DropTable(
                name: "Bildirimler");

            migrationBuilder.DropTable(
                name: "DersIlerlemeleri");

            migrationBuilder.DropTable(
                name: "DersMateryalleri");

            migrationBuilder.DropTable(
                name: "EgitmenOnaylari");

            migrationBuilder.DropTable(
                name: "Favoriler");

            migrationBuilder.DropTable(
                name: "KursDegerlendirmeleri");

            migrationBuilder.DropTable(
                name: "KursOnaylari");

            migrationBuilder.DropTable(
                name: "OgrenciCevaplari");

            migrationBuilder.DropTable(
                name: "Oneriler");

            migrationBuilder.DropTable(
                name: "Sertifikalar");

            migrationBuilder.DropTable(
                name: "SoruDersleri");

            migrationBuilder.DropTable(
                name: "IslemTipleri");

            migrationBuilder.DropTable(
                name: "BildirimTipleri");

            migrationBuilder.DropTable(
                name: "MateryalTipleri");

            migrationBuilder.DropTable(
                name: "SinavKatilimlari");

            migrationBuilder.DropTable(
                name: "SoruSecenekleri");

            migrationBuilder.DropTable(
                name: "OneriTipleri");

            migrationBuilder.DropTable(
                name: "Dersler");

            migrationBuilder.DropTable(
                name: "KursKayitlari");

            migrationBuilder.DropTable(
                name: "Sorular");

            migrationBuilder.DropTable(
                name: "Konular");

            migrationBuilder.DropTable(
                name: "Sinavlar");

            migrationBuilder.DropTable(
                name: "Kurslar");

            migrationBuilder.DropTable(
                name: "Kategoriler");

            migrationBuilder.DropTable(
                name: "Kullanicilar");

            migrationBuilder.DropTable(
                name: "Durumlar");

            migrationBuilder.DropTable(
                name: "Roller");
        }
    }
}

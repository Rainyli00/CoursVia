using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoursVia.Migrations
{
    /// <inheritdoc />
    public partial class AddEgitmenProfiliVeBranslari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EgitmenProfilleri",
                columns: table => new
                {
                    EgitmenProfilId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    UzmanlikAlani = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Biyografi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeneyimYili = table.Column<int>(type: "int", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EgitmenProfilleri", x => x.EgitmenProfilId);
                    table.CheckConstraint("CK_EgitmenProfilleri_DeneyimYili", "[DeneyimYili] IS NULL OR [DeneyimYili] >= 0");
                    table.ForeignKey(
                        name: "FK_EgitmenProfilleri_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EgitmenBranslari",
                columns: table => new
                {
                    EgitmenBransId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EgitmenProfilId = table.Column<int>(type: "int", nullable: false),
                    KategoriId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EgitmenBranslari", x => x.EgitmenBransId);
                    table.ForeignKey(
                        name: "FK_EgitmenBranslari_EgitmenProfilleri_EgitmenProfilId",
                        column: x => x.EgitmenProfilId,
                        principalTable: "EgitmenProfilleri",
                        principalColumn: "EgitmenProfilId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EgitmenBranslari_Kategoriler_KategoriId",
                        column: x => x.KategoriId,
                        principalTable: "Kategoriler",
                        principalColumn: "KategoriId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EgitmenBranslari_EgitmenProfilId_KategoriId",
                table: "EgitmenBranslari",
                columns: new[] { "EgitmenProfilId", "KategoriId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EgitmenBranslari_KategoriId",
                table: "EgitmenBranslari",
                column: "KategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_EgitmenProfilleri_KullaniciId",
                table: "EgitmenProfilleri",
                column: "KullaniciId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EgitmenBranslari");

            migrationBuilder.DropTable(
                name: "EgitmenProfilleri");
        }
    }
}

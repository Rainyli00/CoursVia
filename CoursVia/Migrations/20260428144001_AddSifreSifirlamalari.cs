using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoursVia.Migrations
{
    /// <inheritdoc />
    public partial class AddSifreSifirlamalari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SifreSifirlamalari",
                columns: table => new
                {
                    SifreSifirlamaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()"),
                    GecerlilikTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KullanildiMi = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SifreSifirlamalari", x => x.SifreSifirlamaId);
                    table.ForeignKey(
                        name: "FK_SifreSifirlamalari_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SifreSifirlamalari_KullaniciId_Kod",
                table: "SifreSifirlamalari",
                columns: new[] { "KullaniciId", "Kod" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SifreSifirlamalari");
        }
    }
}

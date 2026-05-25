using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoursVia.Migrations
{
    /// <inheritdoc />
    public partial class MobilOturumRefreshTokenSistemi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MobilOturumlari",
                columns: table => new
                {
                    MobilOturumId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RefreshTokenBitisTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MobilOturumlari", x => x.MobilOturumId);
                    table.ForeignKey(
                        name: "FK_MobilOturumlari_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MobilOturumlari_KullaniciId",
                table: "MobilOturumlari",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_MobilOturumlari_RefreshTokenHash",
                table: "MobilOturumlari",
                column: "RefreshTokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MobilOturumlari");
        }
    }
}

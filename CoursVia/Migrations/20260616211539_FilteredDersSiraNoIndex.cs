using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoursVia.Migrations
{
    /// <inheritdoc />
    public partial class FilteredDersSiraNoIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Dersler_KursId_SiraNo",
                table: "Dersler");

            migrationBuilder.CreateIndex(
                name: "IX_Dersler_KursId_SiraNo",
                table: "Dersler",
                columns: new[] { "KursId", "SiraNo" },
                unique: true,
                filter: "[AktifMi] = 1 AND [SistemDersiMi] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Dersler_KursId_SiraNo",
                table: "Dersler");

            migrationBuilder.CreateIndex(
                name: "IX_Dersler_KursId_SiraNo",
                table: "Dersler",
                columns: new[] { "KursId", "SiraNo" },
                unique: true);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CoursVia.Migrations
{
    /// <inheritdoc />
    public partial class SeedBildirimTipleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "BildirimTipleri",
                columns: new[] { "BildirimTipId", "BildirimTipAdi" },
                values: new object[,]
                {
                    { 1, "Bilgilendirme" },
                    { 2, "Uyarı" },
                    { 3, "Hata" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BildirimTipleri",
                keyColumn: "BildirimTipId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "BildirimTipleri",
                keyColumn: "BildirimTipId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "BildirimTipleri",
                keyColumn: "BildirimTipId",
                keyValue: 3);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CoursVia.Migrations
{
    /// <inheritdoc />
    public partial class SeedRolesAndStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Durumlar",
                columns: new[] { "DurumId", "DurumAdi" },
                values: new object[,]
                {
                    { 1, "Aktif" },
                    { 2, "Pasif" },
                    { 3, "Taslak" },
                    { 4, "Onay Bekliyor" },
                    { 5, "Onaylandı" },
                    { 6, "Reddedildi" },
                    { 7, "Düzeltme İsteniyor" }
                });

            migrationBuilder.InsertData(
                table: "Roller",
                columns: new[] { "RolId", "RolAdi" },
                values: new object[,]
                {
                    { 1, "Admin" },
                    { 2, "Eğitmen" },
                    { 3, "Öğrenci" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Durumlar",
                keyColumn: "DurumId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Durumlar",
                keyColumn: "DurumId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Durumlar",
                keyColumn: "DurumId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Durumlar",
                keyColumn: "DurumId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Durumlar",
                keyColumn: "DurumId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Durumlar",
                keyColumn: "DurumId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Durumlar",
                keyColumn: "DurumId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Roller",
                keyColumn: "RolId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Roller",
                keyColumn: "RolId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Roller",
                keyColumn: "RolId",
                keyValue: 3);
        }
    }
}

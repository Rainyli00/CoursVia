using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CoursVia.Migrations
{
    /// <inheritdoc />
    public partial class SeedDataGuncelleme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "IslemTipleri",
                columns: new[] { "IslemTipId", "IslemTipAdi" },
                values: new object[,]
                {
                    { 1, "Kullanıcı İşlemleri" },
                    { 2, "Kurs Onayları" },
                    { 3, "Eğitmen Başvuruları" },
                    { 4, "Kurs İşlemleri" },
                    { 5, "Sistem / Kullanıcı" }
                });

            migrationBuilder.InsertData(
                table: "OneriTipleri",
                columns: new[] { "OneriTipId", "OneriTipAdi" },
                values: new object[,]
                {
                    { 1, "Eğitmen Kurs Analizi" },
                    { 2, "Öğrenci Çalışma Önerisi" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "IslemTipleri",
                keyColumn: "IslemTipId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "IslemTipleri",
                keyColumn: "IslemTipId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "IslemTipleri",
                keyColumn: "IslemTipId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "IslemTipleri",
                keyColumn: "IslemTipId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "IslemTipleri",
                keyColumn: "IslemTipId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "OneriTipleri",
                keyColumn: "OneriTipId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "OneriTipleri",
                keyColumn: "OneriTipId",
                keyValue: 2);
        }
    }
}

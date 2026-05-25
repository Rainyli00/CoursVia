using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CoursVia.Migrations
{
    /// <inheritdoc />
    public partial class SeedMateryalTipleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MateryalTipleri",
                columns: new[] { "MateryalTipId", "MateryalTipAdi" },
                values: new object[,]
                {
                    { 1, "Doküman" },
                    { 2, "Görsel" },
                    { 3, "Ses" },
                    { 4, "Video" },
                    { 5, "Kod" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MateryalTipleri",
                keyColumn: "MateryalTipId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "MateryalTipleri",
                keyColumn: "MateryalTipId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "MateryalTipleri",
                keyColumn: "MateryalTipId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "MateryalTipleri",
                keyColumn: "MateryalTipId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "MateryalTipleri",
                keyColumn: "MateryalTipId",
                keyValue: 5);
        }
    }
}

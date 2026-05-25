using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoursVia.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHataBildirimTipi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mevcut "Hata" tipindeki bildirimleri "Uyarı" tipine (ID=2) taşı
            migrationBuilder.Sql(
                "UPDATE Bildirimler SET BildirimTipId = 2 WHERE BildirimTipId = 3");

            // Artık referans kalmadığından "Hata" tipini sil
            migrationBuilder.DeleteData(
                table: "BildirimTipleri",
                keyColumn: "BildirimTipId",
                keyValue: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "BildirimTipleri",
                columns: new[] { "BildirimTipId", "BildirimTipAdi" },
                values: new object[] { 3, "Hata" });
        }
    }
}

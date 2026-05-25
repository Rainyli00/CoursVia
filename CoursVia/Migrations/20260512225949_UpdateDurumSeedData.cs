using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoursVia.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDurumSeedData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Durumlar SET DurumAdi = N'Yayında' WHERE DurumId = 5;");
            
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM Durumlar WHERE DurumId = 8) 
                BEGIN 
                    SET IDENTITY_INSERT Durumlar ON; 
                    INSERT INTO Durumlar (DurumId, DurumAdi) VALUES (8, N'Onaylandı'); 
                    SET IDENTITY_INSERT Durumlar OFF; 
                END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Durumlar",
                keyColumn: "DurumId",
                keyValue: 8);

            migrationBuilder.UpdateData(
                table: "Durumlar",
                keyColumn: "DurumId",
                keyValue: 5,
                column: "DurumAdi",
                value: "Onaylandı");
        }
    }
}

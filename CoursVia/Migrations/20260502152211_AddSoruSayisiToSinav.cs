using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoursVia.Migrations
{
    /// <inheritdoc />
    public partial class AddSoruSayisiToSinav : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Sinavlar_GecmeNotu",
                table: "Sinavlar");

            migrationBuilder.AddColumn<int>(
                name: "SoruSayisi",
                table: "Sinavlar",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sinavlar_GecmeNotu",
                table: "Sinavlar",
                sql: "[GecmeNotu] BETWEEN 1 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sinavlar_SoruSayisi",
                table: "Sinavlar",
                sql: "[SoruSayisi] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Sinavlar_GecmeNotu",
                table: "Sinavlar");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Sinavlar_SoruSayisi",
                table: "Sinavlar");

            migrationBuilder.DropColumn(
                name: "SoruSayisi",
                table: "Sinavlar");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Sinavlar_GecmeNotu",
                table: "Sinavlar",
                sql: "[GecmeNotu] BETWEEN 0 AND 100");
        }
    }
}

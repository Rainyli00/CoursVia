using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoursVia.Migrations
{
    /// <inheritdoc />
    public partial class AddAktifMiToSoruSecenegi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_SoruSecenekleri_TekDogru",
                table: "SoruSecenekleri");

            migrationBuilder.AddColumn<bool>(
                name: "AktifMi",
                table: "SoruSecenekleri",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "UX_SoruSecenekleri_TekDogru",
                table: "SoruSecenekleri",
                column: "SoruId",
                unique: true,
                filter: "[DogruMu] = 1 AND [AktifMi] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_SoruSecenekleri_TekDogru",
                table: "SoruSecenekleri");

            migrationBuilder.DropColumn(
                name: "AktifMi",
                table: "SoruSecenekleri");

            migrationBuilder.CreateIndex(
                name: "UX_SoruSecenekleri_TekDogru",
                table: "SoruSecenekleri",
                column: "SoruId",
                unique: true,
                filter: "[DogruMu] = 1");
        }
    }
}

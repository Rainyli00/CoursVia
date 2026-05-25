using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoursVia.Migrations
{
    /// <inheritdoc />
    public partial class OneriGorulduMuKaldirildi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GorulduMu",
                table: "Oneriler");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "GorulduMu",
                table: "Oneriler",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}

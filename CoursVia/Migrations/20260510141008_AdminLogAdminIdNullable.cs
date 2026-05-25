using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoursVia.Migrations
{
    /// <inheritdoc />
    public partial class AdminLogAdminIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdminLog_Kullanicilar_AdminId",
                table: "AdminLog");

            migrationBuilder.AlterColumn<int>(
                name: "AdminId",
                table: "AdminLog",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_AdminLog_Kullanicilar_AdminId",
                table: "AdminLog",
                column: "AdminId",
                principalTable: "Kullanicilar",
                principalColumn: "KullaniciId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdminLog_Kullanicilar_AdminId",
                table: "AdminLog");

            migrationBuilder.AlterColumn<int>(
                name: "AdminId",
                table: "AdminLog",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AdminLog_Kullanicilar_AdminId",
                table: "AdminLog",
                column: "AdminId",
                principalTable: "Kullanicilar",
                principalColumn: "KullaniciId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

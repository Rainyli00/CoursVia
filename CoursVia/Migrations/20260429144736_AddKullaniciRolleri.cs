using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoursVia.Migrations
{
    /// <inheritdoc />
    public partial class AddKullaniciRolleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kullanicilar_Roller_RolId",
                table: "Kullanicilar");

            migrationBuilder.DropIndex(
                name: "IX_Kullanicilar_RolId",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "RolId",
                table: "Kullanicilar");

            migrationBuilder.AddColumn<int>(
                name: "DurumId",
                table: "EgitmenProfilleri",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "KullaniciRolleri",
                columns: table => new
                {
                    KullaniciRolId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KullaniciId = table.Column<int>(type: "int", nullable: false),
                    RolId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KullaniciRolleri", x => x.KullaniciRolId);
                    table.ForeignKey(
                        name: "FK_KullaniciRolleri_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "KullaniciId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KullaniciRolleri_Roller_RolId",
                        column: x => x.RolId,
                        principalTable: "Roller",
                        principalColumn: "RolId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EgitmenProfilleri_DurumId",
                table: "EgitmenProfilleri",
                column: "DurumId");

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciRolleri_KullaniciId_RolId",
                table: "KullaniciRolleri",
                columns: new[] { "KullaniciId", "RolId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciRolleri_RolId",
                table: "KullaniciRolleri",
                column: "RolId");

            migrationBuilder.AddForeignKey(
                name: "FK_EgitmenProfilleri_Durumlar_DurumId",
                table: "EgitmenProfilleri",
                column: "DurumId",
                principalTable: "Durumlar",
                principalColumn: "DurumId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EgitmenProfilleri_Durumlar_DurumId",
                table: "EgitmenProfilleri");

            migrationBuilder.DropTable(
                name: "KullaniciRolleri");

            migrationBuilder.DropIndex(
                name: "IX_EgitmenProfilleri_DurumId",
                table: "EgitmenProfilleri");

            migrationBuilder.DropColumn(
                name: "DurumId",
                table: "EgitmenProfilleri");

            migrationBuilder.AddColumn<int>(
                name: "RolId",
                table: "Kullanicilar",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_RolId",
                table: "Kullanicilar",
                column: "RolId");

            migrationBuilder.AddForeignKey(
                name: "FK_Kullanicilar_Roller_RolId",
                table: "Kullanicilar",
                column: "RolId",
                principalTable: "Roller",
                principalColumn: "RolId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

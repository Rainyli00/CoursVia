using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoursVia.Migrations
{
    /// <inheritdoc />
    public partial class AddKursKategorileri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Kurslar_Kategoriler_KategoriId",
                table: "Kurslar");

            migrationBuilder.DropIndex(
                name: "IX_Kurslar_KategoriId",
                table: "Kurslar");

            migrationBuilder.DropColumn(
                name: "KategoriId",
                table: "Kurslar");

            migrationBuilder.CreateTable(
                name: "KursKategorileri",
                columns: table => new
                {
                    KursKategoriId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KursId = table.Column<int>(type: "int", nullable: false),
                    KategoriId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KursKategorileri", x => x.KursKategoriId);
                    table.ForeignKey(
                        name: "FK_KursKategorileri_Kategoriler_KategoriId",
                        column: x => x.KategoriId,
                        principalTable: "Kategoriler",
                        principalColumn: "KategoriId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KursKategorileri_Kurslar_KursId",
                        column: x => x.KursId,
                        principalTable: "Kurslar",
                        principalColumn: "KursId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KursKategorileri_KategoriId",
                table: "KursKategorileri",
                column: "KategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_KursKategorileri_KursId_KategoriId",
                table: "KursKategorileri",
                columns: new[] { "KursId", "KategoriId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KursKategorileri");

            migrationBuilder.AddColumn<int>(
                name: "KategoriId",
                table: "Kurslar",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Kurslar_KategoriId",
                table: "Kurslar",
                column: "KategoriId");

            migrationBuilder.AddForeignKey(
                name: "FK_Kurslar_Kategoriler_KategoriId",
                table: "Kurslar",
                column: "KategoriId",
                principalTable: "Kategoriler",
                principalColumn: "KategoriId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

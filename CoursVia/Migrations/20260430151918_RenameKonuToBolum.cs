using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoursVia.Migrations
{
    /// <inheritdoc />
    public partial class RenameKonuToBolum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Dersler_Konular_KonuId",
                table: "Dersler");

            migrationBuilder.DropTable(
                name: "Konular");

            migrationBuilder.RenameColumn(
                name: "KonuId",
                table: "Dersler",
                newName: "BolumId");

            migrationBuilder.RenameIndex(
                name: "IX_Dersler_KonuId",
                table: "Dersler",
                newName: "IX_Dersler_BolumId");

            migrationBuilder.CreateTable(
                name: "Bolumler",
                columns: table => new
                {
                    BolumId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KursId = table.Column<int>(type: "int", nullable: false),
                    BolumAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SiraNo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bolumler", x => x.BolumId);
                    table.ForeignKey(
                        name: "FK_Bolumler_Kurslar_KursId",
                        column: x => x.KursId,
                        principalTable: "Kurslar",
                        principalColumn: "KursId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bolumler_KursId_BolumAdi",
                table: "Bolumler",
                columns: new[] { "KursId", "BolumAdi" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bolumler_KursId_SiraNo",
                table: "Bolumler",
                columns: new[] { "KursId", "SiraNo" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Dersler_Bolumler_BolumId",
                table: "Dersler",
                column: "BolumId",
                principalTable: "Bolumler",
                principalColumn: "BolumId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Dersler_Bolumler_BolumId",
                table: "Dersler");

            migrationBuilder.DropTable(
                name: "Bolumler");

            migrationBuilder.RenameColumn(
                name: "BolumId",
                table: "Dersler",
                newName: "KonuId");

            migrationBuilder.RenameIndex(
                name: "IX_Dersler_BolumId",
                table: "Dersler",
                newName: "IX_Dersler_KonuId");

            migrationBuilder.CreateTable(
                name: "Konular",
                columns: table => new
                {
                    KonuId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KursId = table.Column<int>(type: "int", nullable: false),
                    KonuAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SiraNo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Konular", x => x.KonuId);
                    table.ForeignKey(
                        name: "FK_Konular_Kurslar_KursId",
                        column: x => x.KursId,
                        principalTable: "Kurslar",
                        principalColumn: "KursId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Konular_KursId_KonuAdi",
                table: "Konular",
                columns: new[] { "KursId", "KonuAdi" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Konular_KursId_SiraNo",
                table: "Konular",
                columns: new[] { "KursId", "SiraNo" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Dersler_Konular_KonuId",
                table: "Dersler",
                column: "KonuId",
                principalTable: "Konular",
                principalColumn: "KonuId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

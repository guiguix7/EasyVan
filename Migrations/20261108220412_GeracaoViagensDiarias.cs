using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gerenciamento_de_Van.Migrations
{
    /// <inheritdoc />
    public partial class GeracaoViagensDiarias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Viagens_HorarioRotaId",
                table: "Viagens");

            migrationBuilder.CreateIndex(
                name: "IX_Viagens_HorarioRotaId_Data",
                table: "Viagens",
                columns: new[] { "HorarioRotaId", "Data" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Viagens_HorarioRotaId_Data",
                table: "Viagens");

            migrationBuilder.CreateIndex(
                name: "IX_Viagens_HorarioRotaId",
                table: "Viagens",
                column: "HorarioRotaId");
        }
    }
}

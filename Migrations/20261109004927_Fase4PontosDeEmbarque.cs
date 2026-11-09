using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gerenciamento_de_Van.Migrations
{
    /// <inheritdoc />
    public partial class Fase4PontosDeEmbarque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EnderecoEmbarque",
                table: "ViagensParadas",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LatitudeEmbarque",
                table: "ViagensParadas",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LongitudeEmbarque",
                table: "ViagensParadas",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnderecoEmbarque",
                table: "LinhasPassageiros",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LatitudeEmbarque",
                table: "LinhasPassageiros",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LongitudeEmbarque",
                table: "LinhasPassageiros",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnderecoEmbarque",
                table: "ViagensParadas");

            migrationBuilder.DropColumn(
                name: "LatitudeEmbarque",
                table: "ViagensParadas");

            migrationBuilder.DropColumn(
                name: "LongitudeEmbarque",
                table: "ViagensParadas");

            migrationBuilder.DropColumn(
                name: "EnderecoEmbarque",
                table: "LinhasPassageiros");

            migrationBuilder.DropColumn(
                name: "LatitudeEmbarque",
                table: "LinhasPassageiros");

            migrationBuilder.DropColumn(
                name: "LongitudeEmbarque",
                table: "LinhasPassageiros");
        }
    }
}

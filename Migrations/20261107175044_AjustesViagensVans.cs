using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gerenciamento_de_Van.Migrations
{
    /// <inheritdoc />
    public partial class AjustesViagensVans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DataChegada",
                table: "Viagens",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FimReal",
                table: "Viagens",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InicioReal",
                table: "Viagens",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Modelo",
                table: "Vans",
                type: "TEXT",
                maxLength: 80,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataChegada",
                table: "Viagens");

            migrationBuilder.DropColumn(
                name: "FimReal",
                table: "Viagens");

            migrationBuilder.DropColumn(
                name: "InicioReal",
                table: "Viagens");

            migrationBuilder.DropColumn(
                name: "Modelo",
                table: "Vans");
        }
    }
}

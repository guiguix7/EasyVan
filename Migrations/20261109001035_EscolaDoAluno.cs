using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gerenciamento_de_Van.Migrations
{
    /// <inheritdoc />
    public partial class EscolaDoAluno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Escola",
                table: "Alunos",
                type: "TEXT",
                maxLength: 160,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Escola",
                table: "Alunos");
        }
    }
}

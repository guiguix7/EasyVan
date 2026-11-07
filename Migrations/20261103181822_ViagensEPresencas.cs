using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gerenciamento_de_Van.Migrations
{
    /// <inheritdoc />
    public partial class ViagensEPresencas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PresencasViagem",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ViagemId = table.Column<int>(nullable: false),
                    AlunoId = table.Column<int>(nullable: false),
                    Presente = table.Column<bool>(nullable: false),
                    Observacao = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresencasViagem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Viagens",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VanId = table.Column<int>(nullable: false),
                    MotoristaId = table.Column<int>(nullable: false),
                    RotaId = table.Column<int>(nullable: false),
                    Data = table.Column<DateTime>(nullable: false),
                    HoraInicio = table.Column<TimeSpan>(nullable: true),
                    HoraFim = table.Column<TimeSpan>(nullable: true),
                    Status = table.Column<string>(maxLength: 20, nullable: false),
                    Observacao = table.Column<string>(nullable: true),
                    DataCriacao = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Viagens", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PresencasViagem");

            migrationBuilder.DropTable(
                name: "Viagens");
        }
    }
}

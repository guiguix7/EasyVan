using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gerenciamento_de_Van.Migrations
{
    /// <inheritdoc />
    public partial class AuditoriaLogOperacaoFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogOperacao",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<int>(nullable: true),
                    UsuarioNome = table.Column<string>(maxLength: 120, nullable: true),
                    TipoOperacao = table.Column<string>(maxLength: 50, nullable: false),
                    Entidade = table.Column<string>(maxLength: 50, nullable: false),
                    EntidadeId = table.Column<int>(nullable: true),
                    Descricao = table.Column<string>(maxLength: 500, nullable: true),
                    DataOperacao = table.Column<DateTime>(nullable: false),
                    InformacoesComplementares = table.Column<string>(maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogOperacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogOperacao_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LogOperacao_UsuarioId",
                table: "LogOperacao",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogOperacao");
        }
    }
}

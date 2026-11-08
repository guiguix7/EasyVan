using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gerenciamento_de_Van.Migrations
{
    /// <inheritdoc />
    public partial class SincronizarModeloAtual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "VanId",
                table: "Viagens",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "MotoristaId",
                table: "Viagens",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "HorarioRotaId",
                table: "Viagens",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Sentido",
                table: "Viagens",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Usuarios",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RespondidoEm",
                table: "PresencasViagem",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "PresencasViagem",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "HorariosRota",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RotaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Sentido = table.Column<int>(type: "INTEGER", nullable: false),
                    DiasSemana = table.Column<int>(type: "INTEGER", nullable: false),
                    HorarioSaida = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    VanPadraoId = table.Column<int>(type: "INTEGER", nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosRota", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HorariosRota_Rotas_RotaId",
                        column: x => x.RotaId,
                        principalTable: "Rotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HorariosRota_Vans_VanPadraoId",
                        column: x => x.VanPadraoId,
                        principalTable: "Vans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Organizacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizacoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Paradas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RotaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Sentido = table.Column<int>(type: "INTEGER", nullable: false),
                    Ordem = table.Column<int>(type: "INTEGER", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Endereco = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    HorarioPrevisto = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paradas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Paradas_Rotas_RotaId",
                        column: x => x.RotaId,
                        principalTable: "Rotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VinculosAluno",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AlunoId = table.Column<int>(type: "INTEGER", nullable: false),
                    RotaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParadaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Sentido = table.Column<int>(type: "INTEGER", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DataFim = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VinculosAluno", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VinculosAluno_Alunos_AlunoId",
                        column: x => x.AlunoId,
                        principalTable: "Alunos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VinculosAluno_Paradas_ParadaId",
                        column: x => x.ParadaId,
                        principalTable: "Paradas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VinculosAluno_Rotas_RotaId",
                        column: x => x.RotaId,
                        principalTable: "Rotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Viagens_HorarioRotaId",
                table: "Viagens",
                column: "HorarioRotaId");

            migrationBuilder.CreateIndex(
                name: "IX_HorariosRota_AgendaUnica",
                table: "HorariosRota",
                columns: new[] { "RotaId", "Sentido", "HorarioSaida", "DiasSemana" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HorariosRota_RotaAtivo",
                table: "HorariosRota",
                columns: new[] { "RotaId", "Ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_HorariosRota_VanPadraoId",
                table: "HorariosRota",
                column: "VanPadraoId");

            migrationBuilder.CreateIndex(
                name: "IX_Paradas_RotaSentidoOrdem",
                table: "Paradas",
                columns: new[] { "RotaId", "Sentido", "Ordem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VinculosAluno_AlunoSentido",
                table: "VinculosAluno",
                columns: new[] { "AlunoId", "Sentido" });

            migrationBuilder.CreateIndex(
                name: "IX_VinculosAluno_ParadaId",
                table: "VinculosAluno",
                column: "ParadaId");

            migrationBuilder.CreateIndex(
                name: "IX_VinculosAluno_RotaSentidoAtivo",
                table: "VinculosAluno",
                columns: new[] { "RotaId", "Sentido", "Ativo" });

            migrationBuilder.AddForeignKey(
                name: "FK_Viagens_HorariosRota_HorarioRotaId",
                table: "Viagens",
                column: "HorarioRotaId",
                principalTable: "HorariosRota",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Viagens_HorariosRota_HorarioRotaId",
                table: "Viagens");

            migrationBuilder.DropTable(
                name: "HorariosRota");

            migrationBuilder.DropTable(
                name: "Organizacoes");

            migrationBuilder.DropTable(
                name: "VinculosAluno");

            migrationBuilder.DropTable(
                name: "Paradas");

            migrationBuilder.DropIndex(
                name: "IX_Viagens_HorarioRotaId",
                table: "Viagens");

            migrationBuilder.DropColumn(
                name: "HorarioRotaId",
                table: "Viagens");

            migrationBuilder.DropColumn(
                name: "Sentido",
                table: "Viagens");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "RespondidoEm",
                table: "PresencasViagem");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PresencasViagem");

            migrationBuilder.AlterColumn<int>(
                name: "VanId",
                table: "Viagens",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MotoristaId",
                table: "Viagens",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}

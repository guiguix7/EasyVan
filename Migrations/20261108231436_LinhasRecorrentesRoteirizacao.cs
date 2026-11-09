using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gerenciamento_de_Van.Migrations
{
    /// <inheritdoc />
    public partial class LinhasRecorrentesRoteirizacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrganizacaoId",
                table: "VinculosAluno",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CalculadaEm",
                table: "Viagens",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DistanciaMetros",
                table: "Viagens",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DuracaoSegundos",
                table: "Viagens",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HashConfirmados",
                table: "Viagens",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HorarioSaidaSugerido",
                table: "Viagens",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LinhaId",
                table: "Viagens",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacaoId",
                table: "Viagens",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RoteirizacaoFalhouEm",
                table: "Viagens",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusRoteirizacao",
                table: "Viagens",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "OrganizacaoId",
                table: "Vans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Endereco",
                table: "Usuarios",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "GeocodificadoEm",
                table: "Usuarios",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Lat",
                table: "Usuarios",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Lng",
                table: "Usuarios",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacaoId",
                table: "Usuarios",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacaoId",
                table: "Rotas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacaoId",
                table: "PresencasViagem",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "PresencasViagem",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacaoId",
                table: "Paradas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacaoId",
                table: "Motoristas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacaoId",
                table: "LogOperacao",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacaoId",
                table: "HorariosRota",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OrganizacaoId",
                table: "Alunos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "Linhas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrganizacaoId = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    RotaLegadaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Destino = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    DestinoLat = table.Column<double>(type: "REAL", nullable: false),
                    DestinoLng = table.Column<double>(type: "REAL", nullable: false),
                    Origem = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    OrigemLat = table.Column<double>(type: "REAL", nullable: false),
                    OrigemLng = table.Column<double>(type: "REAL", nullable: false),
                    DiasSemana = table.Column<int>(type: "INTEGER", nullable: false),
                    HorarioChegadaIda = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    HorarioSaidaVolta = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    TemIda = table.Column<bool>(type: "INTEGER", nullable: false),
                    TemVolta = table.Column<bool>(type: "INTEGER", nullable: false),
                    VoltaTerminaNoUltimoAluno = table.Column<bool>(type: "INTEGER", nullable: false),
                    FolgaMinutos = table.Column<int>(type: "INTEGER", nullable: false),
                    Ativa = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Linhas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Linhas_Rotas_RotaLegadaId",
                        column: x => x.RotaLegadaId,
                        principalTable: "Rotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ViagensParadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrganizacaoId = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    ViagemId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Ordem = table.Column<int>(type: "INTEGER", nullable: false),
                    EtaPrevisto = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViagensParadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ViagensParadas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ViagensParadas_Viagens_ViagemId",
                        column: x => x.ViagemId,
                        principalTable: "Viagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LinhasPassageiros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrganizacaoId = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    LinhaId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    VanId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinhasPassageiros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LinhasPassageiros_Linhas_LinhaId",
                        column: x => x.LinhaId,
                        principalTable: "Linhas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LinhasPassageiros_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LinhasPassageiros_Vans_VanId",
                        column: x => x.VanId,
                        principalTable: "Vans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LinhasVans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrganizacaoId = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    LinhaId = table.Column<int>(type: "INTEGER", nullable: false),
                    VanId = table.Column<int>(type: "INTEGER", nullable: false),
                    MotoristaId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinhasVans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LinhasVans_Linhas_LinhaId",
                        column: x => x.LinhaId,
                        principalTable: "Linhas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LinhasVans_Motoristas_MotoristaId",
                        column: x => x.MotoristaId,
                        principalTable: "Motoristas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LinhasVans_Vans_VanId",
                        column: x => x.VanId,
                        principalTable: "Vans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Viagens_LinhaVanDataSentido",
                table: "Viagens",
                columns: new[] { "LinhaId", "VanId", "Data", "Sentido" },
                unique: true,
                filter: "\"LinhaId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PresencasViagem_UsuarioId",
                table: "PresencasViagem",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Linhas_OrganizacaoId_Nome",
                table: "Linhas",
                columns: new[] { "OrganizacaoId", "Nome" });

            migrationBuilder.CreateIndex(
                name: "IX_Linhas_OrganizacaoId_RotaLegadaId",
                table: "Linhas",
                columns: new[] { "OrganizacaoId", "RotaLegadaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Linhas_RotaLegadaId",
                table: "Linhas",
                column: "RotaLegadaId");

            migrationBuilder.CreateIndex(
                name: "IX_LinhasPassageiros_LinhaId_UsuarioId",
                table: "LinhasPassageiros",
                columns: new[] { "LinhaId", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LinhasPassageiros_UsuarioId",
                table: "LinhasPassageiros",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_LinhasPassageiros_VanId",
                table: "LinhasPassageiros",
                column: "VanId");

            migrationBuilder.CreateIndex(
                name: "IX_LinhasVans_LinhaId_VanId",
                table: "LinhasVans",
                columns: new[] { "LinhaId", "VanId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LinhasVans_MotoristaId",
                table: "LinhasVans",
                column: "MotoristaId");

            migrationBuilder.CreateIndex(
                name: "IX_LinhasVans_VanId",
                table: "LinhasVans",
                column: "VanId");

            migrationBuilder.CreateIndex(
                name: "IX_ViagensParadas_UsuarioId",
                table: "ViagensParadas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ViagensParadas_ViagemId_Ordem",
                table: "ViagensParadas",
                columns: new[] { "ViagemId", "Ordem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ViagensParadas_ViagemId_UsuarioId",
                table: "ViagensParadas",
                columns: new[] { "ViagemId", "UsuarioId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PresencasViagem_Usuarios_UsuarioId",
                table: "PresencasViagem",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Viagens_Linhas_LinhaId",
                table: "Viagens",
                column: "LinhaId",
                principalTable: "Linhas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PresencasViagem_Usuarios_UsuarioId",
                table: "PresencasViagem");

            migrationBuilder.DropForeignKey(
                name: "FK_Viagens_Linhas_LinhaId",
                table: "Viagens");

            migrationBuilder.DropTable(
                name: "LinhasPassageiros");

            migrationBuilder.DropTable(
                name: "LinhasVans");

            migrationBuilder.DropTable(
                name: "ViagensParadas");

            migrationBuilder.DropTable(
                name: "Linhas");

            migrationBuilder.DropIndex(
                name: "IX_Viagens_LinhaVanDataSentido",
                table: "Viagens");

            migrationBuilder.DropIndex(
                name: "IX_PresencasViagem_UsuarioId",
                table: "PresencasViagem");

            migrationBuilder.DropColumn(
                name: "OrganizacaoId",
                table: "VinculosAluno");

            migrationBuilder.DropColumn(
                name: "CalculadaEm",
                table: "Viagens");

            migrationBuilder.DropColumn(
                name: "DistanciaMetros",
                table: "Viagens");

            migrationBuilder.DropColumn(
                name: "DuracaoSegundos",
                table: "Viagens");

            migrationBuilder.DropColumn(
                name: "HashConfirmados",
                table: "Viagens");

            migrationBuilder.DropColumn(
                name: "HorarioSaidaSugerido",
                table: "Viagens");

            migrationBuilder.DropColumn(
                name: "LinhaId",
                table: "Viagens");

            migrationBuilder.DropColumn(
                name: "OrganizacaoId",
                table: "Viagens");

            migrationBuilder.DropColumn(
                name: "RoteirizacaoFalhouEm",
                table: "Viagens");

            migrationBuilder.DropColumn(
                name: "StatusRoteirizacao",
                table: "Viagens");

            migrationBuilder.DropColumn(
                name: "OrganizacaoId",
                table: "Vans");

            migrationBuilder.DropColumn(
                name: "Endereco",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "GeocodificadoEm",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Lat",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Lng",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "OrganizacaoId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "OrganizacaoId",
                table: "Rotas");

            migrationBuilder.DropColumn(
                name: "OrganizacaoId",
                table: "PresencasViagem");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "PresencasViagem");

            migrationBuilder.DropColumn(
                name: "OrganizacaoId",
                table: "Paradas");

            migrationBuilder.DropColumn(
                name: "OrganizacaoId",
                table: "Motoristas");

            migrationBuilder.DropColumn(
                name: "OrganizacaoId",
                table: "LogOperacao");

            migrationBuilder.DropColumn(
                name: "OrganizacaoId",
                table: "HorariosRota");

            migrationBuilder.DropColumn(
                name: "OrganizacaoId",
                table: "Alunos");
        }
    }
}

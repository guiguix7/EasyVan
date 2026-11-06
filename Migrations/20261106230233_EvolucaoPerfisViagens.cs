using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gerenciamento_de_Van.Migrations
{
    /// <inheritdoc />
    public partial class EvolucaoPerfisViagens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MotoristaId",
                table: "Vans",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RotaId",
                table: "Vans",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FotoPerfil",
                table: "Usuarios",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tema",
                table: "Usuarios",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "claro");

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmadoEm",
                table: "PresencasViagem",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StatusConfirmacao",
                table: "PresencasViagem",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "Motoristas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "Alunos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Viagens_MotoristaId",
                table: "Viagens",
                column: "MotoristaId");

            migrationBuilder.CreateIndex(
                name: "IX_Viagens_RotaId",
                table: "Viagens",
                column: "RotaId");

            migrationBuilder.CreateIndex(
                name: "IX_Viagens_VanId",
                table: "Viagens",
                column: "VanId");

            migrationBuilder.CreateIndex(
                name: "IX_Vans_MotoristaId",
                table: "Vans",
                column: "MotoristaId");

            migrationBuilder.CreateIndex(
                name: "IX_Vans_RotaId",
                table: "Vans",
                column: "RotaId");

            migrationBuilder.CreateIndex(
                name: "IX_PresencasViagem_AlunoId",
                table: "PresencasViagem",
                column: "AlunoId");

            migrationBuilder.CreateIndex(
                name: "IX_PresencasViagem_ViagemId_AlunoId",
                table: "PresencasViagem",
                columns: new[] { "ViagemId", "AlunoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Motoristas_UsuarioId",
                table: "Motoristas",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alunos_UsuarioId",
                table: "Alunos",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Alunos_Usuarios_UsuarioId",
                table: "Alunos",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Motoristas_Usuarios_UsuarioId",
                table: "Motoristas",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PresencasViagem_Alunos_AlunoId",
                table: "PresencasViagem",
                column: "AlunoId",
                principalTable: "Alunos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PresencasViagem_Viagens_ViagemId",
                table: "PresencasViagem",
                column: "ViagemId",
                principalTable: "Viagens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Vans_Motoristas_MotoristaId",
                table: "Vans",
                column: "MotoristaId",
                principalTable: "Motoristas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Vans_Rotas_RotaId",
                table: "Vans",
                column: "RotaId",
                principalTable: "Rotas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Viagens_Motoristas_MotoristaId",
                table: "Viagens",
                column: "MotoristaId",
                principalTable: "Motoristas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Viagens_Rotas_RotaId",
                table: "Viagens",
                column: "RotaId",
                principalTable: "Rotas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Viagens_Vans_VanId",
                table: "Viagens",
                column: "VanId",
                principalTable: "Vans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alunos_Usuarios_UsuarioId",
                table: "Alunos");

            migrationBuilder.DropForeignKey(
                name: "FK_Motoristas_Usuarios_UsuarioId",
                table: "Motoristas");

            migrationBuilder.DropForeignKey(
                name: "FK_PresencasViagem_Alunos_AlunoId",
                table: "PresencasViagem");

            migrationBuilder.DropForeignKey(
                name: "FK_PresencasViagem_Viagens_ViagemId",
                table: "PresencasViagem");

            migrationBuilder.DropForeignKey(
                name: "FK_Vans_Motoristas_MotoristaId",
                table: "Vans");

            migrationBuilder.DropForeignKey(
                name: "FK_Vans_Rotas_RotaId",
                table: "Vans");

            migrationBuilder.DropForeignKey(
                name: "FK_Viagens_Motoristas_MotoristaId",
                table: "Viagens");

            migrationBuilder.DropForeignKey(
                name: "FK_Viagens_Rotas_RotaId",
                table: "Viagens");

            migrationBuilder.DropForeignKey(
                name: "FK_Viagens_Vans_VanId",
                table: "Viagens");

            migrationBuilder.DropIndex(
                name: "IX_Viagens_MotoristaId",
                table: "Viagens");

            migrationBuilder.DropIndex(
                name: "IX_Viagens_RotaId",
                table: "Viagens");

            migrationBuilder.DropIndex(
                name: "IX_Viagens_VanId",
                table: "Viagens");

            migrationBuilder.DropIndex(
                name: "IX_Vans_MotoristaId",
                table: "Vans");

            migrationBuilder.DropIndex(
                name: "IX_Vans_RotaId",
                table: "Vans");

            migrationBuilder.DropIndex(
                name: "IX_PresencasViagem_AlunoId",
                table: "PresencasViagem");

            migrationBuilder.DropIndex(
                name: "IX_PresencasViagem_ViagemId_AlunoId",
                table: "PresencasViagem");

            migrationBuilder.DropIndex(
                name: "IX_Motoristas_UsuarioId",
                table: "Motoristas");

            migrationBuilder.DropIndex(
                name: "IX_Alunos_UsuarioId",
                table: "Alunos");

            migrationBuilder.DropColumn(
                name: "MotoristaId",
                table: "Vans");

            migrationBuilder.DropColumn(
                name: "RotaId",
                table: "Vans");

            migrationBuilder.DropColumn(
                name: "FotoPerfil",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Tema",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ConfirmadoEm",
                table: "PresencasViagem");

            migrationBuilder.DropColumn(
                name: "StatusConfirmacao",
                table: "PresencasViagem");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Motoristas");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Alunos");
        }
    }
}

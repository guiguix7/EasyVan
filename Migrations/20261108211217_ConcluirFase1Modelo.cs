using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gerenciamento_de_Van.Migrations
{
    /// <inheritdoc />
    public partial class ConcluirFase1Modelo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "PresencasViagem"
                SET "Status" = CASE
                    WHEN "StatusConfirmacao" = TRUE THEN 1
                    WHEN "StatusConfirmacao" = FALSE THEN 2
                    ELSE 0
                END,
                "RespondidoEm" = COALESCE("RespondidoEm", "ConfirmadoEm");
                """);

            migrationBuilder.DropIndex(
                name: "IX_VinculosAluno_AlunoSentido",
                table: "VinculosAluno");

            if (migrationBuilder.ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql("CREATE UNIQUE INDEX \"IX_VinculosAluno_AlunoSentidoAtivo\" ON \"VinculosAluno\" (\"AlunoId\", \"Sentido\") WHERE \"Ativo\" = 1;");
            }
            else
            {
                migrationBuilder.Sql("CREATE UNIQUE INDEX \"IX_VinculosAluno_AlunoSentidoAtivo\" ON \"VinculosAluno\" (\"AlunoId\", \"Sentido\") WHERE \"Ativo\" = TRUE;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VinculosAluno_AlunoSentidoAtivo",
                table: "VinculosAluno");

            migrationBuilder.CreateIndex(
                name: "IX_VinculosAluno_AlunoSentido",
                table: "VinculosAluno",
                columns: new[] { "AlunoId", "Sentido" });
        }
    }
}

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
            if (migrationBuilder.ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                // SQLite stores DateTime values as TEXT. Keep the original SQLite path unchanged.
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

                migrationBuilder.Sql("CREATE UNIQUE INDEX \"IX_VinculosAluno_AlunoSentidoAtivo\" ON \"VinculosAluno\" (\"AlunoId\", \"Sentido\") WHERE \"Ativo\" = 1;");
            }
            else
            {
                // Older PostgreSQL schemas may have RespondidoEm as TEXT while
                // ConfirmadoEm is timestamptz. Normalize the legacy column before
                // COALESCE so PostgreSQL never has to resolve incompatible types.
                // Empty strings become NULL; valid ISO date/time values are preserved.
                migrationBuilder.Sql("""
                    ALTER TABLE "PresencasViagem"
                    ALTER COLUMN "RespondidoEm" TYPE timestamp with time zone
                    USING NULLIF(BTRIM("RespondidoEm"::text), '')::timestamp with time zone;

                    UPDATE "PresencasViagem"
                    SET "Status" = CASE
                        WHEN "StatusConfirmacao" = TRUE THEN 1
                        WHEN "StatusConfirmacao" = FALSE THEN 2
                        ELSE 0
                    END,
                    "RespondidoEm" = COALESCE("RespondidoEm", "ConfirmadoEm");
                    """);

                // The legacy schema can still have Ativo as an integer (0/1),
                // although the current EF model maps it as boolean. Normalize it
                // without changing the represented values before building the
                // PostgreSQL partial unique index.
                migrationBuilder.Sql("""
                    DO $migration$
                    DECLARE ativo_type text;
                    BEGIN
                        SELECT data_type INTO ativo_type
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'VinculosAluno'
                          AND column_name = 'Ativo';

                        IF ativo_type IN ('smallint', 'integer', 'bigint') THEN
                            ALTER TABLE "VinculosAluno"
                            ALTER COLUMN "Ativo" TYPE boolean
                            USING ("Ativo" <> 0);
                        END IF;
                    END
                    $migration$;

                    DROP INDEX IF EXISTS "IX_VinculosAluno_AlunoSentido";
                    DROP INDEX IF EXISTS "IX_VinculosAluno_AlunoSentidoAtivo";

                    CREATE UNIQUE INDEX "IX_VinculosAluno_AlunoSentidoAtivo"
                    ON "VinculosAluno" ("AlunoId", "Sentido")
                    WHERE "Ativo" = TRUE;
                    """);
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

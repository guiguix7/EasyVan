using System.Data;
using Microsoft.EntityFrameworkCore;

namespace EasyVan.Data;

/// <summary>
/// Inicializa o PostgreSQL vazio a partir do modelo EF atual e registra as migrations
/// SQLite existentes como baseline. A partir daí, novas migrations EF são aplicadas normalmente.
/// </summary>
public static class PostgresDatabaseBootstrapper
{
    public static void Initialize(ApplicationDbContext db)
    {
        if (db.Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            db.Database.Migrate();
            return;
        }

        var appliedMigrations = db.Database.GetAppliedMigrations().ToArray();
        if (appliedMigrations.Length == 0)
        {
            EnsurePublicSchemaIsEmpty(db);
            db.Database.EnsureCreated();

            db.Database.ExecuteSqlRaw("""
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" character varying(150) NOT NULL,
                    "ProductVersion" character varying(32) NOT NULL,
                    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
                );
                """);

            foreach (var migrationId in db.Database.GetMigrations())
            {
                db.Database.ExecuteSqlInterpolated($"""
                    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                    VALUES ({migrationId}, {"10.0.13"})
                    ON CONFLICT ("MigrationId") DO NOTHING;
                    """);
            }
        }

        // Aplica migrations futuras normalmente após o baseline inicial.
        db.Database.Migrate();

        // O EasyVan autentica usuários por cookies ASP.NET, não pelo Supabase Auth.
        // Nenhum cliente público deve consultar diretamente as tabelas operacionais.
        db.Database.ExecuteSqlRaw("""
            DO $body$
            DECLARE app_table record;
            BEGIN
                FOR app_table IN
                    SELECT tablename
                    FROM pg_tables
                    WHERE schemaname = 'public'
                      AND tablename <> '__EFMigrationsHistory'
                LOOP
                    EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', app_table.tablename);
                END LOOP;
            END $body$;

            REVOKE ALL ON ALL TABLES IN SCHEMA public FROM anon, authenticated;
            REVOKE ALL ON ALL SEQUENCES IN SCHEMA public FROM anon, authenticated;
            ALTER DEFAULT PRIVILEGES IN SCHEMA public
                REVOKE ALL ON TABLES FROM anon, authenticated;
            ALTER DEFAULT PRIVILEGES IN SCHEMA public
                REVOKE ALL ON SEQUENCES FROM anon, authenticated;
            """);
    }

    private static void EnsurePublicSchemaIsEmpty(ApplicationDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        var closeAfterCheck = connection.State != ConnectionState.Open;
        if (closeAfterCheck)
            connection.Open();

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND table_type = 'BASE TABLE';
                """;

            var tableCount = Convert.ToInt32(command.ExecuteScalar());
            if (tableCount > 0)
            {
                throw new InvalidOperationException(
                    "O schema public do Supabase já contém tabelas, mas não há histórico EF Core. " +
                    "A inicialização automática foi interrompida para não sobrescrever dados existentes.");
            }
        }
        finally
        {
            if (closeAfterCheck)
                connection.Close();
        }
    }
}

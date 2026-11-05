using System.Data;
using Microsoft.EntityFrameworkCore;

namespace EasyVan.Data;

/// <summary>
/// Inicializa o PostgreSQL vazio a partir do modelo EF atual e registra as migrations
/// SQLite existentes como baseline. A partir daí, novas migrations EF são aplicadas normalmente.
/// </summary>
public static class PostgresDatabaseBootstrapper
{
    private static readonly string[] ExistingMigrationIds =
    [
        "20261103174311_InitialCreate",
        "20261103175128_RenamePassageirosToAlunos",
        "20261103181822_ViagensEPresencas"
    ];

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

            foreach (var migrationId in ExistingMigrationIds)
            {
                db.Database.ExecuteSqlInterpolated($"""
                    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                    VALUES ({migrationId}, {"8.0.0"})
                    ON CONFLICT ("MigrationId") DO NOTHING;
                    """);
            }
        }

        // Aplica migrations futuras normalmente após o baseline inicial.
        db.Database.Migrate();

        // O EasyVan autentica usuários por cookies ASP.NET, não pelo Supabase Auth.
        // Nenhum cliente público deve consultar diretamente as tabelas operacionais.
        db.Database.ExecuteSqlRaw("""
            DO $$
            DECLARE table_name text;
            BEGIN
                FOREACH table_name IN ARRAY ARRAY[
                    'Usuarios', 'Vans', 'Motoristas', 'Alunos',
                    'Rotas', 'Viagens', 'PresencasViagem'
                ]
                LOOP
                    EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', table_name);
                END LOOP;
            END $$;

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

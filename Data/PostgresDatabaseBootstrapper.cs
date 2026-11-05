using System.Data;
using Microsoft.EntityFrameworkCore;

namespace EasyVan.Data;

/// <summary>
/// Initializes an empty PostgreSQL database from the current EF model and records
/// the existing SQLite migrations as a baseline. Later pending migrations are applied normally.
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

        // GetAppliedMigrations creates __EFMigrationsHistory when it does not exist.
        // EnsureCreated treats that history table as an existing application schema and
        // then skips creating every application table. Exclude the history table when
        // deciding whether the application schema is empty.
        var appliedMigrations = db.Database.GetAppliedMigrations().ToArray();
        var applicationTableCount = GetPublicApplicationTableCount(db);

        if (applicationTableCount == 0)
        {
            // This also repairs a previous interrupted bootstrap where only the EF
            // history table was created. No application tables or user data are removed.
            db.Database.ExecuteSqlRaw("""DROP TABLE IF EXISTS "__EFMigrationsHistory";""");
            db.Database.EnsureCreated();

            db.Database.ExecuteSqlRaw("""
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" character varying(150) NOT NULL,
                    "ProductVersion" character varying(32) NOT NULL,
                    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
                );
                """);

            // On a fresh database, baseline the existing SQLite migrations. If a prior
            // bootstrap recorded them but failed before creating app tables, preserve
            // that same baseline after EnsureCreated has recreated the schema.
            var migrationsToBaseline = appliedMigrations.Length == 0
                ? db.Database.GetMigrations()
                : appliedMigrations;

            foreach (var migrationId in migrationsToBaseline)
            {
                db.Database.ExecuteSqlInterpolated($"""
                    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                    VALUES ({migrationId}, {"10.0.13"})
                    ON CONFLICT ("MigrationId") DO NOTHING;
                    """);
            }
        }
        else if (appliedMigrations.Length == 0)
        {
            throw new InvalidOperationException(
                "O schema public do Supabase já contém tabelas, mas não há histórico EF Core. " +
                "A inicialização automática foi interrompida para não sobrescrever dados existentes.");
        }

        // SQLite-generated snapshots contain provider-specific column types, so invoking
        // Migrate unconditionally can raise PendingModelChangesWarning on PostgreSQL even
        // when every existing migration has already been baselined. Only invoke the
        // migrator when migration IDs are actually pending.
        if (db.Database.GetPendingMigrations().Any())
            db.Database.Migrate();

        // The EasyVan app authenticates users with ASP.NET cookies, not Supabase Auth.
        // Public Supabase API roles must not query operational application tables.
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
            """);

        db.Database.ExecuteSqlRaw("REVOKE ALL ON ALL TABLES IN SCHEMA public FROM anon, authenticated;");
        db.Database.ExecuteSqlRaw("REVOKE ALL ON ALL SEQUENCES IN SCHEMA public FROM anon, authenticated;");
        db.Database.ExecuteSqlRaw("ALTER DEFAULT PRIVILEGES IN SCHEMA public REVOKE ALL ON TABLES FROM anon, authenticated;");
        db.Database.ExecuteSqlRaw("ALTER DEFAULT PRIVILEGES IN SCHEMA public REVOKE ALL ON SEQUENCES FROM anon, authenticated;");
    }

    private static int GetPublicApplicationTableCount(ApplicationDbContext db)
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
                  AND table_type = 'BASE TABLE'
                  AND table_name <> '__EFMigrationsHistory';
                """;

            return Convert.ToInt32(command.ExecuteScalar());
        }
        finally
        {
            if (closeAfterCheck)
                connection.Close();
        }
    }
}

using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

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
        // Keep the history separate from the application schema when detecting an empty DB.
        var appliedMigrations = db.Database.GetAppliedMigrations().ToArray();
        var applicationTableCount = GetPublicApplicationTableCount(db);

        if (applicationTableCount == 0)
        {
            // Repair an interrupted bootstrap where only the EF history table exists.
            // No application tables or user data are removed.
            db.Database.ExecuteSqlRaw("""DROP TABLE IF EXISTS "__EFMigrationsHistory";""");

            // EnsureCreated() can decide not to create the model when PostgreSQL reports
            // any relation in the schema (including non-table relations). Execute the
            // provider-generated model DDL directly so every EF entity is materialized.
            var createModelSql = db.Database.GenerateCreateScript();
            if (string.IsNullOrWhiteSpace(createModelSql))
                throw new InvalidOperationException("O EF Core gerou um script vazio para criar o schema PostgreSQL.");

            db.Database.ExecuteSqlRaw(createModelSql);

            var createdTableCount = GetPublicApplicationTableCount(db);
            if (createdTableCount == 0)
                throw new InvalidOperationException("O script de criação do EF Core foi executado, mas nenhuma tabela da aplicação foi encontrada no schema public.");

            db.Database.ExecuteSqlRaw("""
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" character varying(150) NOT NULL,
                    "ProductVersion" character varying(32) NOT NULL,
                    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
                );
                """);

            // On a fresh database, baseline the existing SQLite migrations. If a prior
            // bootstrap recorded them but failed before creating app tables, preserve
            // that same baseline after the schema has been recreated.
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

        // SQLite-origin migrations create boolean columns as INTEGER on PostgreSQL.
        // Normalize every bool property in the EF model before DbInitializer writes
        // data, preserving NULL and mapping legacy 0/1 values to false/true.
        NormalizeLegacyBooleanColumns(db);

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

    private static void NormalizeLegacyBooleanColumns(ApplicationDbContext db)
    {
        var booleanColumns = db.Model.GetEntityTypes()
            .SelectMany(entityType =>
            {
                var tableName = entityType.GetTableName();
                if (tableName is null)
                    return Enumerable.Empty<(string Schema, string Table, string Column)>();

                // EF model metadata uses null for the default schema; the SQL
                // lookup below resolves that same default schema explicitly as public.
                var schema = entityType.GetSchema();
                var storeObject = StoreObjectIdentifier.Table(tableName, schema);

                return entityType.GetProperties()
                    .Where(property =>
                        (Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) == typeof(bool))
                    .Select(property => property.GetColumnName(storeObject))
                    .Where(column => !string.IsNullOrWhiteSpace(column))
                    .Select(column => (Schema: schema ?? "public", Table: tableName, Column: column!));
            })
            .Distinct()
            .ToArray();

        if (booleanColumns.Length == 0)
            return;

        var connection = db.Database.GetDbConnection();
        var closeAfterCheck = connection.State != ConnectionState.Open;
        if (closeAfterCheck)
            connection.Open();

        try
        {
            var sqlHelper = db.GetService<ISqlGenerationHelper>();
            foreach (var column in booleanColumns)
            {
                using var typeCommand = connection.CreateCommand();
                typeCommand.CommandText = """
                    SELECT data_type
                    FROM information_schema.columns
                    WHERE table_schema = @schema
                      AND table_name = @table
                      AND column_name = @column;
                    """;

                foreach (var (name, value) in new[]
                {
                    ("@schema", column.Schema),
                    ("@table", column.Table),
                    ("@column", column.Column)
                })
                {
                    var parameter = typeCommand.CreateParameter();
                    parameter.ParameterName = name;
                    parameter.Value = value;
                    typeCommand.Parameters.Add(parameter);
                }

                var dataType = Convert.ToString(typeCommand.ExecuteScalar());
                if (dataType is not ("smallint" or "integer" or "bigint"))
                    continue;

                var tableIdentifier = sqlHelper.DelimitIdentifier(column.Table, column.Schema);
                var columnIdentifier = sqlHelper.DelimitIdentifier(column.Column);
                using var alterCommand = connection.CreateCommand();
                alterCommand.CommandText = $"""
                    ALTER TABLE {tableIdentifier}
                    ALTER COLUMN {columnIdentifier} TYPE boolean
                    USING ({columnIdentifier} <> 0);
                    """;
                alterCommand.ExecuteNonQuery();
            }
        }
        finally
        {
            if (closeAfterCheck)
                connection.Close();
        }
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

using EasyVan.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gerenciamento_de_Van.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261103235959_EnsureBaseTables")]
    public partial class EnsureBaseTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // These two tables existed in older databases created with EnsureCreated().
            // Create them only when absent so existing installations keep their data.
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "Usuarios" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Usuarios" PRIMARY KEY AUTOINCREMENT,
                    "Nome" TEXT NOT NULL,
                    "Email" TEXT NOT NULL,
                    "RoleManager" TEXT NOT NULL,
                    "PasswordHasher" TEXT NOT NULL
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "Vans" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Vans" PRIMARY KEY AUTOINCREMENT,
                    "Plate" TEXT NOT NULL,
                    "Driver" TEXT NOT NULL,
                    "Capacity" INTEGER NOT NULL,
                    "Route" TEXT NOT NULL,
                    "Schedule" TEXT NOT NULL,
                    "Status" TEXT NOT NULL,
                    "Description" TEXT NOT NULL
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally leave these tables in place: they may predate migrations
            // and can contain user data in existing installations.
        }
    }
}

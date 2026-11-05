using EasyVan.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gerenciamento_de_Van.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261105010000_AddUniqueNormalizedUserEmail")]
    public partial class AddUniqueNormalizedUserEmail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Do not delete or merge accounts automatically. If duplicates exist,
            // resolve them deliberately before applying this migration.
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_Usuarios_Email"
                ON "Usuarios" ("Email" COLLATE NOCASE);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Usuarios_Email";""");
        }
    }
}
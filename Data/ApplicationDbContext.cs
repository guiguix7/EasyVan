using Microsoft.EntityFrameworkCore;
using EasyVan.Models;

namespace EasyVan.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Usuarios> Usuarios { get; set; }
        public DbSet<Van> Vans { get; set; }
        public DbSet<Motorista> Motoristas { get; set; }
        public DbSet<Aluno> Alunos { get; set; }
        public DbSet<Rota> Rotas { get; set; }
        public DbSet<Viagem> Viagens { get; set; }
        public DbSet<PresencaViagem> PresencasViagem { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Usuarios>(entity =>
            {
                // E-mails são normalizados para minúsculas pela aplicação antes de salvar.
                // Evita depender da collation NOCASE, que existe no SQLite, mas não no PostgreSQL.
                entity.HasIndex(usuario => usuario.Email)
                    .IsUnique()
                    .HasDatabaseName("IX_Usuarios_Email");
            });
        }
    }
}

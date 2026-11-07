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
        public DbSet<LogOperacao> LogOperacao { get; set; }

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

            modelBuilder.Entity<Aluno>()
                .HasIndex(a => a.UsuarioId)
                .IsUnique();

            modelBuilder.Entity<Motorista>()
                .HasIndex(m => m.UsuarioId)
                .IsUnique();

            modelBuilder.Entity<Aluno>()
                .HasOne(a => a.Usuario)
                .WithOne()
                .HasForeignKey<Aluno>(a => a.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Motorista>()
                .HasOne(m => m.Usuario)
                .WithOne()
                .HasForeignKey<Motorista>(m => m.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Van>()
                .HasOne(v => v.Motorista)
                .WithMany()
                .HasForeignKey(v => v.MotoristaId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Van>()
                .HasOne(v => v.Rota)
                .WithMany()
                .HasForeignKey(v => v.RotaId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Viagem>()
                .HasOne(v => v.Van)
                .WithMany()
                .HasForeignKey(v => v.VanId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Viagem>()
                .HasOne(v => v.Motorista)
                .WithMany()
                .HasForeignKey(v => v.MotoristaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Viagem>()
                .HasOne(v => v.Rota)
                .WithMany()
                .HasForeignKey(v => v.RotaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PresencaViagem>()
                .HasIndex(p => new { p.ViagemId, p.AlunoId })
                .IsUnique();

            modelBuilder.Entity<PresencaViagem>()
                .HasOne(p => p.Viagem)
                .WithMany(v => v.Presencas)
                .HasForeignKey(p => p.ViagemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PresencaViagem>()
                .HasOne(p => p.Aluno)
                .WithMany()
                .HasForeignKey(p => p.AlunoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

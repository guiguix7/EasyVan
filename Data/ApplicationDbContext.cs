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
        public DbSet<Organizacao> Organizacoes { get; set; }
        public DbSet<HorarioRota> HorariosRota { get; set; }
        public DbSet<Parada> Paradas { get; set; }
        public DbSet<VinculoAluno> VinculosAluno { get; set; }
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

                entity.Property(usuario => usuario.Ativo)
                    .HasDefaultValue(true);
            });

            modelBuilder.Entity<Organizacao>(entity =>
            {
                entity.ToTable("Organizacoes");
            });

            modelBuilder.Entity<HorarioRota>(entity =>
            {
                entity.ToTable("HorariosRota");
                entity.HasIndex(h => new { h.RotaId, h.Sentido, h.HorarioSaida, h.DiasSemana })
                    .IsUnique()
                    .HasDatabaseName("IX_HorariosRota_AgendaUnica");
                entity.HasIndex(h => new { h.RotaId, h.Ativo })
                    .HasDatabaseName("IX_HorariosRota_RotaAtivo");

                entity.HasOne(h => h.Rota)
                    .WithMany()
                    .HasForeignKey(h => h.RotaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(h => h.VanPadrao)
                    .WithMany()
                    .HasForeignKey(h => h.VanPadraoId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Parada>(entity =>
            {
                entity.ToTable("Paradas");
                entity.HasIndex(p => new { p.RotaId, p.Sentido, p.Ordem })
                    .IsUnique()
                    .HasDatabaseName("IX_Paradas_RotaSentidoOrdem");

                entity.HasOne(p => p.Rota)
                    .WithMany()
                    .HasForeignKey(p => p.RotaId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<VinculoAluno>(entity =>
            {
                entity.ToTable("VinculosAluno");
                // O índice único parcial (um vínculo ativo por aluno/sentido) é criado na
                // migration com SQL específico de cada provider; o modelo mantém apenas a busca.
                entity.HasIndex(v => new { v.AlunoId, v.Sentido })
                    .HasDatabaseName("IX_VinculosAluno_AlunoSentido");
                entity.HasIndex(v => new { v.RotaId, v.Sentido, v.Ativo })
                    .HasDatabaseName("IX_VinculosAluno_RotaSentidoAtivo");

                entity.HasOne(v => v.Aluno)
                    .WithMany()
                    .HasForeignKey(v => v.AlunoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(v => v.Rota)
                    .WithMany()
                    .HasForeignKey(v => v.RotaId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(v => v.Parada)
                    .WithMany(p => p.Vinculos)
                    .HasForeignKey(v => v.ParadaId)
                    .OnDelete(DeleteBehavior.Restrict);
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

            modelBuilder.Entity<Viagem>()
                .HasOne(v => v.HorarioRota)
                .WithMany(h => h.Viagens)
                .HasForeignKey(v => v.HorarioRotaId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PresencaViagem>()
                .Property(p => p.Status)
                .HasDefaultValue(StatusPresenca.SemResposta);

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

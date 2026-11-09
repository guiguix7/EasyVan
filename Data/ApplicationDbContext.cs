using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using EasyVan.Models;

namespace EasyVan.Data
{
    public class ApplicationDbContext : DbContext
    {
        private int _organizacaoAtualId;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : this(options, 1) { }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor accessor)
            : this(options, int.TryParse(accessor.HttpContext?.User.FindFirstValue("EasyVan:OrganizacaoId"), out var id) ? id : 1) { }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, int organizacaoId) : base(options)
        {
            _organizacaoAtualId = organizacaoId > 0 ? organizacaoId : 1;
        }

        internal void DefinirOrganizacaoAtual(int organizacaoId)
        {
            if (organizacaoId <= 0) throw new ArgumentOutOfRangeException(nameof(organizacaoId));
            _organizacaoAtualId = organizacaoId;
        }

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
        public DbSet<Linha> Linhas { get; set; }
        public DbSet<LinhaVan> LinhasVans { get; set; }
        public DbSet<LinhaPassageiro> LinhasPassageiros { get; set; }
        public DbSet<ViagemParada> ViagensParadas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Usuarios>().HasQueryFilter(e => e.OrganizacaoId == _organizacaoAtualId);
            modelBuilder.Entity<Aluno>().HasQueryFilter(e => e.OrganizacaoId == _organizacaoAtualId);
            modelBuilder.Entity<Motorista>().HasQueryFilter(e => e.OrganizacaoId == _organizacaoAtualId);
            modelBuilder.Entity<Van>().HasQueryFilter(e => e.OrganizacaoId == _organizacaoAtualId);
            modelBuilder.Entity<Rota>().HasQueryFilter(e => e.OrganizacaoId == _organizacaoAtualId);
            modelBuilder.Entity<Viagem>().HasQueryFilter(e => e.OrganizacaoId == _organizacaoAtualId);
            modelBuilder.Entity<PresencaViagem>().HasQueryFilter(e => e.OrganizacaoId == _organizacaoAtualId);
            modelBuilder.Entity<HorarioRota>().HasQueryFilter(e => e.OrganizacaoId == _organizacaoAtualId);
            modelBuilder.Entity<Parada>().HasQueryFilter(e => e.OrganizacaoId == _organizacaoAtualId);
            modelBuilder.Entity<VinculoAluno>().HasQueryFilter(e => e.OrganizacaoId == _organizacaoAtualId);
            modelBuilder.Entity<LogOperacao>().HasQueryFilter(e => e.OrganizacaoId == _organizacaoAtualId);
            modelBuilder.Entity<Linha>().HasQueryFilter(e => e.OrganizacaoId == _organizacaoAtualId);
            modelBuilder.Entity<LinhaVan>().HasQueryFilter(e => e.OrganizacaoId == _organizacaoAtualId);
            modelBuilder.Entity<LinhaPassageiro>().HasQueryFilter(e => e.OrganizacaoId == _organizacaoAtualId);
            modelBuilder.Entity<ViagemParada>().HasQueryFilter(e => e.OrganizacaoId == _organizacaoAtualId);
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
                entity.HasIndex(v => new { v.AlunoId, v.Sentido })
                    .IsUnique()
                    .HasFilter(Database.IsNpgsql() ? "\"Ativo\" = TRUE" : "\"Ativo\" = 1")
                    .HasDatabaseName("IX_VinculosAluno_AlunoSentidoAtivo");
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

            modelBuilder.Entity<Viagem>()
                .HasIndex(v => new { v.HorarioRotaId, v.Data })
                .IsUnique()
                .HasDatabaseName("IX_Viagens_HorarioRotaId_Data");

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
            modelBuilder.Entity<PresencaViagem>()
                .HasOne(p => p.Usuario)
                .WithMany()
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Linha>(entity =>
            {
                entity.HasIndex(l => new { l.OrganizacaoId, l.Nome });
                entity.HasIndex(l => new { l.OrganizacaoId, l.RotaLegadaId }).IsUnique();
                entity.HasOne<Rota>().WithMany().HasForeignKey(l => l.RotaLegadaId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LinhaVan>(entity =>
            {
                entity.HasIndex(v => new { v.LinhaId, v.VanId }).IsUnique();
                entity.HasOne(v => v.Linha).WithMany(l => l.Vans).HasForeignKey(v => v.LinhaId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(v => v.Van).WithMany().HasForeignKey(v => v.VanId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(v => v.Motorista).WithMany().HasForeignKey(v => v.MotoristaId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LinhaPassageiro>(entity =>
            {
                entity.HasIndex(p => new { p.LinhaId, p.UsuarioId }).IsUnique();
                entity.HasOne(p => p.Linha).WithMany(l => l.Passageiros).HasForeignKey(p => p.LinhaId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(p => p.Usuario).WithMany().HasForeignKey(p => p.UsuarioId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(p => p.Van).WithMany().HasForeignKey(p => p.VanId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ViagemParada>(entity =>
            {
                entity.HasIndex(p => new { p.ViagemId, p.Ordem }).IsUnique();
                entity.HasIndex(p => new { p.ViagemId, p.UsuarioId }).IsUnique();
                entity.HasOne(p => p.Viagem).WithMany(v => v.ParadasCalculadas).HasForeignKey(p => p.ViagemId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(p => p.Usuario).WithMany().HasForeignKey(p => p.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Viagem>().HasOne(v => v.Linha).WithMany().HasForeignKey(v => v.LinhaId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Viagem>().HasIndex(v => new { v.LinhaId, v.VanId, v.Data, v.Sentido })
                .IsUnique().HasFilter("\"LinhaId\" IS NOT NULL").HasDatabaseName("IX_Viagens_LinhaVanDataSentido");

            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                         .Where(t => typeof(IOrganizacaoScoped).IsAssignableFrom(t.ClrType)))
            {
                modelBuilder.Entity(entityType.ClrType).Property<int>(nameof(IOrganizacaoScoped.OrganizacaoId)).HasDefaultValue(1);
            }
        }

        public override int SaveChanges()
        {
            AjustarOrganizacao();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            AjustarOrganizacao();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void AjustarOrganizacao()
        {
            foreach (var entry in ChangeTracker.Entries<IOrganizacaoScoped>().Where(e => e.State == EntityState.Added))
                entry.Entity.OrganizacaoId = _organizacaoAtualId;
        }
    }
}

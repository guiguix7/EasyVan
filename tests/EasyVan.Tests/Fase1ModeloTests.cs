using EasyVan.Data;
using EasyVan.Models;
using EasyVan.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EasyVan.Tests;

public sealed class Fase1ModeloTests
{
    [Fact]
    public async Task MigrationsDaFase1AplicamEmSQLiteVazio()
    {
        await using var conexao = new SqliteConnection("Data Source=:memory:");
        await conexao.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(conexao)
            .Options;

        await using var db = new ApplicationDbContext(options);
        await db.Database.MigrateAsync();

        Assert.Contains("20261108211217_ConcluirFase1Modelo", await db.Database.GetAppliedMigrationsAsync());
        Assert.Equal(0, await db.Organizacoes.CountAsync());
        await using var comando = db.Database.GetDbConnection().CreateCommand();
        comando.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'IX_VinculosAluno_AlunoSentidoAtivo';";
        var sqlIndice = (string?)await comando.ExecuteScalarAsync();
        Assert.Contains("WHERE \"Ativo\" = 1", sqlIndice, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InicializadorCriaOrganizacaoAgendaParadasEVinculosDeDemonstracaoSemDuplicar()
    {
        using var db = TestesInfra.CriarBanco();
        var senha = new HasherFake();

        DbInitializer.Initialize(db, senha, seedDemoUsers: true);
        DbInitializer.Initialize(db, senha, seedDemoUsers: true);

        Assert.Equal("EasyVan", Assert.Single(db.Organizacoes).Nome);
        Assert.Equal(2, db.HorariosRota.Count());
        Assert.Equal(2, db.Paradas.Count());
        Assert.Equal(2, db.VinculosAluno.Count(v => v.Ativo));
    }

    [Fact]
    public async Task AgendaRejeitaDiasVaziosHorarioInvalidoEAgendaDuplicada()
    {
        using var db = TestesInfra.CriarBanco();
        var (_, rota, _) = TestesInfra.CriarReferencias(db);
        var service = new AgendaRotaService(db);

        var semDias = await service.ValidarHorarioAsync(new HorarioRota
        {
            RotaId = rota.Id,
            Sentido = SentidoViagem.Ida,
            DiasSemana = DiasSemana.Nenhum,
            HorarioSaida = new TimeSpan(7, 0, 0)
        });
        Assert.False(semDias.Sucesso);

        var horarioInvalido = await service.ValidarHorarioAsync(new HorarioRota
        {
            RotaId = rota.Id,
            Sentido = SentidoViagem.Ida,
            DiasSemana = DiasSemana.DiasUteis,
            HorarioSaida = TimeSpan.FromDays(1)
        });
        Assert.False(horarioInvalido.Sucesso);

        db.HorariosRota.Add(new HorarioRota
        {
            RotaId = rota.Id,
            Sentido = SentidoViagem.Ida,
            DiasSemana = DiasSemana.DiasUteis,
            HorarioSaida = new TimeSpan(7, 0, 0)
        });
        await db.SaveChangesAsync();

        var duplicado = await service.ValidarHorarioAsync(new HorarioRota
        {
            RotaId = rota.Id,
            Sentido = SentidoViagem.Ida,
            DiasSemana = DiasSemana.DiasUteis,
            HorarioSaida = new TimeSpan(7, 0, 0)
        });
        Assert.False(duplicado.Sucesso);
    }

    [Fact]
    public async Task ParadaRejeitaOrdemInvalidaEnderecoVazioEDuplicidade()
    {
        using var db = TestesInfra.CriarBanco();
        var (_, rota, _) = TestesInfra.CriarReferencias(db);
        var service = new AgendaRotaService(db);

        Assert.False((await service.ValidarParadaAsync(new Parada
        {
            RotaId = rota.Id,
            Sentido = SentidoViagem.Ida,
            Ordem = 0,
            Endereco = "Ponto"
        })).Sucesso);

        Assert.False((await service.ValidarParadaAsync(new Parada
        {
            RotaId = rota.Id,
            Sentido = SentidoViagem.Ida,
            Ordem = 1,
            Endereco = " "
        })).Sucesso);

        db.Paradas.Add(new Parada
        {
            RotaId = rota.Id,
            Sentido = SentidoViagem.Ida,
            Ordem = 1,
            Endereco = "Centro"
        });
        await db.SaveChangesAsync();

        Assert.False((await service.ValidarParadaAsync(new Parada
        {
            RotaId = rota.Id,
            Sentido = SentidoViagem.Ida,
            Ordem = 1,
            Endereco = "Outro ponto"
        })).Sucesso);
    }

    [Fact]
    public async Task NovoVinculoDesativaOVinculoAnteriorDoMesmoSentido()
    {
        using var db = TestesInfra.CriarBanco();
        var (_, rota, _) = TestesInfra.CriarReferencias(db);
        var aluno = new Aluno { Nome = "Aluno", Email = "aluno@easyvan.com", Ativo = true };
        db.Alunos.Add(aluno);
        db.Paradas.AddRange(
            new Parada { RotaId = rota.Id, Sentido = SentidoViagem.Ida, Ordem = 1, Endereco = "Ponto A" },
            new Parada { RotaId = rota.Id, Sentido = SentidoViagem.Ida, Ordem = 2, Endereco = "Ponto B" },
            new Parada { RotaId = rota.Id, Sentido = SentidoViagem.Volta, Ordem = 1, Endereco = "Ponto C" });
        await db.SaveChangesAsync();
        var paradas = await db.Paradas.OrderBy(p => p.Id).ToListAsync();
        var service = new VinculoAlunoService(db);

        var ida = await service.DefinirAsync(aluno.Id, rota.Id, paradas[0].Id, SentidoViagem.Ida);
        Assert.True(ida.Sucesso);
        await db.SaveChangesAsync();

        var alteracao = await service.DefinirAsync(aluno.Id, rota.Id, paradas[1].Id, SentidoViagem.Ida);
        Assert.True(alteracao.Sucesso);
        await db.SaveChangesAsync();

        var volta = await service.DefinirAsync(aluno.Id, rota.Id, paradas[2].Id, SentidoViagem.Volta);
        Assert.True(volta.Sucesso);
        await db.SaveChangesAsync();

        var vinculos = await db.VinculosAluno.OrderBy(v => v.Id).ToListAsync();
        Assert.False(vinculos[0].Ativo);
        Assert.NotNull(vinculos[0].DataFim);
        Assert.True(vinculos[1].Ativo);
        Assert.True(vinculos[2].Ativo);
    }

    [Fact]
    public async Task VinculoRejeitaParadaDeOutraRotaOuSentido()
    {
        using var db = TestesInfra.CriarBanco();
        var (_, rota, _) = TestesInfra.CriarReferencias(db);
        var outraRota = new Rota { Nome = "Outra", Origem = "A", Destino = "B", Ativo = true };
        var aluno = new Aluno { Nome = "Aluno", Email = "aluno@easyvan.com", Ativo = true };
        db.Rotas.Add(outraRota);
        db.Alunos.Add(aluno);
        await db.SaveChangesAsync();
        var parada = new Parada
        {
            RotaId = outraRota.Id,
            Sentido = SentidoViagem.Volta,
            Ordem = 1,
            Endereco = "Ponto"
        };
        db.Paradas.Add(parada);
        await db.SaveChangesAsync();

        var result = await new VinculoAlunoService(db)
            .DefinirAsync(aluno.Id, rota.Id, parada.Id, SentidoViagem.Ida);

        Assert.False(result.Sucesso);
        Assert.Empty(db.VinculosAluno);
    }

    private sealed class HasherFake : IPasswordService
    {
        public string Hash(string senha) => $"hash:{senha}";
        public bool Verificar(string senha, string hash) => hash == Hash(senha);
        public bool PrecisaAtualizar(string hash) => false;
    }
}

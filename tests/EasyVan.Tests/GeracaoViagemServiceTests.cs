using EasyVan.Data;
using EasyVan.Models;
using EasyVan.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EasyVan.Tests;

public sealed class GeracaoViagemServiceTests
{
    [Fact]
    public async Task GeraViagemDaAgendaComAlocacaoEPresencaVinculada()
    {
        using var db = TestesInfra.CriarBanco();
        var (motorista, rota, van) = TestesInfra.CriarReferencias(db);
        var aluno = new Aluno { Nome = "Aluno de teste", Email = "aluno-fase3@easyvan.com", Ativo = true };
        db.Alunos.Add(aluno);
        var parada = new Parada
        {
            RotaId = rota.Id, Sentido = SentidoViagem.Ida, Ordem = 1, Endereco = "Rua de teste", Ativo = true
        };
        db.Paradas.Add(parada);
        await db.SaveChangesAsync();
        db.VinculosAluno.Add(new VinculoAluno
        {
            AlunoId = aluno.Id, RotaId = rota.Id, ParadaId = parada.Id,
            Sentido = SentidoViagem.Ida, Ativo = true
        });
        db.HorariosRota.Add(new HorarioRota
        {
            RotaId = rota.Id, Sentido = SentidoViagem.Ida,
            DiasSemana = DiasSemana.Terca, HorarioSaida = new TimeSpan(7, 30, 0),
            VanPadraoId = van.Id, Ativo = true
        });
        await db.SaveChangesAsync();
        var service = CriarServico(db);

        var resultado = await service.GerarPeriodoAsync(new DateTime(2026, 11, 10), new DateTime(2026, 11, 10));

        var gerada = Assert.Single(resultado.ViagensCriadas);
        Assert.Equal(1, resultado.TotalCriadas);
        Assert.Equal(rota.Id, gerada.RotaId);
        Assert.Equal(van.Id, gerada.VanId);
        Assert.Equal(motorista.Id, gerada.MotoristaId);
        Assert.Equal(SentidoViagem.Ida, gerada.Sentido);
        Assert.Equal(1, gerada.PresencasCriadas);
    }

    [Fact]
    public async Task ReexecutarGeracaoNaoDuplicaViagemDaMesmaAgendaENemDoMesmoDia()
    {
        using var db = TestesInfra.CriarBanco();
        var (_, rota, _) = TestesInfra.CriarReferencias(db);
        db.HorariosRota.Add(new HorarioRota
        {
            RotaId = rota.Id, Sentido = SentidoViagem.Volta,
            DiasSemana = DiasSemana.Terca, HorarioSaida = new TimeSpan(17, 0, 0), Ativo = true
        });
        await db.SaveChangesAsync();
        var service = CriarServico(db);
        var data = new DateTime(2026, 11, 10);

        var primeira = await service.GerarPeriodoAsync(data, data);
        var segunda = await service.GerarPeriodoAsync(data, data);

        Assert.Equal(1, primeira.TotalCriadas);
        Assert.Equal(0, primeira.JaExistiam);
        Assert.Equal(0, segunda.TotalCriadas);
        Assert.Equal(1, segunda.JaExistiam);
        Assert.Empty(segunda.ViagensCriadas);
    }

    [Fact]
    public async Task IgnoraAgendaInativaERotaInativaEDatasForaDosDiasConfigurados()
    {
        using var db = TestesInfra.CriarBanco();
        var (_, rotaAtiva, _) = TestesInfra.CriarReferencias(db);
        var rotaInativa = new Rota { Nome = "Inativa", Origem = "A", Destino = "B", Ativo = false };
        db.Rotas.Add(rotaInativa);
        await db.SaveChangesAsync();
        db.HorariosRota.AddRange(
            new HorarioRota
            {
                RotaId = rotaAtiva.Id, Sentido = SentidoViagem.Ida,
                DiasSemana = DiasSemana.Quarta, HorarioSaida = new TimeSpan(7, 0, 0), Ativo = true
            },
            new HorarioRota
            {
                RotaId = rotaAtiva.Id, Sentido = SentidoViagem.Volta,
                DiasSemana = DiasSemana.Terca, HorarioSaida = new TimeSpan(17, 0, 0), Ativo = false
            },
            new HorarioRota
            {
                RotaId = rotaInativa.Id, Sentido = SentidoViagem.Ida,
                DiasSemana = DiasSemana.Terca, HorarioSaida = new TimeSpan(8, 0, 0), Ativo = true
            });
        await db.SaveChangesAsync();
        var service = CriarServico(db);

        var resultado = await service.GerarPeriodoAsync(new DateTime(2026, 11, 10), new DateTime(2026, 11, 10));

        Assert.Equal(0, resultado.TotalCriadas);
        Assert.Empty(resultado.ViagensCriadas);
    }

    [Fact]
    public async Task GeraViagemSemVanQuandoAgendaNaoTemAlocacao()
    {
        using var db = TestesInfra.CriarBanco();
        var (_, rota, _) = TestesInfra.CriarReferencias(db);
        db.HorariosRota.Add(new HorarioRota
        {
            RotaId = rota.Id, Sentido = SentidoViagem.Ida,
            DiasSemana = DiasSemana.Terca, HorarioSaida = new TimeSpan(7, 0, 0), Ativo = true
        });
        await db.SaveChangesAsync();

        var resultado = await CriarServico(db).GerarPeriodoAsync(
            new DateTime(2026, 11, 10), new DateTime(2026, 11, 10));

        var gerada = Assert.Single(resultado.ViagensCriadas);
        Assert.Null(gerada.VanId);
        Assert.Null(gerada.MotoristaId);
    }

    private static GeracaoViagemService CriarServico(ApplicationDbContext db) =>
        new(db, new PresencaViagemService(db), TimeProvider.System);
}

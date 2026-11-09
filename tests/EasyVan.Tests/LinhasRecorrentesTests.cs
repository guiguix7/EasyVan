using EasyVan.Data;
using EasyVan.Models;
using EasyVan.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EasyVan.Tests;

public sealed class LinhasRecorrentesTests
{
    [Fact]
    public async Task FiltroDeOrganizacaoIsolaLinhasEntreContextos()
    {
        using var organizacao1 = TestesInfra.CriarBanco();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(organizacao1.Database.GetDbConnection()).Options;
        var rota1 = new Rota { Nome = "Rota Org 1", Origem = "Origem", Destino = "Destino" };
        organizacao1.Rotas.Add(rota1);
        await organizacao1.SaveChangesAsync();
        organizacao1.Linhas.Add(new Linha { Nome = "Linha Org 1", RotaLegadaId = rota1.Id });
        await organizacao1.SaveChangesAsync();

        await using (var organizacao2 = new ApplicationDbContext(options, 2))
        {
            var rota2 = new Rota { Nome = "Rota Org 2", Origem = "Origem", Destino = "Destino" };
            organizacao2.Rotas.Add(rota2);
            await organizacao2.SaveChangesAsync();
            organizacao2.Linhas.Add(new Linha { Nome = "Linha Org 2", RotaLegadaId = rota2.Id });
            await organizacao2.SaveChangesAsync();
            Assert.Equal("Linha Org 2", await organizacao2.Linhas.Select(l => l.Nome).SingleAsync());
        }

        Assert.Equal("Linha Org 1", await organizacao1.Linhas.Select(l => l.Nome).SingleAsync());
    }

    [Fact]
    public void HashConfirmadosIgnoraOrdemDosIdsEMudaComSentidoOuData()
    {
        var data = new DateTime(2026, 11, 9);
        var primeiro = RoteirizacaoService.CriarHashConfirmados(new[] { 3, 1, 2, 1 }, data, SentidoViagem.Ida);
        var mesmaComOutraOrdem = RoteirizacaoService.CriarHashConfirmados(new[] { 2, 1, 3 }, data, SentidoViagem.Ida);

        Assert.Equal(primeiro, mesmaComOutraOrdem);
        Assert.NotEqual(primeiro, RoteirizacaoService.CriarHashConfirmados(new[] { 1, 2, 3 }, data, SentidoViagem.Volta));
        Assert.NotEqual(primeiro, RoteirizacaoService.CriarHashConfirmados(new[] { 1, 2, 3 }, data.AddDays(1), SentidoViagem.Ida));
    }

    [Fact]
    public async Task RoteirizadorFalsoOrdenaPorProximidadeSemRede()
    {
        var fake = new FakeRoteirizador();
        var origem = new Coordenada(-23.55, -46.63);
        var paradas = new[]
        {
            new ParadaRoteavel(20, "aluno 20", new Coordenada(-23.60, -46.63)),
            new ParadaRoteavel(10, "aluno 10", new Coordenada(-23.56, -46.63))
        };

        var resultado = await fake.CalcularAsync(origem, new Coordenada(-23.70, -46.63), paradas,
            DateTimeOffset.UtcNow, otimizarOrdem: true);

        Assert.Equal(new[] { 10, 20 }, resultado.Paradas.Select(p => p.UsuarioId));
        Assert.True(resultado.DuracaoSegundos > 0);
    }

    [Fact]
    public async Task GeracaoDeLinhaEIdempotenteEIniciaPresencaPendente()
    {
        using var db = TestesInfra.CriarBanco();
        var (motorista, rota, van) = TestesInfra.CriarReferencias(db);
        var usuario = new Usuarios
        {
            Nome = "Aluno teste", Email = "aluno-teste@easyvan.local", RoleManager = "Aluno", Ativo = true,
            Lat = -23.56, Lng = -46.63
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        var aluno = new Aluno { Nome = usuario.Nome, Email = usuario.Email, Ativo = true, UsuarioId = usuario.Id };
        db.Alunos.Add(aluno);
        await db.SaveChangesAsync();
        var linha = new Linha
        {
            RotaLegadaId = rota.Id, Nome = "Fatec", Origem = "origem", OrigemLat = -23.55, OrigemLng = -46.63,
            Destino = "destino", DestinoLat = -23.65, DestinoLng = -46.64,
            DiasSemana = DiasSemana.Segunda, TemIda = true, TemVolta = false
        };
        db.Linhas.Add(linha);
        await db.SaveChangesAsync();
        db.LinhasVans.Add(new LinhaVan { LinhaId = linha.Id, VanId = van.Id, MotoristaId = motorista.Id });
        db.LinhasPassageiros.Add(new LinhaPassageiro { LinhaId = linha.Id, VanId = van.Id, UsuarioId = usuario.Id });
        await db.SaveChangesAsync();

        var gerador = new GeracaoViagensLinhaService(db, TimeProvider.System);
        var data = new DateTime(2026, 11, 9);
        var primeira = await gerador.GerarPeriodoAsync(data, data);
        var segunda = await gerador.GerarPeriodoAsync(data, data);

        Assert.Equal(new ResumoViagensLinha(1, 0), primeira);
        Assert.Equal(new ResumoViagensLinha(0, 1), segunda);
        var viagem = await db.Viagens.Include(v => v.Presencas).SingleAsync(v => v.LinhaId == linha.Id);
        Assert.Single(viagem.Presencas);
        Assert.Equal(StatusPresenca.SemResposta, viagem.Presencas.Single().Status);
    }

    [Fact]
    public async Task LinhaRejeitaPassageirosAcimaDaCapacidadeDaVan()
    {
        using var db = TestesInfra.CriarBanco();
        var (motorista, _, van) = TestesInfra.CriarReferencias(db);
        var ids = new List<int>();
        for (var i = 1; i <= 13; i++)
        {
            var usuario = new Usuarios
            {
                Nome = $"Aluno {i}", Email = $"aluno{i}@easyvan.local", RoleManager = "Aluno", Ativo = true,
                Lat = -23.5 - i / 1000d, Lng = -46.63
            };
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();
            ids.Add(usuario.Id);
            db.Alunos.Add(new Aluno { Nome = usuario.Nome, Email = usuario.Email, Ativo = true, UsuarioId = usuario.Id });
        }
        await db.SaveChangesAsync();

        var formulario = new LinhaFormViewModel
        {
            Nome = "Fatec", Destino = "destino", DestinoLat = -23.6, DestinoLng = -46.6,
            Origem = "origem", OrigemLat = -23.5, OrigemLng = -46.6,
            DiasSemana = DiasSemana.Segunda, TemIda = true, TemVolta = false,
            PassageirosIds = ids, VansIds = new List<int> { van.Id },
            MotoristasPorVan = new Dictionary<int, int?> { [van.Id] = motorista.Id }
        };

        var resultado = await new LinhaService(db).SalvarAsync(formulario);

        Assert.False(resultado.Sucesso);
        Assert.Equal(13, resultado.Passageiros);
        Assert.Equal(van.Capacity, resultado.Capacidade);
        Assert.Contains("adicione outra van", resultado.Erro, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(db.Linhas);
    }

    [Fact]
    public async Task FalhaDoGoogleGeraOrdemAproximadaESeguraNovasChamadasPorCincoMinutos()
    {
        using var db = TestesInfra.CriarBanco();
        var (motorista, rota, van) = TestesInfra.CriarReferencias(db);
        var usuario = new Usuarios
        {
            Nome = "Aluno teste", Email = "aluno-rota@easyvan.local", RoleManager = "Aluno", Ativo = true,
            Lat = -23.56, Lng = -46.63
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        var aluno = new Aluno { Nome = usuario.Nome, Email = usuario.Email, Ativo = true, UsuarioId = usuario.Id };
        db.Alunos.Add(aluno);
        await db.SaveChangesAsync();
        var linha = new Linha
        {
            RotaLegadaId = rota.Id, Nome = "Fatec", Origem = "origem", OrigemLat = -23.55, OrigemLng = -46.63,
            Destino = "destino", DestinoLat = -23.65, DestinoLng = -46.64,
            DiasSemana = DiasSemana.Segunda, TemIda = true, TemVolta = false
        };
        db.Linhas.Add(linha);
        await db.SaveChangesAsync();
        db.LinhasVans.Add(new LinhaVan { LinhaId = linha.Id, VanId = van.Id, MotoristaId = motorista.Id });
        db.LinhasPassageiros.Add(new LinhaPassageiro { LinhaId = linha.Id, VanId = van.Id, UsuarioId = usuario.Id });
        await db.SaveChangesAsync();
        var data = new DateTime(2026, 11, 9);
        await new GeracaoViagensLinhaService(db, TimeProvider.System).GerarPeriodoAsync(data, data);
        var viagem = await db.Viagens.Include(v => v.Presencas).SingleAsync(v => v.LinhaId == linha.Id);
        viagem.Presencas.Single().Status = StatusPresenca.Confirmado;
        await db.SaveChangesAsync();

        var apiIndisponivel = new RoteirizadorIndisponivel();
        var service = new RoteirizacaoService(db, apiIndisponivel, TimeProvider.System,
            new PrazoPresencaOptions(), NullLogger<RoteirizacaoService>.Instance);
        var primeiro = await service.RecalcularSeNecessarioAsync(viagem.Id);
        var segundo = await service.RecalcularSeNecessarioAsync(viagem.Id);

        Assert.False(primeiro.Reutilizada);
        Assert.True(segundo.Reutilizada);
        Assert.Equal(1, apiIndisponivel.Chamadas);
        Assert.Equal("Fallback", (await db.Viagens.FindAsync(viagem.Id))!.StatusRoteirizacao);
        Assert.Single(await db.ViagensParadas.Where(p => p.ViagemId == viagem.Id).ToListAsync());
    }

    private sealed class RoteirizadorIndisponivel : IRoteirizador
    {
        public int Chamadas { get; private set; }

        public Task<ResultadoRoteirizacao> CalcularAsync(Coordenada origem, Coordenada destino,
            IReadOnlyList<ParadaRoteavel> paradas, DateTimeOffset partidaEstimada, bool otimizarOrdem,
            CancellationToken cancellationToken = default)
        {
            Chamadas++;
            throw new HttpRequestException("Serviço indisponível.");
        }
    }
}

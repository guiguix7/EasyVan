using EasyVan.Models;
using EasyVan.Services;
using Xunit;

namespace EasyVan.Tests;

public sealed class CentroOperacoesServiceTests
{
    [Fact]
    public async Task ObterPainelAsync_CarregaOcupacaoSQLiteSemOrdenarTimeSpanNoBanco()
    {
        using var db = TestesInfra.CriarBanco();
        var (motorista, rota, van) = TestesInfra.CriarReferencias(db);
        var viagem = new Viagem
        {
            VanId = van.Id,
            MotoristaId = motorista.Id,
            RotaId = rota.Id,
            Data = DateTime.SpecifyKind(ViagemAgenda.AgoraLocal().Date.AddDays(1), DateTimeKind.Unspecified),
            HoraInicio = new TimeSpan(7, 0, 0),
            Status = "Agendada"
        };
        db.Viagens.Add(viagem);
        await db.SaveChangesAsync();

        var painel = await new CentroOperacoesService(db, new ValidacaoOperacional(db)).ObterPainelAsync();

        var ocupacao = Assert.Single(painel.OcupacaoProximasViagens);
        Assert.Equal(viagem.Id, ocupacao.Viagem.Id);
        Assert.Equal(0, ocupacao.Confirmados);
        Assert.Equal(van.Capacity, ocupacao.Capacidade);
    }
}

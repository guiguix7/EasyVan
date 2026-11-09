using EasyVan.Models;
using EasyVan.Services;
using Xunit;

namespace EasyVan.Tests;

public sealed class PrazoPresencaServiceTests
{
    [Fact]
    public void IdaPermiteAlteracaoAte21hDoDiaAnteriorEmSaoPaulo()
    {
        var viagem = new Viagem
        {
            Data = new DateTime(2026, 11, 10),
            Sentido = SentidoViagem.Ida,
            HoraInicio = new TimeSpan(7, 0, 0)
        };
        var servico = CriarServico(new DateTimeOffset(2026, 11, 10, 0, 0, 0, TimeSpan.Zero));

        var resultado = servico.Avaliar(viagem);

        Assert.True(resultado.PodeAlterar);
        Assert.Equal(new DateTime(2026, 11, 9, 21, 0, 0), resultado.LimiteLocal);
    }

    [Fact]
    public void IdaBloqueiaUmInstanteDepoisDoPrazo()
    {
        var viagem = new Viagem
        {
            Data = new DateTime(2026, 11, 10),
            Sentido = SentidoViagem.Ida,
            HoraInicio = new TimeSpan(7, 0, 0)
        };
        var servico = CriarServico(new DateTimeOffset(2026, 11, 10, 0, 0, 1, TimeSpan.Zero));

        var resultado = servico.Avaliar(viagem);

        Assert.False(resultado.PodeAlterar);
        Assert.Equal("O prazo para responder esta viagem encerrou.", resultado.Mensagem);
    }

    [Fact]
    public void VoltaPermiteAlteracaoExatamenteDuasHorasAntesDaSaida()
    {
        var viagem = new Viagem
        {
            Data = new DateTime(2026, 11, 10),
            Sentido = SentidoViagem.Volta,
            HoraInicio = new TimeSpan(17, 0, 0)
        };
        var servico = CriarServico(new DateTimeOffset(2026, 11, 10, 18, 0, 0, TimeSpan.Zero));

        var resultado = servico.Avaliar(viagem);

        Assert.True(resultado.PodeAlterar);
        Assert.Equal(new DateTime(2026, 11, 10, 15, 0, 0), resultado.LimiteLocal);
    }

    [Fact]
    public void VoltaBloqueiaAposDuasHorasAntesDaSaida()
    {
        var viagem = new Viagem
        {
            Data = new DateTime(2026, 11, 10),
            Sentido = SentidoViagem.Volta,
            HoraInicio = new TimeSpan(17, 0, 0)
        };
        var servico = CriarServico(new DateTimeOffset(2026, 11, 10, 18, 0, 1, TimeSpan.Zero));

        Assert.False(servico.Avaliar(viagem).PodeAlterar);
    }

    [Fact]
    public void PrazoDaIdaPodeSerConfigurado()
    {
        var viagem = new Viagem
        {
            Data = new DateTime(2026, 11, 10),
            Sentido = SentidoViagem.Ida,
            HoraInicio = new TimeSpan(7, 0, 0)
        };
        var options = new PrazoPresencaOptions { HorarioLimiteIda = new TimeSpan(20, 30, 0) };
        var service = new PrazoPresencaService(options, new RelogioFalso(
            new DateTimeOffset(2026, 11, 9, 23, 30, 1, TimeSpan.Zero)));

        var resultado = service.Avaliar(viagem);

        Assert.False(resultado.PodeAlterar);
        Assert.Equal(new DateTime(2026, 11, 9, 20, 30, 0), resultado.LimiteLocal);
    }

    private static PrazoPresencaService CriarServico(DateTimeOffset agoraUtc) =>
        new(new PrazoPresencaOptions(), new RelogioFalso(agoraUtc));

    private sealed class RelogioFalso(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }
}

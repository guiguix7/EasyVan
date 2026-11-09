using EasyVan.Models;
using EasyVan.Services;
using Xunit;

namespace EasyVan.Tests;

public sealed class Fase4MotoristaTests
{
    [Fact]
    public void AgruparUnePassageirosDoMesmoPontoESegueOrdemDeParada()
    {
        var hora = DateTimeOffset.Parse("2026-11-09T10:00:00Z");
        var paradas = new[]
        {
            new ViagemParada { Ordem = 2, EnderecoEmbarque = "Rua A, 10", LatitudeEmbarque = -23.5, LongitudeEmbarque = -46.6,
                EtaPrevisto = hora.AddMinutes(10), Usuario = new Usuarios { Nome = "Bruno" } },
            new ViagemParada { Ordem = 1, EnderecoEmbarque = "Rua B, 20", LatitudeEmbarque = -23.6, LongitudeEmbarque = -46.7,
                EtaPrevisto = hora, Usuario = new Usuarios { Nome = "Ana" } },
            new ViagemParada { Ordem = 3, EnderecoEmbarque = "Rua A, 10", LatitudeEmbarque = -23.5, LongitudeEmbarque = -46.6,
                EtaPrevisto = hora.AddMinutes(12), Usuario = new Usuarios { Nome = "Carla" } }
        };

        var grupos = AgrupamentoParadasMotorista.Agrupar(paradas);

        Assert.Equal(2, grupos.Count);
        Assert.Equal(new[] { "Ana" }, grupos[0].Passageiros);
        Assert.Equal(new[] { "Bruno", "Carla" }, grupos[1].Passageiros);
        Assert.Equal(2, grupos[1].Ordem);
        Assert.Equal("Rua A, 10", grupos[1].Endereco);
    }

    [Fact]
    public void LinkMapsUsaPontoOperacionalEDivideEmTrechosDeAteTresParadas()
    {
        var linha = new Linha { OrigemLat = -23.5, OrigemLng = -46.6, DestinoLat = -23.8, DestinoLng = -46.9 };
        var paradas = Enumerable.Range(1, 4).Select(i => new ViagemParada
        {
            Ordem = i,
            EnderecoEmbarque = $"Ponto {i}",
            LatitudeEmbarque = -23.5 - i / 100d,
            LongitudeEmbarque = -46.6 - i / 100d,
            Usuario = new Usuarios { Nome = $"Aluno {i}", Endereco = "Endereço residencial não deve ser usado" }
        }).ToArray();

        var trechos = GoogleMapsLinkService.CriarTrechos(linha, SentidoViagem.Ida, paradas);

        Assert.Equal(2, trechos.Count);
        Assert.All(trechos, trecho => Assert.Contains("origin=", trecho.Url));
        Assert.Contains("origin=-23.5%2C-46.6", trechos[0].Url);
        Assert.Contains("-23.51%2C-46.61", trechos[0].Url);
        Assert.Contains("origin=-23.54%2C-46.64", trechos[1].Url);
        Assert.DoesNotContain("Endere%C3%A7o%20residencial", string.Join(" ", trechos.Select(t => t.Url)));
        Assert.Equal("Próximo trecho", trechos[1].Botao);
    }

    [Fact]
    public void EnderecoResidencialSemPontoOperacionalNaoGeraParadaDeRota()
    {
        var parada = new ViagemParada
        {
            Ordem = 1,
            Usuario = new Usuarios { Nome = "Aluno", Endereco = "Rua residencial", Lat = -23.5, Lng = -46.6 }
        };

        Assert.Empty(AgrupamentoParadasMotorista.Agrupar(new[] { parada }));
        Assert.Empty(GoogleMapsLinkService.CriarTrechos(new Linha(), SentidoViagem.Ida, new[] { parada }));
    }
}

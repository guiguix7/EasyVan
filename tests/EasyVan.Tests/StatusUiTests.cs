using EasyVan.Models;
using Xunit;

namespace EasyVan.Tests;

public class StatusUiTests
{
    [Theory]
    [InlineData("Planejada", "Planejada", "ev-badge--info")]
    [InlineData("Confirmada", "Confirmada", "ev-badge--info")]
    [InlineData("Agendada", "Agendada", "ev-badge--info")]
    [InlineData("EmAndamento", "Em andamento", "ev-badge--warning")]
    [InlineData("Finalizada", "Finalizada", "ev-badge--success")]
    [InlineData("Cancelada", "Cancelada", "ev-badge--neutral")]
    public void Label_RetornaRotuloECssClassEsperados(string entrada, string rotuloEsperado, string cssEsperado)
    {
        // Act
        var rotulo = StatusUi.Label(entrada);
        var css = StatusUi.CssClass(entrada);

        // Assert
        Assert.Equal(rotuloEsperado, rotulo);
        Assert.Equal(cssEsperado, css);
    }

    [Fact]
    public void Label_ValorNulo_RetornaNaoInformado()
    {
        // Act
        var rotulo = StatusUi.Label(null);

        // Assert
        Assert.Equal("Não informado", rotulo);
    }

    [Fact]
    public void Label_ValorVazio_RetornaNaoInformado()
    {
        // Act
        var rotulo = StatusUi.Label("");

        // Assert
        Assert.Equal("Não informado", rotulo);
    }

    [Fact]
    public void Label_IgnoraMaiusculasMinusculas()
    {
        // Act
        var rotulo = StatusUi.Label("PLANEJADA");
        var css = StatusUi.CssClass("confirmada");

        // Assert
        Assert.Equal("Planejada", rotulo);
        Assert.Equal("ev-badge--info", css);
    }
}
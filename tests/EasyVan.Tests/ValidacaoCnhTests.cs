using EasyVan.Services;
using Xunit;

namespace EasyVan.Tests;

public sealed class ValidacaoCnhTests
{
    [Theory]
    [InlineData("61727865535")]
    [InlineData("617.278.655-35")]
    // Caso-limite: primeiro resto 10, então o segundo dígito recebe desconto de 2.
    [InlineData("00000001801")]
    public void CnhValida_AceitaDigitosVerificadoresCorretos(string cnh)
    {
        Assert.True(ValidacaoBrasil.CnhValida(cnh));
    }

    [Theory]
    [InlineData("61727865536")]
    [InlineData("11111111111")]
    // Esta base gera segundo dígito negativo após o desconto e não pode ser válida.
    [InlineData("00000009300")]
    [InlineData("123")]
    public void CnhValida_RejeitaNumeroInvalido(string cnh)
    {
        Assert.False(ValidacaoBrasil.CnhValida(cnh));
    }

    [Fact]
    public void CnhValida_AceitaCampoVazioPorqueCadastroMantemCnhOpcional()
    {
        Assert.True(ValidacaoBrasil.CnhValida(null));
        Assert.True(ValidacaoBrasil.CnhValida(" "));
    }

    [Fact]
    public void NormalizarCnh_MantemSomenteDigitos()
    {
        Assert.Equal("61727865535", ValidacaoBrasil.NormalizarCnh("617.278.655-35"));
    }
}

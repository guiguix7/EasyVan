using EasyVan.Models;
using Xunit;

namespace EasyVan.Tests;

public class UsuariosRoleTests
{
    [Theory]
    [InlineData("Admin", "Administrador")]
    [InlineData("Administrador", "Administrador")]
    [InlineData("Motorista", "Motorista")]
    [InlineData("Aluno", "Aluno")]
    [InlineData("Usuario", "Aluno")]
    public void NormalizeRole_ConverteValoresEsperados(string entrada, string esperado)
    {
        var resultado = Usuarios.NormalizeRole(entrada);

        Assert.Equal(esperado, resultado);
    }
}

using System.ComponentModel.DataAnnotations;
using EasyVan.Models;
using Xunit;

namespace EasyVan.Tests;

public class PasswordPolicyTests
{
    [Theory]
    [InlineData("123456", true)]
    [InlineData("12345678901234567890", true)]
    [InlineData("12345", false)]
    [InlineData("123456789012345678901", false)]
    public void Usuarios_ValidaSenhaEntreSeisEVinteCaracteres(string senha, bool esperado)
    {
        var usuario = new Usuarios
        {
            Nome = "Usuário de teste",
            Email = "teste@example.com",
            RoleManager = "Aluno",
            PasswordHasher = senha
        };
        var erros = new List<ValidationResult>();
        var valido = Validator.TryValidateObject(usuario, new ValidationContext(usuario), erros, validateAllProperties: true);

        Assert.Equal(esperado, valido);
    }

    [Theory]
    [InlineData("Administrador", true)]
    [InlineData("Motorista", true)]
    [InlineData("Aluno", true)]
    [InlineData("Admin", false)]
    [InlineData("SuperAdmin", false)]
    [InlineData("", false)]
    public void IsSupportedRole_AceitaSomentePapeisDoSistema(string? role, bool esperado)
    {
        Assert.Equal(esperado, Usuarios.IsSupportedRole(role));
    }
}

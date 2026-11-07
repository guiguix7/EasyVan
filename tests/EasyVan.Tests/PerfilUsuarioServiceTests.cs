using System.Threading.Tasks;
using EasyVan.Models;
using EasyVan.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EasyVan.Tests;

public class PerfilUsuarioServiceTests
{
    [Fact]
    public async Task ContaDeAlunoCriaPerfilVinculado()
    {
        using var db = TestesInfra.CriarBanco();
        var service = new PerfilUsuarioService(db);

        var usuario = new Usuarios
        {
            Nome = "Ana Souza",
            Email = "ana.souza@easyvan.com",
            RoleManager = "Aluno"
        };

        var resultado = await service.GarantirPerfilAsync(
            usuario,
            new DadosPerfilUsuario("11912345678", "Rua A, 10", null, true));

        await db.SaveChangesAsync();

        Assert.True(resultado.Sucesso);
        var aluno = await db.Alunos.SingleAsync();
        Assert.Equal(usuario.Id, aluno.UsuarioId);
        Assert.Equal("ana.souza@easyvan.com", aluno.Email);
        Assert.Equal("Rua A, 10", aluno.Endereco);
    }

    [Fact]
    public async Task ContaDeMotoristaCriaPerfilComCnh()
    {
        using var db = TestesInfra.CriarBanco();
        var service = new PerfilUsuarioService(db);

        var usuario = new Usuarios
        {
            Nome = "Carlos Lima",
            Email = "carlos.lima@easyvan.com",
            RoleManager = "Motorista"
        };

        var resultado = await service.GarantirPerfilAsync(
            usuario,
            new DadosPerfilUsuario(null, null, "12345678901", true));

        await db.SaveChangesAsync();

        Assert.True(resultado.Sucesso);
        var motorista = await db.Motoristas.SingleAsync();
        Assert.Equal(usuario.Id, motorista.UsuarioId);
        Assert.Equal("12345678901", motorista.CNH);
    }

    [Fact]
    public async Task PerfilExistenteSemContaEhVinculadoPeloEmail()
    {
        using var db = TestesInfra.CriarBanco();
        var aluno = new Aluno { Nome = "Aluno Antigo", Email = "antigo@easyvan.com", Ativo = true };
        db.Alunos.Add(aluno);
        await db.SaveChangesAsync();

        var usuario = new Usuarios
        {
            Nome = "Aluno Antigo",
            Email = "antigo@easyvan.com",
            RoleManager = "Aluno"
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        var resultado = await new PerfilUsuarioService(db).GarantirPerfilAsync(
            usuario,
            new DadosPerfilUsuario(null, null, null, true));

        await db.SaveChangesAsync();

        Assert.True(resultado.Sucesso);
        var atualizado = await db.Alunos.SingleAsync();
        Assert.Equal(usuario.Id, atualizado.UsuarioId);
    }

    [Fact]
    public async Task EmailDeMotoristaNaoPodeVirarAluno()
    {
        using var db = TestesInfra.CriarBanco();
        db.Motoristas.Add(new Motorista
        {
            Nome = "Motorista Existente",
            Email = "pessoa@easyvan.com",
            CNH = "12345678901",
            Ativo = true
        });
        await db.SaveChangesAsync();

        var usuario = new Usuarios
        {
            Nome = "Pessoa",
            Email = "pessoa@easyvan.com",
            RoleManager = "Aluno"
        };

        var resultado = await new PerfilUsuarioService(db).GarantirPerfilAsync(
            usuario,
            new DadosPerfilUsuario(null, null, null, true));

        Assert.False(resultado.Sucesso);
        Assert.Contains("motorista", resultado.Erro!, System.StringComparison.OrdinalIgnoreCase);
        Assert.Empty(db.Alunos);
    }

    [Fact]
    public async Task PerfilVinculadoAOutraContaEhRejeitado()
    {
        using var db = TestesInfra.CriarBanco();
        var contaExistente = new Usuarios { Nome = "Conta 1", Email = "um@easyvan.com", RoleManager = "Aluno" };
        db.Usuarios.Add(contaExistente);
        await db.SaveChangesAsync();

        db.Alunos.Add(new Aluno
        {
            Nome = "Conta 1",
            Email = "compartilhado@easyvan.com",
            Ativo = true,
            UsuarioId = contaExistente.Id
        });
        await db.SaveChangesAsync();

        var outraConta = new Usuarios
        {
            Nome = "Conta 2",
            Email = "compartilhado@easyvan.com",
            RoleManager = "Aluno"
        };

        var resultado = await new PerfilUsuarioService(db).GarantirPerfilAsync(
            outraConta,
            new DadosPerfilUsuario(null, null, null, true));

        Assert.False(resultado.Sucesso);
        Assert.Contains("outra conta", resultado.Erro!, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdministradorNaoCriaPerfilOperacional()
    {
        using var db = TestesInfra.CriarBanco();

        var usuario = new Usuarios
        {
            Nome = "Admin",
            Email = "admin@easyvan.com",
            RoleManager = "Administrador"
        };

        var resultado = await new PerfilUsuarioService(db).GarantirPerfilAsync(
            usuario,
            new DadosPerfilUsuario(null, null, null, true));

        await db.SaveChangesAsync();

        Assert.True(resultado.Sucesso);
        Assert.Empty(db.Alunos);
        Assert.Empty(db.Motoristas);
    }
}

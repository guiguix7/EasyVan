using EasyVan.Controllers;
using EasyVan.Data;
using EasyVan.Models;
using EasyVan.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EasyVan.Tests;

public sealed class UsuariosAtivosTests
{
    [Fact]
    public async Task AdministradorNaoPodeDesativarPropriaConta()
    {
        using var db = TestesInfra.CriarBanco();
        var usuario = CriarAdministrador(1, "admin@easyvan.com");
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        var controller = CriarController(db, usuario.Id);
        var resultado = await controller.Edit(usuario.Id, new UsuarioEditViewModel
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email,
            RoleManager = "Administrador",
            Ativo = false
        });

        Assert.IsType<ViewResult>(resultado);
        Assert.False(controller.ModelState.IsValid);
        Assert.True((await db.Usuarios.FindAsync(usuario.Id))!.Ativo);
    }

    [Fact]
    public async Task AdministradorNaoPodeDesativarOUltimoAdministradorAtivo()
    {
        using var db = TestesInfra.CriarBanco();
        var usuario = CriarAdministrador(2, "alvo@easyvan.com");
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        var controller = CriarController(db, usuarioId: 1);
        var resultado = await controller.Edit(usuario.Id, new UsuarioEditViewModel
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email,
            RoleManager = "Administrador",
            Ativo = false
        });

        Assert.IsType<ViewResult>(resultado);
        Assert.False(controller.ModelState.IsValid);
        Assert.True((await db.Usuarios.FindAsync(usuario.Id))!.Ativo);
    }

    private static UsuariosController CriarController(ApplicationDbContext db, int usuarioId)
    {
        var controller = new UsuariosController(
            db,
            new HasherFake(),
            new PerfilUsuarioService(db),
            NullLogger<UsuariosController>.Instance);
        TestesInfra.ConfigurarController(controller, "Administrador", usuarioId);
        return controller;
    }

    private static Usuarios CriarAdministrador(int id, string email) => new()
    {
        Id = id,
        Nome = "Administrador",
        Email = email,
        RoleManager = "Administrador",
        PasswordHasher = "hash",
        Tema = "claro",
        Ativo = true
    };

    private sealed class HasherFake : IPasswordService
    {
        public string Hash(string senha) => $"hash:{senha}";
        public bool Verificar(string senha, string hash) => hash == Hash(senha);
        public bool PrecisaAtualizar(string hash) => false;
    }
}

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using EasyVan.Data;
using EasyVan.Models;
using EasyVan.Services;
using Gerenciamento_de_Van.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace EasyVan.Tests;

public class VansControllerTests
{
    private static VansController CriarController(ApplicationDbContext db)
    {
        var controller = new VansController(db);
        TestesInfra.ConfigurarController(controller);
        return controller;
    }

    private static async Task AdicionarSegundaVan(ApplicationDbContext db)
    {
        var rota = new Rota
        {
            Nome = "Zona Leste - Centro",
            Origem = "Zona Leste",
            Destino = "Centro",
            Ativo = true
        };
        db.Rotas.Add(rota);
        db.Vans.Add(new Van
        {
            Plate = "DEF-5678",
            Modelo = "Master",
            Driver = "Maria Oliveira",
            Capacity = 10,
            Route = rota.Nome,
            Schedule = "06:30",
            Status = "Manutenção"
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task BuscaComUmCaractereEncontraVan()
    {
        using var db = TestesInfra.CriarBanco();
        TestesInfra.CriarReferencias(db);
        await AdicionarSegundaVan(db);

        var controller = CriarController(db);
        var resultado = await controller.Index(new VanFiltro { Busca = "j" });

        var view = Assert.IsType<ViewResult>(resultado);
        var model = Assert.IsType<VanIndexViewModel>(view.Model);

        Assert.Single(model.Vans);
        Assert.Equal("ABC-1234", model.Vans[0].Plate);
    }

    [Fact]
    public async Task BuscaPorModeloEncontraVan()
    {
        using var db = TestesInfra.CriarBanco();
        TestesInfra.CriarReferencias(db);
        await AdicionarSegundaVan(db);

        var controller = CriarController(db);
        var resultado = await controller.Index(new VanFiltro { Busca = "sprinter" });

        var model = Assert.IsType<VanIndexViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Single(model.Vans);
        Assert.Equal("ABC-1234", model.Vans[0].Plate);
    }

    [Fact]
    public async Task BuscaAceitaPlacaSemHifen()
    {
        using var db = TestesInfra.CriarBanco();
        TestesInfra.CriarReferencias(db);

        var controller = CriarController(db);
        var resultado = await controller.Index(new VanFiltro { Busca = "abc1234" });

        var model = Assert.IsType<VanIndexViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Single(model.Vans);
    }

    [Fact]
    public async Task CriarVanComMotoristaInexistenteExibeErro()
    {
        using var db = TestesInfra.CriarBanco();
        var (_, rota, _) = TestesInfra.CriarReferencias(db);

        var controller = CriarController(db);
        var resultado = await controller.Create(new VanFormViewModel
        {
            Plate = "XYZ-9999",
            Modelo = "Sprinter",
            Capacity = 15,
            Status = "Operacional",
            MotoristaId = 999,
            RotaId = rota.Id,
            Horarios = new List<string> { "07:00" }
        });

        Assert.IsType<ViewResult>(resultado);
        Assert.True(controller.ModelState.ContainsKey(nameof(VanFormViewModel.MotoristaId)));
        Assert.Equal(1, db.Vans.Count());
    }

    [Fact]
    public async Task CriarVanComPlacaDuplicadaExibeErro()
    {
        using var db = TestesInfra.CriarBanco();
        var (motorista, rota, _) = TestesInfra.CriarReferencias(db);

        var controller = CriarController(db);
        var resultado = await controller.Create(new VanFormViewModel
        {
            Plate = "abc-1234",
            Modelo = "Sprinter",
            Capacity = 15,
            Status = "Operacional",
            MotoristaId = motorista.Id,
            RotaId = rota.Id,
            Horarios = new List<string> { "07:00" }
        });

        Assert.IsType<ViewResult>(resultado);
        Assert.True(controller.ModelState.ContainsKey(nameof(VanFormViewModel.Plate)));
        Assert.Equal(1, db.Vans.Count());
    }

    [Fact]
    public async Task EdicaoExigeModelo()
    {
        using var db = TestesInfra.CriarBanco();
        var (motorista, rota, van) = TestesInfra.CriarReferencias(db);

        var model = new VanFormViewModel
        {
            Id = van.Id,
            Plate = van.Plate,
            Modelo = string.Empty,
            Capacity = van.Capacity,
            Status = van.Status,
            MotoristaId = motorista.Id,
            RotaId = rota.Id,
            Horarios = new List<string> { "07:00" }
        };

        var erros = new List<ValidationResult>();
        var valido = Validator.TryValidateObject(model, new ValidationContext(model), erros, validateAllProperties: true);

        Assert.False(valido);
        Assert.Contains(erros, e => e.MemberNames.Contains(nameof(VanFormViewModel.Modelo)));
    }

    [Fact]
    public void IdentificacaoUsaPlacaEModeloNuncaOId()
    {
        var van = new Van { Id = 42, Plate = "ABC-1234", Modelo = "Sprinter" };

        var identificacao = VanUi.Identificacao(van);

        Assert.Equal("ABC-1234 · Sprinter", identificacao);
        Assert.DoesNotContain("42", identificacao);
    }
}

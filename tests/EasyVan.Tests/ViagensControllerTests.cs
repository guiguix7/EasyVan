using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EasyVan.Controllers;
using EasyVan.Data;
using EasyVan.Models;
using EasyVan.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EasyVan.Tests;

public class ViagensControllerTests
{
    private static ViagensController CriarController(ApplicationDbContext db, AuditoriaFake? auditoria = null)
    {
        var controller = new ViagensController(
            db,
            new ValidacaoOperacional(db),
            auditoria ?? new AuditoriaFake(),
            NullLogger<ViagensController>.Instance);

        TestesInfra.ConfigurarController(controller);
        return controller;
    }

    [Fact]
    public async Task CriarViagemComMotoristaExistenteGravaViagemEPresencas()
    {
        using var db = TestesInfra.CriarBanco();
        var (motorista, rota, van) = TestesInfra.CriarReferencias(db);
        db.Alunos.AddRange(
            new Aluno { Nome = "Ativo", Email = "ativo@easyvan.com", Ativo = true },
            new Aluno { Nome = "Inativo", Email = "inativo@easyvan.com", Ativo = false });
        await db.SaveChangesAsync();

        var controller = CriarController(db);
        var amanha = DateTime.Today.AddDays(1);

        var resultado = await controller.Create(new ViagemFormViewModel
        {
            VanId = van.Id,
            MotoristaId = motorista.Id,
            RotaId = rota.Id,
            Data = amanha,
            DataChegada = amanha,
            HoraInicio = "08:00",
            HoraFim = "09:30"
        });

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(ViagensController.Details), redirect.ActionName);

        var viagem = await db.Viagens.SingleAsync();
        Assert.Equal(van.Id, viagem.VanId);
        Assert.Equal(motorista.Id, viagem.MotoristaId);
        Assert.Equal(new TimeSpan(8, 0, 0), viagem.HoraInicio!.Value);
        Assert.Equal(new TimeSpan(9, 30, 0), viagem.HoraFim!.Value);
        Assert.Equal("Agendada", viagem.Status);

        var presencas = db.PresencasViagem.ToList();
        Assert.Single(presencas);
        Assert.Equal("Ativo", presencas[0].Aluno!.Nome);
    }

    [Fact]
    public async Task CriarViagemComMotoristaInexistenteExibeErro()
    {
        using var db = TestesInfra.CriarBanco();
        var (_, rota, van) = TestesInfra.CriarReferencias(db);
        var controller = CriarController(db);
        var amanha = DateTime.Today.AddDays(1);

        var resultado = await controller.Create(new ViagemFormViewModel
        {
            VanId = van.Id,
            MotoristaId = 999,
            RotaId = rota.Id,
            Data = amanha,
            DataChegada = amanha,
            HoraInicio = "08:00",
            HoraFim = "09:00"
        });

        Assert.IsType<ViewResult>(resultado);
        Assert.True(controller.ModelState.ContainsKey(nameof(ViagemFormViewModel.MotoristaId)));
        Assert.Empty(db.Viagens);
    }

    [Fact]
    public async Task CriarViagemComDataPassadaEhBloqueada()
    {
        using var db = TestesInfra.CriarBanco();
        var (motorista, rota, van) = TestesInfra.CriarReferencias(db);
        var controller = CriarController(db);
        var ontem = DateTime.Today.AddDays(-1);

        var resultado = await controller.Create(new ViagemFormViewModel
        {
            VanId = van.Id,
            MotoristaId = motorista.Id,
            RotaId = rota.Id,
            Data = ontem,
            DataChegada = ontem,
            HoraInicio = "08:00",
            HoraFim = "09:00"
        });

        Assert.IsType<ViewResult>(resultado);
        Assert.Contains(
            controller.ModelState[string.Empty]!.Errors,
            e => e.ErrorMessage.Contains("anterior a hoje", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(db.Viagens);
    }

    [Fact]
    public async Task CriarViagemComChegadaAntesDaPartidaEhBloqueada()
    {
        using var db = TestesInfra.CriarBanco();
        var (motorista, rota, van) = TestesInfra.CriarReferencias(db);
        var controller = CriarController(db);
        var amanha = DateTime.Today.AddDays(2);

        var resultado = await controller.Create(new ViagemFormViewModel
        {
            VanId = van.Id,
            MotoristaId = motorista.Id,
            RotaId = rota.Id,
            Data = amanha,
            DataChegada = amanha,
            HoraInicio = "10:00",
            HoraFim = "09:00"
        });

        Assert.IsType<ViewResult>(resultado);
        Assert.Contains(
            controller.ModelState[string.Empty]!.Errors,
            e => e.ErrorMessage.Contains("posterior à partida", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(db.Viagens);
    }

    [Fact]
    public async Task ListagemOrdenaPorDataHorarioEId()
    {
        using var db = TestesInfra.CriarBanco();
        var (motorista, rota, van) = TestesInfra.CriarReferencias(db);

        var hoje = DateTime.Today;
        db.Viagens.AddRange(
            new Viagem { VanId = van.Id, MotoristaId = motorista.Id, RotaId = rota.Id, Data = hoje.AddDays(2), HoraInicio = new TimeSpan(8, 0, 0), HoraFim = new TimeSpan(9, 0, 0) },
            new Viagem { VanId = van.Id, MotoristaId = motorista.Id, RotaId = rota.Id, Data = hoje.AddDays(1), HoraInicio = new TimeSpan(9, 0, 0), HoraFim = new TimeSpan(10, 0, 0) },
            new Viagem { VanId = van.Id, MotoristaId = motorista.Id, RotaId = rota.Id, Data = hoje.AddDays(1), HoraInicio = new TimeSpan(7, 0, 0), HoraFim = new TimeSpan(8, 0, 0) });
        await db.SaveChangesAsync();

        var controller = CriarController(db);
        var resultado = await controller.Index(null, null);

        var view = Assert.IsType<ViewResult>(resultado);
        var viagens = Assert.IsAssignableFrom<IEnumerable<Viagem>>(view.Model).ToList();

        Assert.Equal(3, viagens.Count);
        Assert.Equal(new TimeSpan(7, 0, 0), viagens[0].HoraInicio!.Value);
        Assert.Equal(new TimeSpan(9, 0, 0), viagens[1].HoraInicio!.Value);
        Assert.Equal(hoje.AddDays(2).Date, viagens[2].Data.Date);
    }

    [Fact]
    public async Task IniciarEFinalizarRegistramHorariosReaisSemAlterarPrevistos()
    {
        using var db = TestesInfra.CriarBanco();
        var (motorista, rota, van) = TestesInfra.CriarReferencias(db);

        var viagem = new Viagem
        {
            VanId = van.Id,
            MotoristaId = motorista.Id,
            RotaId = rota.Id,
            Data = DateTime.Today.AddDays(1),
            HoraInicio = new TimeSpan(7, 0, 0),
            HoraFim = new TimeSpan(8, 0, 0),
            Status = "Agendada"
        };
        db.Viagens.Add(viagem);
        await db.SaveChangesAsync();

        var controller = CriarController(db);
        var antes = DateTime.UtcNow.AddSeconds(-1);

        var iniciar = await controller.Start(viagem.Id);
        Assert.IsType<RedirectToActionResult>(iniciar);

        await db.Entry(viagem).ReloadAsync();
        Assert.Equal("EmAndamento", viagem.Status);
        Assert.NotNull(viagem.InicioReal);
        Assert.True(viagem.InicioReal >= antes);
        Assert.Equal(new TimeSpan(7, 0, 0), viagem.HoraInicio!.Value);
        Assert.Equal(new TimeSpan(8, 0, 0), viagem.HoraFim!.Value);

        var finalizar = await controller.Finish(viagem.Id);
        Assert.IsType<RedirectToActionResult>(finalizar);

        await db.Entry(viagem).ReloadAsync();
        Assert.Equal("Finalizada", viagem.Status);
        Assert.NotNull(viagem.FimReal);
        Assert.True(viagem.FimReal >= viagem.InicioReal);
    }

    [Fact]
    public async Task EdicaoAtualizaPrevisaoEPreservaHorariosReais()
    {
        using var db = TestesInfra.CriarBanco();
        var (motorista, rota, van) = TestesInfra.CriarReferencias(db);

        var inicioReal = DateTime.UtcNow.AddHours(-1);
        var viagem = new Viagem
        {
            VanId = van.Id,
            MotoristaId = motorista.Id,
            RotaId = rota.Id,
            Data = DateTime.Today.AddDays(1),
            DataChegada = DateTime.Today.AddDays(1),
            HoraInicio = new TimeSpan(7, 0, 0),
            HoraFim = new TimeSpan(8, 0, 0),
            Status = "Confirmada",
            InicioReal = inicioReal
        };
        db.Viagens.Add(viagem);
        await db.SaveChangesAsync();

        var controller = CriarController(db);
        var novaData = DateTime.Today.AddDays(3);

        var resultado = await controller.Edit(viagem.Id, new ViagemFormViewModel
        {
            Id = viagem.Id,
            VanId = van.Id,
            MotoristaId = motorista.Id,
            RotaId = rota.Id,
            Data = novaData,
            DataChegada = novaData,
            HoraInicio = "10:00",
            HoraFim = "11:30"
        });

        Assert.IsType<RedirectToActionResult>(resultado);

        await db.Entry(viagem).ReloadAsync();
        Assert.Equal(novaData.Date, viagem.Data.Date);
        Assert.Equal(new TimeSpan(10, 0, 0), viagem.HoraInicio!.Value);
        Assert.Equal(new TimeSpan(11, 30, 0), viagem.HoraFim!.Value);
        Assert.Equal(inicioReal, viagem.InicioReal);
    }

    [Fact]
    public async Task EdicaoDeViagemEmAndamentoEhBloqueada()
    {
        using var db = TestesInfra.CriarBanco();
        var (motorista, rota, van) = TestesInfra.CriarReferencias(db);

        var viagem = new Viagem
        {
            VanId = van.Id,
            MotoristaId = motorista.Id,
            RotaId = rota.Id,
            Data = DateTime.Today,
            HoraInicio = new TimeSpan(7, 0, 0),
            HoraFim = new TimeSpan(8, 0, 0),
            Status = "EmAndamento"
        };
        db.Viagens.Add(viagem);
        await db.SaveChangesAsync();

        var controller = CriarController(db);
        var resultado = await controller.Edit(viagem.Id);

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal(nameof(ViagensController.Details), redirect.ActionName);
    }
}

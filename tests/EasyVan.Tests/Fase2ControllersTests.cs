using System.ComponentModel.DataAnnotations;
using System.Reflection;
using EasyVan.Controllers;
using EasyVan.Data;
using EasyVan.Models;
using EasyVan.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EasyVan.Tests;

public sealed class Fase2ControllersTests
{
    [Fact]
    public async Task CriacaoManualDeParadaRedirecionaParaCadastroDeLinha()
    {
        using var db = TestesInfra.CriarBanco();
        var (_, rota, _) = TestesInfra.CriarReferencias(db);
        var controller = CriarParadasController(db);

        var result = await controller.Create(new ParadaFormViewModel
        {
            RotaId = rota.Id,
            Sentido = SentidoViagem.Ida,
            Ordem = 1,
            Nome = "  Terminal  ",
            Endereco = " Rua Central ",
            HorarioPrevisto = new TimeSpan(7, 0, 0)
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Create", redirect.ActionName);
        Assert.Equal("Linhas", redirect.ControllerName);
        Assert.Empty(db.Paradas);
    }

    [Fact]
    public async Task CriacaoManualDeParadaNaoAlteraParadasLegadas()
    {
        using var db = TestesInfra.CriarBanco();
        var (_, rota, _) = TestesInfra.CriarReferencias(db);
        db.Paradas.Add(new Parada { RotaId = rota.Id, Sentido = SentidoViagem.Ida, Ordem = 1, Endereco = "Existente" });
        await db.SaveChangesAsync();
        var controller = CriarParadasController(db);

        var result = await controller.Create(new ParadaFormViewModel
        {
            RotaId = rota.Id,
            Sentido = SentidoViagem.Ida,
            Ordem = 1,
            Endereco = "Duplicada"
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Linhas", redirect.ControllerName);
        Assert.Single(db.Paradas);
    }

    [Fact]
    public async Task ParadasControllerImpedeMoverParadaComHistoricoDeVinculos()
    {
        using var db = TestesInfra.CriarBanco();
        var (_, rota, _) = TestesInfra.CriarReferencias(db);
        var outraRota = new Rota { Nome = "Outra rota", Origem = "A", Destino = "B", Ativo = true };
        db.Rotas.Add(outraRota);
        var parada = new Parada { RotaId = rota.Id, Sentido = SentidoViagem.Ida, Ordem = 1, Endereco = "Ponto" };
        db.Paradas.Add(parada);
        var aluno = new Aluno { Nome = "Aluno", Email = "aluno@easyvan.com", Ativo = true };
        db.Alunos.Add(aluno);
        await db.SaveChangesAsync();
        db.VinculosAluno.Add(new VinculoAluno
        {
            AlunoId = aluno.Id,
            RotaId = rota.Id,
            ParadaId = parada.Id,
            Sentido = SentidoViagem.Ida,
            Ativo = false
        });
        await db.SaveChangesAsync();
        var controller = CriarParadasController(db);

        var result = await controller.Edit(parada.Id, new ParadaFormViewModel
        {
            Id = parada.Id,
            RotaId = outraRota.Id,
            Sentido = SentidoViagem.Volta,
            Ordem = 1,
            Endereco = "Ponto transferido",
            Ativo = true
        });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Equal(rota.Id, (await db.Paradas.SingleAsync()).RotaId);
    }

    [Fact]
    public async Task CriacaoManualDeAgendaRedirecionaParaCadastroDeLinha()
    {
        using var db = TestesInfra.CriarBanco();
        var (_, rota, van) = TestesInfra.CriarReferencias(db);
        var controller = CriarHorariosController(db);

        var result = await controller.Create(new HorarioRotaFormViewModel
        {
            RotaId = rota.Id,
            Sentido = SentidoViagem.Volta,
            DiasSelecionados = new List<int> { (int)DiasSemana.Segunda, (int)DiasSemana.Quarta },
            HorarioSaida = new TimeSpan(17, 30, 0),
            VanPadraoId = van.Id
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Linhas", redirect.ControllerName);
        Assert.Empty(db.HorariosRota);
    }

    [Fact]
    public async Task CriacaoManualDeVinculoRedirecionaParaCadastroDeLinha()
    {
        using var db = TestesInfra.CriarBanco();
        var (_, rota, _) = TestesInfra.CriarReferencias(db);
        var aluno = new Aluno { Nome = "Aluno", Email = "aluno@easyvan.com", Ativo = true };
        db.Alunos.Add(aluno);
        db.Paradas.AddRange(
            new Parada { RotaId = rota.Id, Sentido = SentidoViagem.Ida, Ordem = 1, Endereco = "Ponto 1" },
            new Parada { RotaId = rota.Id, Sentido = SentidoViagem.Ida, Ordem = 2, Endereco = "Ponto 2" });
        await db.SaveChangesAsync();
        var paradas = await db.Paradas.OrderBy(p => p.Ordem).ToListAsync();

        var primeira = await CriarVinculosController(db).Create(new VinculoAlunoFormViewModel
        {
            AlunoId = aluno.Id,
            RotaId = rota.Id,
            ParadaId = paradas[0].Id,
            Sentido = SentidoViagem.Ida,
            DataInicio = DateTime.Today
        });
        Assert.Equal("Linhas", Assert.IsType<RedirectToActionResult>(primeira).ControllerName);

        var segunda = await CriarVinculosController(db).Create(new VinculoAlunoFormViewModel
        {
            AlunoId = aluno.Id,
            RotaId = rota.Id,
            ParadaId = paradas[1].Id,
            Sentido = SentidoViagem.Ida,
            DataInicio = DateTime.Today.AddDays(1)
        });
        Assert.Equal("Linhas", Assert.IsType<RedirectToActionResult>(segunda).ControllerName);
        Assert.Empty(db.VinculosAluno);
    }

    [Fact]
    public async Task VinculoControllerRejeitaParadaDeOutraRota()
    {
        using var db = TestesInfra.CriarBanco();
        var (_, rota, _) = TestesInfra.CriarReferencias(db);
        var outraRota = new Rota { Nome = "Outra rota", Origem = "A", Destino = "B", Ativo = true };
        var aluno = new Aluno { Nome = "Aluno", Email = "aluno@easyvan.com", Ativo = true };
        db.Rotas.Add(outraRota);
        db.Alunos.Add(aluno);
        await db.SaveChangesAsync();
        var parada = new Parada { RotaId = outraRota.Id, Sentido = SentidoViagem.Ida, Ordem = 1, Endereco = "Ponto" };
        db.Paradas.Add(parada);
        await db.SaveChangesAsync();

        var result = await CriarVinculosController(db).Create(new VinculoAlunoFormViewModel
        {
            AlunoId = aluno.Id,
            RotaId = rota.Id,
            ParadaId = parada.Id,
            Sentido = SentidoViagem.Ida
        });

        Assert.Equal("Linhas", Assert.IsType<RedirectToActionResult>(result).ControllerName);
        Assert.Empty(db.VinculosAluno);
    }

    [Fact]
    public async Task AlunoComVinculoNaoPodeSerExcluido()
    {
        using var db = TestesInfra.CriarBanco();
        var (_, rota, _) = TestesInfra.CriarReferencias(db);
        var aluno = new Aluno { Nome = "Aluno", Email = "aluno@easyvan.com", Ativo = true };
        db.Alunos.Add(aluno);
        await db.SaveChangesAsync();
        var parada = new Parada { RotaId = rota.Id, Sentido = SentidoViagem.Ida, Ordem = 1, Endereco = "Ponto" };
        db.Paradas.Add(parada);
        await db.SaveChangesAsync();
        db.VinculosAluno.Add(new VinculoAluno
        {
            AlunoId = aluno.Id,
            RotaId = rota.Id,
            ParadaId = parada.Id,
            Sentido = SentidoViagem.Ida,
            Ativo = true
        });
        await db.SaveChangesAsync();

        var controller = new AlunosController(db, new HasherFake());
        TestesInfra.ConfigurarController(controller);
        var result = await controller.DeleteConfirmed(aluno.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Single(db.Alunos);
        Assert.Equal("Este aluno possui vínculos atuais ou históricos. Desative o cadastro para preservar o histórico.", controller.TempData["Erro"]);
    }

    [Fact]
    public async Task EdicaoDeAlunoSalvaEscolaInformada()
    {
        using var db = TestesInfra.CriarBanco();
        var aluno = new Aluno { Nome = "Ana", Email = "ana@easyvan.com", Ativo = true };
        db.Alunos.Add(aluno);
        await db.SaveChangesAsync();
        var controller = new AlunosController(db, new HasherFake());
        TestesInfra.ConfigurarController(controller);

        var result = await controller.Edit(aluno.Id, new AlunoFormViewModel
        {
            Id = aluno.Id,
            Nome = "Ana Silva",
            Email = "ana@easyvan.com",
            Escola = "  Fatec  ",
            Ativo = true,
            Senha = "senha123",
            ConfirmarSenha = "senha123"
        });

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Fatec", (await db.Alunos.SingleAsync()).Escola);
    }

    [Theory]
    [InlineData(typeof(ParadasController))]
    [InlineData(typeof(HorariosRotaController))]
    [InlineData(typeof(VinculosAlunosController))]
    public void NovasTelasExigemPapelAdministrador(Type controller)
    {
        var authorize = controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Single();
        Assert.Equal("Administrador", authorize.Roles);
    }

    private static ParadasController CriarParadasController(ApplicationDbContext db)
    {
        var controller = new ParadasController(db, new AgendaRotaService(db));
        TestesInfra.ConfigurarController(controller);
        return controller;
    }

    private static HorariosRotaController CriarHorariosController(ApplicationDbContext db)
    {
        var controller = new HorariosRotaController(db, new AgendaRotaService(db));
        TestesInfra.ConfigurarController(controller);
        return controller;
    }

    private static VinculosAlunosController CriarVinculosController(ApplicationDbContext db)
    {
        var controller = new VinculosAlunosController(db);
        TestesInfra.ConfigurarController(controller);
        return controller;
    }

    private sealed class HasherFake : IPasswordService
    {
        public string Hash(string senha) => $"hash:{senha}";
        public bool Verificar(string senha, string hash) => hash == Hash(senha);
        public bool PrecisaAtualizar(string hash) => false;
    }
}

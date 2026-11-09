using EasyVan.Data;
using EasyVan.Models;
using EasyVan.Services;
using Gerenciamento_de_Van.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EasyVan.Tests;

public sealed class ConfirmacaoPresencaControllerTests
{
    [Fact]
    public async Task PainelAlunoIncluiViagemDaDataLocalDeHoje()
    {
        using var db = TestesInfra.CriarBanco();
        var dataLocal = new DateTime(2026, 11, 8);
        var (aluno, _) = await CriarDadosAsync(db, dataLocal);
        var controller = CriarController(
            db,
            new DateTimeOffset(2026, 11, 8, 18, 0, 0, TimeSpan.Zero),
            aluno.UsuarioId!.Value);

        var resultado = await controller.Aluno();

        var painel = Assert.IsType<PainelAlunoViewModel>(Assert.IsType<ViewResult>(resultado).Model);
        Assert.Equal(dataLocal, Assert.Single(painel.Presencas).Viagem!.Data);
    }

    [Fact]
    public async Task AlunoConsegueConfirmarAteOPrazoEConsultaRespostaAtualizada()
    {
        using var db = TestesInfra.CriarBanco();
        var (aluno, viagem) = await CriarDadosAsync(db, new DateTime(2026, 11, 9));
        var agoraUtc = new DateTimeOffset(2026, 11, 8, 18, 0, 0, TimeSpan.Zero);
        var controller = CriarController(db, agoraUtc, aluno.UsuarioId!.Value);

        var resposta = await controller.ConfirmarPresenca(viagem.Id, true);
        var painelResultado = await controller.Aluno();

        Assert.IsType<RedirectToActionResult>(resposta);
        var painel = Assert.IsType<PainelAlunoViewModel>(Assert.IsType<ViewResult>(painelResultado).Model);
        var presenca = Assert.Single(painel.Presencas);
        Assert.Equal(StatusPresenca.Confirmado, presenca.Status);
        Assert.Equal(agoraUtc.UtcDateTime, presenca.RespondidoEm);
    }

    [Fact]
    public async Task EndpointRecusaAlteracaoDepoisDoPrazoMesmoComPostDireto()
    {
        using var db = TestesInfra.CriarBanco();
        var (aluno, viagem) = await CriarDadosAsync(db, new DateTime(2026, 11, 9));
        var controller = CriarController(
            db,
            new DateTimeOffset(2026, 11, 9, 0, 0, 1, TimeSpan.Zero),
            aluno.UsuarioId!.Value);

        var resposta = await controller.ConfirmarPresenca(viagem.Id, true);
        var painelResultado = await controller.Aluno();

        Assert.IsType<RedirectToActionResult>(resposta);
        Assert.Contains("prazo", Assert.IsType<string>(controller.TempData["Erro"]), StringComparison.OrdinalIgnoreCase);
        var painel = Assert.IsType<PainelAlunoViewModel>(Assert.IsType<ViewResult>(painelResultado).Model);
        Assert.Equal(StatusPresenca.SemResposta, Assert.Single(painel.Presencas).Status);
    }

    [Fact]
    public async Task AlunoPodeCancelarPresencaAntesDoPrazo()
    {
        using var db = TestesInfra.CriarBanco();
        var (aluno, viagem) = await CriarDadosAsync(db, new DateTime(2026, 11, 9));
        var controller = CriarController(
            db,
            new DateTimeOffset(2026, 11, 8, 18, 0, 0, TimeSpan.Zero),
            aluno.UsuarioId!.Value);

        var resposta = await controller.ConfirmarPresenca(viagem.Id, false);
        var painelResultado = await controller.Aluno();

        Assert.IsType<RedirectToActionResult>(resposta);
        var painel = Assert.IsType<PainelAlunoViewModel>(Assert.IsType<ViewResult>(painelResultado).Model);
        Assert.Equal(StatusPresenca.Ausente, Assert.Single(painel.Presencas).Status);
    }

    [Fact]
    public async Task PresencaDeOutroAlunoNaoPodeSerAlteradaPorIdDaViagem()
    {
        using var db = TestesInfra.CriarBanco();
        var (_, viagem) = await CriarDadosAsync(db, new DateTime(2026, 11, 9));
        var outroUsuario = new Usuarios
        {
            Nome = "Outro aluno", Email = "outro@easyvan.com", PasswordHasher = "hash", RoleManager = "Aluno"
        };
        db.Usuarios.Add(outroUsuario);
        await db.SaveChangesAsync();
        var outroAluno = new Aluno
        {
            Nome = "Outro aluno", Email = "outro@easyvan.com", UsuarioId = outroUsuario.Id, Ativo = true
        };
        db.Alunos.Add(outroAluno);
        await db.SaveChangesAsync();
        var controller = CriarController(
            db,
            new DateTimeOffset(2026, 11, 8, 18, 0, 0, TimeSpan.Zero),
            outroUsuario.Id);

        var resposta = await controller.ConfirmarPresenca(viagem.Id, true);

        Assert.IsType<RedirectToActionResult>(resposta);
        Assert.Equal("Viagem não encontrada para este aluno.", controller.TempData["Erro"]);
    }

    private static async Task<(Aluno Aluno, Viagem Viagem)> CriarDadosAsync(ApplicationDbContext db, DateTime data)
    {
        var (_, rota, van) = TestesInfra.CriarReferencias(db);
        var usuario = new Usuarios
        {
            Id = 1, Nome = "Aluno de teste", Email = "aluno@easyvan.com",
            PasswordHasher = "hash", RoleManager = "Aluno", Ativo = true
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        var aluno = new Aluno
        {
            Nome = usuario.Nome, Email = usuario.Email, UsuarioId = usuario.Id, Ativo = true
        };
        var viagem = new Viagem
        {
            RotaId = rota.Id, VanId = van.Id, MotoristaId = van.MotoristaId,
            Data = DateTime.SpecifyKind(data.Date, DateTimeKind.Unspecified),
            HoraInicio = new TimeSpan(7, 0, 0), Sentido = SentidoViagem.Ida, Status = "Agendada"
        };
        db.Alunos.Add(aluno);
        db.Viagens.Add(viagem);
        await db.SaveChangesAsync();
        db.PresencasViagem.Add(new PresencaViagem { AlunoId = aluno.Id, ViagemId = viagem.Id });
        await db.SaveChangesAsync();
        return (aluno, viagem);
    }

    private static HomeController CriarController(ApplicationDbContext db, DateTimeOffset agoraUtc, int usuarioId)
    {
        var prazo = new PrazoPresencaService(new PrazoPresencaOptions(), new RelogioFalso(agoraUtc));
        var controller = new HomeController(
            db,
            new CentroOperacoesService(db, new ValidacaoOperacional(db)),
            prazo);
        TestesInfra.ConfigurarController(controller, "Aluno", usuarioId);
        return controller;
    }

    private sealed class RelogioFalso(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using EasyVan.Data;
using EasyVan.Models;
using EasyVan.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EasyVan.Tests;

public class ValidacaoOperacionalTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ValidacaoOperacional _validacao;

    public ValidacaoOperacionalTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _db = new ApplicationDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        _db.Database.EnsureCreated();

        _validacao = new ValidacaoOperacional(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    #region Helpers

    private void CriarReferencias()
    {
        _db.Motoristas.Add(new Motorista { Id = 10, Nome = "Motorista Teste", Email = "m@test.com", Ativo = true });
        _db.Motoristas.Add(new Motorista { Id = 20, Nome = "Motorista 2", Email = "m2@test.com", Ativo = true });
        _db.Rotas.Add(new Rota { Id = 200, Nome = "Rota Teste", Origem = "A", Destino = "B", Ativo = true });
        _db.Rotas.Add(new Rota { Id = 201, Nome = "Rota 2", Origem = "C", Destino = "D", Ativo = true });
        _db.Vans.Add(new Van { Id = 100, Plate = "ABC-1234", Capacity = 10, Status = "Operacional" });
        _db.Vans.Add(new Van { Id = 101, Plate = "DEF-5678", Capacity = 8, Status = "Operacional" });
        _db.SaveChanges();
    }

    private static Viagem NovaViagem(int id, int vanId, int motoristaId, int rotaId, DateTime data,
        TimeSpan? horaInicio = null, TimeSpan? horaFim = null, string status = "Agendada")
    {
        return new Viagem
        {
            Id = id,
            VanId = vanId == 0 ? null : vanId,
            MotoristaId = motoristaId == 0 ? null : motoristaId,
            RotaId = rotaId,
            Data = data,
            HoraInicio = horaInicio,
            HoraFim = horaFim,
            Status = status
        };
    }

    #endregion

    [Fact]
    public void AnalisarAlertas_GeraA5SomenteParaViagemSemConfirmadosEAtiva()
    {
        CriarReferencias();
        var pendente = NovaViagem(801, 100, 10, 200, DateTime.Today.AddDays(1));
        var confirmada = NovaViagem(802, 100, 10, 200, DateTime.Today.AddDays(2));
        var cancelada = NovaViagem(803, 100, 10, 200, DateTime.Today.AddDays(3), status: "Cancelada");
        _db.Viagens.AddRange(pendente, confirmada, cancelada);
        _db.SaveChanges();
        _db.PresencasViagem.AddRange(
            new PresencaViagem { ViagemId = pendente.Id, AlunoId = 1, Status = StatusPresenca.SemResposta },
            new PresencaViagem { ViagemId = confirmada.Id, AlunoId = 2, Status = StatusPresenca.Confirmado },
            new PresencaViagem { ViagemId = cancelada.Id, AlunoId = 3, Status = StatusPresenca.SemResposta });
        _db.SaveChanges();

        var alertas = _validacao.AnalisarAlertas().Where(a => a.Tipo == "A5").ToList();

        var alerta = Assert.Single(alertas);
        Assert.Equal(pendente.Id, alerta.EntidadeId);
        Assert.Equal("Baixa", alerta.Gravidade);
    }

    #region Conflito de horário - Motorista

    [Fact]
    public void AnalisarAlertas_DetectaConflitoHorarioMotorista()
    {
        CriarReferencias();
        var hoje = DateTime.Today;
        _db.Viagens.AddRange(
            NovaViagem(1, 100, 10, 200, hoje, TimeSpan.Parse("08:00"), TimeSpan.Parse("10:00")),
            NovaViagem(2, 101, 10, 201, hoje, TimeSpan.Parse("09:00"), TimeSpan.Parse("11:00"))
        );
        _db.SaveChanges();

        var alertas = _validacao.AnalisarAlertas().ToList();

        var conflito = alertas.FirstOrDefault(a => a.Tipo == "ConflitoHorarioMotorista");
        Assert.NotNull(conflito);
        Assert.Equal("Impeditivo", conflito.Gravidade);
        Assert.Contains("sobrepostas", conflito.Descricao);
    }

    [Fact]
    public void AnalisarAlertas_NaoDetectaConflitoQuandoHorariosNaoSobrepoem()
    {
        CriarReferencias();
        var hoje = DateTime.Today;
        _db.Viagens.AddRange(
            NovaViagem(1, 100, 10, 200, hoje, TimeSpan.Parse("08:00"), TimeSpan.Parse("10:00")),
            NovaViagem(2, 101, 10, 201, hoje, TimeSpan.Parse("10:00"), TimeSpan.Parse("12:00"))
        );
        _db.SaveChanges();

        var alertas = _validacao.AnalisarAlertas().ToList();

        Assert.DoesNotContain(alertas, a => a.Tipo == "ConflitoHorarioMotorista");
    }

    [Fact]
    public void AnalisarAlertas_NaoDetectaConflitoMotoristaEmDatasDiferentes()
    {
        CriarReferencias();
        var hoje = DateTime.Today;
        var amanha = hoje.AddDays(1);
        _db.Viagens.AddRange(
            NovaViagem(1, 100, 10, 200, hoje, TimeSpan.Parse("08:00"), TimeSpan.Parse("10:00")),
            NovaViagem(2, 101, 10, 201, amanha, TimeSpan.Parse("08:00"), TimeSpan.Parse("10:00"))
        );
        _db.SaveChanges();

        var alertas = _validacao.AnalisarAlertas().ToList();

        Assert.DoesNotContain(alertas, a => a.Tipo == "ConflitoHorarioMotorista");
    }

    #endregion

    #region Conflito de horário - Van

    [Fact]
    public void AnalisarAlertas_DetectaConflitoHorarioVan()
    {
        CriarReferencias();
        var hoje = DateTime.Today;
        _db.Viagens.AddRange(
            NovaViagem(1, 100, 10, 200, hoje, TimeSpan.Parse("08:00"), TimeSpan.Parse("10:00")),
            NovaViagem(2, 100, 20, 200, hoje, TimeSpan.Parse("09:00"), TimeSpan.Parse("11:00"))
        );
        _db.SaveChanges();

        var alertas = _validacao.AnalisarAlertas().ToList();

        var conflito = alertas.FirstOrDefault(a => a.Tipo == "ConflitoHorarioVan");
        Assert.NotNull(conflito);
        Assert.Equal("Impeditivo", conflito.Gravidade);
    }

    [Fact]
    public void AnalisarAlertas_NaoDetectaConflitoVanEmDatasDiferentes()
    {
        CriarReferencias();
        var hoje = DateTime.Today;
        var amanha = hoje.AddDays(1);
        _db.Viagens.AddRange(
            NovaViagem(1, 100, 10, 200, hoje, TimeSpan.Parse("08:00"), TimeSpan.Parse("10:00")),
            NovaViagem(2, 100, 20, 200, amanha, TimeSpan.Parse("08:00"), TimeSpan.Parse("10:00"))
        );
        _db.SaveChanges();

        var alertas = _validacao.AnalisarAlertas().ToList();

        Assert.DoesNotContain(alertas, a => a.Tipo == "ConflitoHorarioVan");
    }

    #endregion

    #region Capacidade excedida

    [Fact]
    public void AnalisarAlertas_DetectaCapacidadeExcedida()
    {
        CriarReferencias();
        var hoje = DateTime.Today;
        _db.Viagens.Add(NovaViagem(1, 100, 10, 200, hoje));
        _db.SaveChanges();

        for (var i = 1; i <= 11; i++)
        {
            _db.Alunos.Add(new Aluno { Id = i, Nome = $"Aluno {i}", Email = $"aluno{i}@test.com", Ativo = true });
            _db.PresencasViagem.Add(new PresencaViagem { ViagemId = 1, AlunoId = i, Status = StatusPresenca.Confirmado });
        }
        _db.SaveChanges();

        var alertas = _validacao.AnalisarAlertas().ToList();

        var capacidade = alertas.FirstOrDefault(a => a.Tipo == "CapacidadeExcedida");
        Assert.NotNull(capacidade);
        Assert.Equal("Impeditivo", capacidade.Gravidade);
        Assert.Contains("11", capacidade.Descricao);
        Assert.Contains("10", capacidade.Descricao);
    }

    [Fact]
    public void AnalisarAlertas_NaoDetectaCapacidadeExcedidaQuandoDentroDoLimite()
    {
        CriarReferencias();
        var hoje = DateTime.Today;
        _db.Viagens.Add(NovaViagem(1, 100, 10, 200, hoje));
        _db.SaveChanges();

        for (var i = 1; i <= 8; i++)
        {
            _db.Alunos.Add(new Aluno { Id = i, Nome = $"Aluno {i}", Email = $"aluno{i}@test.com", Ativo = true });
            _db.PresencasViagem.Add(new PresencaViagem { ViagemId = 1, AlunoId = i, Status = StatusPresenca.Confirmado });
        }
        _db.SaveChanges();

        var alertas = _validacao.AnalisarAlertas().ToList();

        Assert.DoesNotContain(alertas, a => a.Tipo == "CapacidadeExcedida");
    }

    #endregion

    #region Viagem sem motorista / sem van

    [Fact]
    public void AnalisarAlertas_DetectaViagemSemMotorista()
    {
        CriarReferencias();
        var hoje = DateTime.Today;
        _db.Viagens.Add(NovaViagem(1, 100, 0, 200, hoje));
        _db.SaveChanges();

        var alertas = _validacao.AnalisarAlertas().ToList();

        Assert.Contains(alertas, a => a.Tipo == "ViagemSemMotorista" && a.Gravidade == "Impeditivo");
    }

    [Fact]
    public void AnalisarAlertas_DetectaViagemSemVan()
    {
        CriarReferencias();
        var hoje = DateTime.Today;
        _db.Viagens.Add(NovaViagem(1, 0, 10, 200, hoje));
        _db.SaveChanges();

        var alertas = _validacao.AnalisarAlertas().ToList();

        Assert.Contains(alertas, a => a.Tipo == "ViagemSemVan" && a.Gravidade == "Impeditivo");
    }

    #endregion

    #region Dados inconsistentes

    [Fact]
    public void AnalisarAlertas_DetectaDadosInconsistentesHorarioFimAntesDoInicio()
    {
        CriarReferencias();
        var hoje = DateTime.Today;
        _db.Viagens.Add(NovaViagem(1, 100, 10, 200, hoje,
            TimeSpan.Parse("14:00"), TimeSpan.Parse("12:00")));
        _db.SaveChanges();

        var alertas = _validacao.AnalisarAlertas().ToList();

        Assert.Contains(alertas, a => a.Tipo == "DadosInconsistentes" && a.Gravidade == "Aviso");
    }

    [Fact]
    public void AnalisarAlertas_NaoDetectaDadosInconsistentesQuandoValidos()
    {
        CriarReferencias();
        var hoje = DateTime.Today;
        _db.Viagens.Add(NovaViagem(1, 100, 10, 200, hoje,
            TimeSpan.Parse("08:00"), TimeSpan.Parse("10:00")));
        _db.SaveChanges();

        var alertas = _validacao.AnalisarAlertas().ToList();

        Assert.DoesNotContain(alertas, a => a.Tipo == "DadosInconsistentes");
    }

    #endregion

    #region Validação de transição de status

    [Fact]
    public void ValidarTransicaoStatus_PlanejadaParaConfirmada_EhValida()
    {
        var resultado = _validacao.ValidarTransicaoStatus("Planejada", "Confirmada");
        Assert.True(resultado.Valido);
        Assert.Equal("TransicaoValida", resultado.Tipo);
    }

    [Fact]
    public void ValidarTransicaoStatus_ConfirmadaParaEmAndamento_EhValida()
    {
        var resultado = _validacao.ValidarTransicaoStatus("Confirmada", "EmAndamento");
        Assert.True(resultado.Valido);
    }

    [Fact]
    public void ValidarTransicaoStatus_EmAndamentoParaFinalizada_EhValida()
    {
        var resultado = _validacao.ValidarTransicaoStatus("EmAndamento", "Finalizada");
        Assert.True(resultado.Valido);
    }

    [Fact]
    public void ValidarTransicaoStatus_PlanejadaParaCancelada_EhValida()
    {
        var resultado = _validacao.ValidarTransicaoStatus("Planejada", "Cancelada");
        Assert.True(resultado.Valido);
    }

    [Fact]
    public void ValidarTransicaoStatus_ConfirmadaParaCancelada_EhValida()
    {
        var resultado = _validacao.ValidarTransicaoStatus("Confirmada", "Cancelada");
        Assert.True(resultado.Valido);
    }

    [Fact]
    public void ValidarTransicaoStatus_FinalizadaParaQualquerOutro_EhInvalida()
    {
        var resultado = _validacao.ValidarTransicaoStatus("Finalizada", "EmAndamento");
        Assert.False(resultado.Valido);
        Assert.Equal("TransicaoInvalida", resultado.Tipo);
        Assert.Equal("Impeditivo", resultado.Gravidade);
    }

    [Fact]
    public void ValidarTransicaoStatus_CanceladaParaQualquerOutro_EhInvalida()
    {
        var resultado = _validacao.ValidarTransicaoStatus("Cancelada", "EmAndamento");
        Assert.False(resultado.Valido);
        Assert.Equal("TransicaoInvalida", resultado.Tipo);
    }

    [Fact]
    public void ValidarTransicaoStatus_PlanejadaParaEmAndamento_EhInvalida()
    {
        var resultado = _validacao.ValidarTransicaoStatus("Planejada", "EmAndamento");
        Assert.False(resultado.Valido);
        Assert.Equal("TransicaoInvalida", resultado.Tipo);
    }

    [Fact]
    public void ValidarTransicaoStatus_StatusDesconhecido_EhInvalido()
    {
        var resultado = _validacao.ValidarTransicaoStatus("Invalida", "Confirmada");
        Assert.False(resultado.Valido);
        Assert.Contains("não é reconhecido", resultado.Descricao);
    }

    #endregion

    #region Validação de nova viagem

    [Fact]
    public void ValidarNovaViagem_VanInexistente_RetornaErro()
    {
        var erros = _validacao.ValidarNovaViagem(999, 10, 200, DateTime.Today).ToList();
        Assert.Contains(erros, e => e.Tipo == "VanInexistente" && !e.Valido);
    }

    [Fact]
    public void ValidarNovaViagem_MotoristaInexistente_RetornaErro()
    {
        var erros = _validacao.ValidarNovaViagem(1, 999, 200, DateTime.Today).ToList();
        Assert.Contains(erros, e => e.Tipo == "MotoristaInexistente" && !e.Valido);
    }

    [Fact]
    public void ValidarNovaViagem_RotaInexistente_RetornaErro()
    {
        var erros = _validacao.ValidarNovaViagem(1, 10, 999, DateTime.Today).ToList();
        Assert.Contains(erros, e => e.Tipo == "RotaInexistente" && !e.Valido);
    }

    [Fact]
    public void ValidarNovaViagem_ConflitoMotorista_RetornaErroImpeditivo()
    {
        CriarReferencias();
        var hoje = DateTime.Today;
        _db.Viagens.Add(NovaViagem(1, 100, 10, 200, hoje, TimeSpan.Parse("08:00"), TimeSpan.Parse("10:00")));
        _db.SaveChanges();

        var erros = _validacao.ValidarNovaViagem(101, 10, 200, hoje,
            TimeSpan.Parse("09:00"), TimeSpan.Parse("11:00")).ToList();

        Assert.Contains(erros, e => e.Tipo == "ConflitoHorarioMotorista" && !e.Valido);
    }

    [Fact]
    public void ValidarNovaViagem_ConflitoVan_RetornaErroImpeditivo()
    {
        CriarReferencias();
        var hoje = DateTime.Today;
        _db.Viagens.Add(NovaViagem(1, 100, 10, 200, hoje, TimeSpan.Parse("08:00"), TimeSpan.Parse("10:00")));
        _db.SaveChanges();

        var erros = _validacao.ValidarNovaViagem(100, 20, 200, hoje,
            TimeSpan.Parse("09:00"), TimeSpan.Parse("11:00")).ToList();

        Assert.Contains(erros, e => e.Tipo == "ConflitoHorarioVan" && !e.Valido);
    }

    [Fact]
    public void ValidarNovaViagem_SemConflitos_RetornaApenasAvisos()
    {
        CriarReferencias();
        var hoje = DateTime.Today.AddDays(1);

        var erros = _validacao.ValidarNovaViagem(100, 10, 200, hoje,
            TimeSpan.Parse("08:00"), TimeSpan.Parse("10:00")).ToList();

        Assert.DoesNotContain(erros, e => !e.Valido);
    }

    #endregion

    #region ValidarCapacidade

    [Fact]
    public void ValidarCapacidade_Excedida_RetornaErro()
    {
        CriarReferencias();
        var hoje = DateTime.Today;
        _db.Viagens.Add(NovaViagem(1, 100, 10, 200, hoje));
        _db.SaveChanges();

        // Van 100 tem Capacity = 10; adiciona 11 passageiros
        for (var i = 1; i <= 11; i++)
        {
            _db.Alunos.Add(new Aluno { Id = i, Nome = $"Aluno {i}", Email = $"a{i}@test.com", Ativo = true });
            _db.PresencasViagem.Add(new PresencaViagem { ViagemId = 1, AlunoId = i, Status = StatusPresenca.Confirmado });
        }
        _db.SaveChanges();

        var erros = _validacao.ValidarCapacidade(1).ToList();
        Assert.Contains(erros, e => e.Tipo == "CapacidadeExcedida" && !e.Valido);
    }

    [Fact]
    public void ValidarCapacidade_DentroDoLimite_NaoRetornaErro()
    {
        CriarReferencias();
        var hoje = DateTime.Today;
        _db.Viagens.Add(NovaViagem(1, 100, 10, 200, hoje));
        _db.SaveChanges();

        for (var i = 1; i <= 5; i++)
        {
            _db.Alunos.Add(new Aluno { Id = i, Nome = $"Aluno {i}", Email = $"a{i}@test.com", Ativo = true });
            _db.PresencasViagem.Add(new PresencaViagem { ViagemId = 1, AlunoId = i, Status = StatusPresenca.Confirmado });
        }
        _db.SaveChanges();

        var erros = _validacao.ValidarCapacidade(1).ToList();
        Assert.DoesNotContain(erros, e => e.Tipo == "CapacidadeExcedida");
    }

    [Fact]
    public void ValidarCapacidade_ViagemInexistente_NaoRetornaErro()
    {
        var erros = _validacao.ValidarCapacidade(999).ToList();
        Assert.Empty(erros);
    }

    #endregion
}

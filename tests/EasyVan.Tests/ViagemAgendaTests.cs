using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EasyVan.Models;
using EasyVan.Services;
using Xunit;

namespace EasyVan.Tests;

public class ViagemAgendaTests
{
    private static readonly DateTime Amanha = new DateTime(2026, 11, 8);
    private static readonly DateTime Hoje = new DateTime(2026, 11, 7);

    [Fact]
    public void ViagemFuturaValidaNaoGeraErros()
    {
        var erros = ViagemAgenda.ValidarPlanejamento(
            Amanha, "07:00", Amanha, "08:30",
            agoraLocal: Hoje.AddHours(10));

        Assert.Empty(erros);
    }

    [Fact]
    public void DataAnteriorAHojeBloqueiaAgendamento()
    {
        var erros = ViagemAgenda.ValidarPlanejamento(
            Hoje.AddDays(-1), "07:00", Hoje.AddDays(-1), "08:00",
            agoraLocal: Hoje.AddHours(10));

        Assert.Contains(erros, e => e.Contains("anterior a hoje", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void HorarioJaPassadoHojeBloqueiaAgendamento()
    {
        var erros = ViagemAgenda.ValidarPlanejamento(
            Hoje, "09:00", Hoje, "10:00",
            agoraLocal: Hoje.AddHours(10));

        Assert.Contains(erros, e => e.Contains("passado", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void HorarioFuturoHojeEhAceito()
    {
        var erros = ViagemAgenda.ValidarPlanejamento(
            Hoje, "11:30", Hoje, "12:30",
            agoraLocal: Hoje.AddHours(10));

        Assert.Empty(erros);
    }

    [Fact]
    public void ChegadaAntesDaPartidaEhRejeitada()
    {
        var erros = ViagemAgenda.ValidarPlanejamento(
            Amanha, "09:00", Amanha, "08:00",
            agoraLocal: Hoje.AddHours(10));

        Assert.Contains(erros, e => e.Contains("posterior à partida", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ViagemQueAtravessaAMeiaNoiteEhAceita()
    {
        var erros = ViagemAgenda.ValidarPlanejamento(
            Amanha, "23:00", Amanha.AddDays(1), "01:00",
            agoraLocal: Hoje.AddHours(10));

        Assert.Empty(erros);
    }

    [Fact]
    public void HorariosObrigatoriosSaoExigidos()
    {
        var erros = ViagemAgenda.ValidarPlanejamento(
            Amanha, null, Amanha, "08:00",
            agoraLocal: Hoje.AddHours(10));

        Assert.Contains(erros, e => e.Contains("partida", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ParaLocalConverteUtcParaSaoPaulo()
    {
        var utc = new DateTime(2026, 11, 7, 15, 0, 0, DateTimeKind.Utc);
        var local = ViagemAgenda.ParaLocal(utc);

        Assert.Equal(12, local.Hour);
        Assert.NotEqual(DateTimeKind.Utc, local.Kind);
    }

    [Fact]
    public void TentarMontarExigeChegadaDepoisDaPartida()
    {
        var montou = ViagemAgenda.TentarMontar(Amanha, "08:00", Amanha, "07:00", out _, out _);

        Assert.False(montou);
    }

    [Fact]
    public void ModeloDeViagemRejeitaChegadaMenorQuePartida()
    {
        var model = new ViagemFormViewModel
        {
            Data = Amanha,
            DataChegada = Amanha,
            HoraInicio = "10:00",
            HoraFim = "09:00"
        };

        var resultados = new List<ValidationResult>();
        var valido = Validator.TryValidateObject(model, new ValidationContext(model), resultados, validateAllProperties: true);

        Assert.False(valido);
        Assert.Contains(resultados, r => r.ErrorMessage!.Contains("posterior à partida", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ModeloDeViagemExigeHorariosPrevistos()
    {
        var model = new ViagemFormViewModel
        {
            Data = Amanha,
            DataChegada = Amanha
        };

        var resultados = new List<ValidationResult>();
        var valido = Validator.TryValidateObject(model, new ValidationContext(model), resultados, validateAllProperties: true);

        Assert.False(valido);
        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(ViagemFormViewModel.HoraInicio)));
    }
}

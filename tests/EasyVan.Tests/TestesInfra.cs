using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using EasyVan.Data;
using EasyVan.Models;
using EasyVan.Services;

namespace EasyVan.Tests;

/// <summary>
/// Infraestrutura compartilhada dos testes: banco SQLite em memória e contexto HTTP simulado.
/// Não substitui o banco real; serve só para exercitar controllers e serviços.
/// </summary>
internal static class TestesInfra
{
    public static ApplicationDbContext CriarBanco()
    {
        var conexao = new SqliteConnection("Data Source=:memory:");
        conexao.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(conexao)
            .Options;

        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");
        return db;
    }

    public static DefaultHttpContext CriarHttpContext(string papel = "Administrador", int usuarioId = 1)
    {
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new Claim(ClaimTypes.Name, "Usuário de teste"),
            new Claim(ClaimTypes.Role, papel)
        }, "Teste"));

        return http;
    }

    public static void ConfigurarController(Controller controller, string papel = "Administrador", int usuarioId = 1)
    {
        var http = CriarHttpContext(papel, usuarioId);
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        controller.TempData = new TempDataDictionary(http, new TempDataFake());
    }

    public static (Motorista Motorista, Rota Rota, Van Van) CriarReferencias(ApplicationDbContext db)
    {
        var motorista = new Motorista
        {
            Nome = "João Silva",
            Email = "joao.silva@easyvan.com",
            CNH = "12345678901",
            Ativo = true
        };
        var rota = new Rota
        {
            Nome = "Centro - Universidade",
            Origem = "Centro",
            Destino = "Universidade",
            Ativo = true
        };
        var van = new Van
        {
            Plate = "ABC-1234",
            Modelo = "Sprinter",
            Driver = motorista.Nome,
            Capacity = 12,
            Route = rota.Nome,
            Schedule = "07:00",
            Status = "Operacional"
        };

        db.Motoristas.Add(motorista);
        db.Rotas.Add(rota);
        db.Vans.Add(van);
        db.SaveChanges();

        van.MotoristaId = motorista.Id;
        van.RotaId = rota.Id;
        db.SaveChanges();

        return (motorista, rota, van);
    }

    private sealed class TempDataFake : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}

/// <summary>Auditoria em memória: permite verificar o que seria registrado sem tocar no banco.</summary>
internal sealed class AuditoriaFake : IAuditoriaService
{
    public List<(string Tipo, string Entidade, int? EntidadeId, string? Descricao)> Registros { get; } = new();

    public void Registrar(
        int? usuarioId,
        string? usuarioNome,
        string tipoOperacao,
        string entidade,
        int? entidadeId,
        string? descricao,
        string? informacoesComplementares = null)
    {
        Registros.Add((tipoOperacao, entidade, entidadeId, descricao));
    }
}

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gerenciamento_de_Van.Models;
using EasyVan.Data;
using EasyVan.Models;
using Microsoft.EntityFrameworkCore;

namespace Gerenciamento_de_Van.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET / — Login sem header. Se já estiver logado, redireciona pelo papel.
    public IActionResult Index()
    {
        if (User.Identity.IsAuthenticated)
        {
            var papel = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
            return papel switch
            {
                "Administrador" => RedirectToAction(nameof(Administrador)),
                "Motorista" => RedirectToAction(nameof(Motorista)),
                _ => RedirectToAction(nameof(Aluno))
            };
        }

        return View(new LoginViewModel());
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // Tela de "Acesso restrito" quando um não-Admin tenta página administrativa.
    public IActionResult AcessoRestrito()
    {
        return View();
    }

    // ----- Aluno -----
    [Authorize(Roles = "Aluno,Administrador")]
    public IActionResult Aluno()
    {
        return View();
    }

    [Authorize(Roles = "Aluno,Administrador")]
    public async Task<IActionResult> Detalhes()
    {
        // "Minha van" do aluno: exibe a primeira van operacional (sem FK aluno-van no MVP,
        // usamos a van de referência como a van atribuída ao aluno logado).
        var van = await _db.Vans.FirstOrDefaultAsync(v => v.Status == "Operacional")
                  ?? await _db.Vans.FirstOrDefaultAsync();
        if (van == null) return NotFound();

        var motoristaNome = van.Driver;
        return View("DetalhesAluno", new
        {
            Van = van,
            MotoristaNome = motoristaNome
        });
    }

    // ----- Motorista -----
    [Authorize(Roles = "Motorista,Administrador")]
    public async Task<IActionResult> Motorista()
    {
        var van = await _db.Vans.FirstOrDefaultAsync(v => v.Status == "Operacional")
                  ?? await _db.Vans.FirstOrDefaultAsync();
        var alunos = await _db.Alunos.Where(a => a.Ativo).OrderBy(a => a.Nome).ToListAsync();
        var rotas = await _db.Rotas.Where(r => r.Ativo).OrderBy(r => r.Nome).ToListAsync();

        return View(new
        {
            Van = van,
            Alunos = alunos,
            Rotas = rotas
        });
    }

    // ----- Administrador -----
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Administrador()
    {
        var totalVans = await _db.Vans.CountAsync();
        var vansOperacionais = await _db.Vans.CountAsync(v => v.Status == "Operacional");
        var vansManutencao = await _db.Vans.CountAsync(v => v.Status == "Manutenção");
        var totalMotoristas = await _db.Motoristas.CountAsync(m => m.Ativo);
        var totalAlunos = await _db.Alunos.CountAsync(a => a.Ativo);
        var totalRotas = await _db.Rotas.CountAsync(r => r.Ativo);

        var inicioDoDia = DateTime.Today;
        var fimDoDia = inicioDoDia.AddDays(1);
        var viagensHoje = await _db.Viagens.CountAsync(v => v.Data >= inicioDoDia && v.Data < fimDoDia);
        var viagensEmAndamento = await _db.Viagens.CountAsync(v => v.Status == "EmAndamento");
        var viagensAgendadas = await _db.Viagens.CountAsync(v => v.Status == "Agendada");

        // Dados reais já existentes no banco: próximas viagens e veículos que exigem atenção.
        var proximasViagens = (await _db.Viagens
            .Include(v => v.Van)
            .Include(v => v.Motorista)
            .Include(v => v.Rota)
            .Where(v => v.Data >= inicioDoDia && (v.Status == "Agendada" || v.Status == "EmAndamento"))
            .ToListAsync())
            .OrderBy(v => v.Data)
            .ThenBy(v => v.HoraInicio ?? TimeSpan.MaxValue)
            .ThenBy(v => v.Id)
            .Take(4)
            .ToList();

        var vansEmManutencao = await _db.Vans
            .Where(v => v.Status == "Manutenção")
            .OrderBy(v => v.Plate)
            .Take(4)
            .ToListAsync();

        var alertas = new List<string>();
        if (vansManutencao > 0)
            alertas.Add($"{vansManutencao} van(es) em manutenção.");
        if (viagensEmAndamento > 0)
            alertas.Add($"{viagensEmAndamento} viagem(ns) em andamento neste momento.");

        return View(new
        {
            TotalVans = totalVans,
            VansOperacionais = vansOperacionais,
            VansManutencao = vansManutencao,
            VansOutros = totalVans - vansOperacionais - vansManutencao,
            TotalMotoristas = totalMotoristas,
            TotalAlunos = totalAlunos,
            TotalRotas = totalRotas,
            ViagensHoje = viagensHoje,
            ViagensEmAndamento = viagensEmAndamento,
            ViagensAgendadas = viagensAgendadas,
            ProximasViagens = proximasViagens,
            VansEmManutencao = vansEmManutencao,
            Alertas = alertas
        });
    }
}

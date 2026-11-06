using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
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
        if (User.Identity?.IsAuthenticated == true)
        {
            var papel = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
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
    public async Task<IActionResult> Aluno()
    {
        var aluno = await ObterAlunoAtualAsync();
        if (aluno == null)
            return View(new PainelAlunoViewModel());

        var (inicioDoDia, _) = ObterIntervaloDoDiaNoFusoDeSaoPaulo();
        var presencas = await _db.PresencasViagem
            .Include(p => p.Viagem)!.ThenInclude(v => v!.Rota)
            .Include(p => p.Viagem)!.ThenInclude(v => v!.Van)
            .Include(p => p.Viagem)!.ThenInclude(v => v!.Motorista)
            .Where(p => p.AlunoId == aluno.Id && p.Viagem != null && p.Viagem.Status != "Cancelada")
            .ToListAsync();

        var futuras = presencas
            .Where(p => p.Viagem!.Data >= inicioDoDia)
            .OrderBy(p => p.Viagem!.Data)
            .ThenBy(p => p.Viagem!.HoraInicio ?? TimeSpan.MaxValue)
            .ThenBy(p => p.Viagem!.Id);

        var passadas = presencas
            .Where(p => p.Viagem!.Data < inicioDoDia)
            .OrderByDescending(p => p.Viagem!.Data)
            .ThenByDescending(p => p.Viagem!.Id)
            .Take(8);

        return View(new PainelAlunoViewModel
        {
            Aluno = aluno,
            Presencas = futuras.Concat(passadas).ToList(),
            Hoje = ObterHojeLocal()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Aluno,Administrador")]
    public async Task<IActionResult> ConfirmarPresenca(int viagemId, bool vou)
    {
        var aluno = await ObterAlunoAtualAsync();
        if (aluno == null)
        {
            TempData["Erro"] = "Sua conta ainda não está vinculada a um cadastro de aluno.";
            return RedirectToAction(nameof(Aluno));
        }

        var presenca = await _db.PresencasViagem
            .Include(p => p.Viagem)
            .FirstOrDefaultAsync(p => p.ViagemId == viagemId && p.AlunoId == aluno.Id);

        if (presenca?.Viagem == null)
        {
            TempData["Erro"] = "Viagem não encontrada para este aluno.";
            return RedirectToAction(nameof(Aluno));
        }

        if (presenca.Viagem.Status is "Finalizada" or "Cancelada")
        {
            TempData["Erro"] = "Esta viagem já foi encerrada e não aceita confirmação.";
            return RedirectToAction(nameof(Aluno));
        }

        var (inicioDoDia, _) = ObterIntervaloDoDiaNoFusoDeSaoPaulo();
        if (presenca.Viagem.Data < inicioDoDia)
        {
            TempData["Erro"] = "Esta viagem já aconteceu e não aceita confirmação.";
            return RedirectToAction(nameof(Aluno));
        }

        presenca.StatusConfirmacao = vou;
        presenca.ConfirmadoEm = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = vou
            ? "Presença confirmada. Bom embarque!"
            : "Resposta registrada: você não vai nesta viagem.";
        return RedirectToAction(nameof(Aluno));
    }

    [Authorize(Roles = "Aluno,Administrador")]
    public async Task<IActionResult> Detalhes()
    {
        var aluno = await ObterAlunoAtualAsync();
        Van? van = null;
        string? motoristaNome = null;

        if (aluno != null)
        {
            var (inicioDoDia, _) = ObterIntervaloDoDiaNoFusoDeSaoPaulo();
            var proximaViagem = await _db.Viagens
                .Include(v => v.Van)!.ThenInclude(van => van!.Rota)
                .Include(v => v.Motorista)
                .Where(v => v.Status != "Cancelada"
                    && v.Data >= inicioDoDia
                    && v.Presencas.Any(p => p.AlunoId == aluno.Id))
                .OrderBy(v => v.Data)
                .ThenBy(v => v.HoraInicio ?? TimeSpan.MaxValue)
                .FirstOrDefaultAsync();

            van = proximaViagem?.Van;
            motoristaNome = proximaViagem?.Motorista?.Nome;
        }

        if (van == null)
        {
            van = await _db.Vans
                .Include(v => v.Motorista)
                .Include(v => v.Rota)
                .FirstOrDefaultAsync(v => v.Status == "Operacional")
                ?? await _db.Vans
                    .Include(v => v.Motorista)
                    .Include(v => v.Rota)
                    .FirstOrDefaultAsync();
            motoristaNome = van?.Motorista?.Nome;
        }

        if (van == null) return NotFound();

        return View("DetalhesAluno", new
        {
            Van = van,
            Rota = van.Rota,
            MotoristaNome = string.IsNullOrWhiteSpace(motoristaNome) ? van.Driver : motoristaNome
        });
    }

    // ----- Motorista -----
    [Authorize(Roles = "Motorista,Administrador")]
    public async Task<IActionResult> Motorista()
    {
        var motorista = await ObterMotoristaAtualAsync();
        if (motorista == null)
            return View(new PainelMotoristaViewModel());

        var viagens = await _db.Viagens
            .Include(v => v.Van)
            .Include(v => v.Rota)
            .Include(v => v.Presencas)!.ThenInclude(p => p.Aluno)
            .Where(v => v.MotoristaId == motorista.Id && v.Status != "Cancelada")
            .ToListAsync();

        var (inicioDoDia, _) = ObterIntervaloDoDiaNoFusoDeSaoPaulo();
        var ordenadas = viagens
            .Where(v => v.Data >= inicioDoDia)
            .OrderBy(v => v.Data)
            .ThenBy(v => v.HoraInicio ?? TimeSpan.MaxValue)
            .ThenBy(v => v.Id)
            .Concat(viagens
                .Where(v => v.Data < inicioDoDia)
                .OrderByDescending(v => v.Data)
                .ThenByDescending(v => v.Id)
                .Take(8))
            .ToList();

        var painel = ordenadas.Select(v => new ViagemDoMotorista
        {
            Viagem = v,
            Alunos = v.Presencas
                .Where(p => p.Aluno != null)
                .OrderBy(p => p.Aluno!.Nome)
                .Select(p => new LinhaAlunoViagem
                {
                    PresencaId = p.Id,
                    AlunoId = p.AlunoId,
                    Nome = p.Aluno!.Nome,
                    Endereco = p.Aluno.Endereco,
                    Telefone = p.Aluno.Telefone,
                    Presente = p.Presente,
                    StatusConfirmacao = p.StatusConfirmacao
                })
                .ToList()
        }).ToList();

        return View(new PainelMotoristaViewModel
        {
            Motorista = motorista,
            Viagens = painel,
            Hoje = ObterHojeLocal()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Motorista,Administrador")]
    public async Task<IActionResult> MarcarPresenca(int viagemId, int alunoId, bool presente)
    {
        var motorista = await ObterMotoristaAtualAsync();
        if (motorista == null && !User.IsInRole("Administrador"))
        {
            TempData["Erro"] = "Sua conta ainda não está vinculada a um cadastro de motorista.";
            return RedirectToAction(nameof(Motorista));
        }

        var viagem = await _db.Viagens.FirstOrDefaultAsync(v => v.Id == viagemId);
        if (viagem == null)
        {
            TempData["Erro"] = "Viagem não encontrada.";
            return RedirectToAction(nameof(Motorista));
        }

        if (!User.IsInRole("Administrador") && viagem.MotoristaId != motorista!.Id)
        {
            TempData["Erro"] = "Você só pode alterar presenças das suas próprias viagens.";
            return RedirectToAction(nameof(Motorista));
        }

        var presenca = await _db.PresencasViagem
            .FirstOrDefaultAsync(p => p.ViagemId == viagemId && p.AlunoId == alunoId);

        if (presenca == null)
        {
            TempData["Erro"] = "Presença não encontrada para esta viagem.";
            return RedirectToAction(nameof(Motorista));
        }

        presenca.Presente = presente;
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = presente ? "Presença registrada." : "Ausência registrada.";
        return RedirectToAction(nameof(Motorista));
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

        var (inicioDoDia, fimDoDia) = ObterIntervaloDoDiaNoFusoDeSaoPaulo();
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

    private int? ObterUsuarioId()
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    }

    private async Task<Aluno?> ObterAlunoAtualAsync()
    {
        var usuarioId = ObterUsuarioId();
        return usuarioId.HasValue
            ? await _db.Alunos.FirstOrDefaultAsync(a => a.UsuarioId == usuarioId.Value)
            : null;
    }

    private async Task<Motorista?> ObterMotoristaAtualAsync()
    {
        var usuarioId = ObterUsuarioId();
        return usuarioId.HasValue
            ? await _db.Motoristas.FirstOrDefaultAsync(m => m.UsuarioId == usuarioId.Value)
            : null;
    }

    private static DateTime ObterHojeLocal()
    {
        var fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, fuso).Date;
    }

    private static (DateTime InicioUtc, DateTime FimUtc) ObterIntervaloDoDiaNoFusoDeSaoPaulo()
    {
        var fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        var hojeLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, fuso).Date;
        var inicioLocal = DateTime.SpecifyKind(hojeLocal, DateTimeKind.Unspecified);
        var fimLocal = inicioLocal.AddDays(1);

        return (
            TimeZoneInfo.ConvertTimeToUtc(inicioLocal, fuso),
            TimeZoneInfo.ConvertTimeToUtc(fimLocal, fuso)
        );
    }
}

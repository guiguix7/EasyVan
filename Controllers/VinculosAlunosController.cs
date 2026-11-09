using EasyVan.Data;
using EasyVan.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EasyVan.Controllers;

[Authorize(Roles = "Administrador")]
public sealed class VinculosAlunosController : Controller
{
    private readonly ApplicationDbContext _db;
    public VinculosAlunosController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(int? alunoId)
    {
        var consulta = _db.VinculosAluno
            .Include(v => v.Aluno)
            .Include(v => v.Rota)
            .Include(v => v.Parada)
            .AsQueryable();
        if (alunoId.HasValue)
            consulta = consulta.Where(v => v.AlunoId == alunoId.Value);
        var vinculos = await consulta
            .OrderByDescending(v => v.Ativo)
            .ThenBy(v => v.Aluno!.Nome)
            .ThenBy(v => v.Sentido)
            .ThenByDescending(v => v.DataInicio)
            .ToListAsync();
        ViewData["AlunoId"] = alunoId;
        return View(vinculos);
    }

    public IActionResult Create(int? alunoId) => RedirectToAction("Create", "Linhas");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Create(VinculoAlunoFormViewModel model) =>
        Task.FromResult<IActionResult>(RedirectToAction("Create", "Linhas")!);

    private async Task PrepararFormularioAsync(VinculoAlunoFormViewModel model)
    {
        model.Alunos = new SelectList(await _db.Alunos.Where(a => a.Ativo || a.Id == model.AlunoId)
            .OrderBy(a => a.Nome).Select(a => new { a.Id, a.Nome }).ToListAsync(), "Id", "Nome", model.AlunoId);
        model.Rotas = new SelectList(await _db.Rotas.Where(r => r.Ativo || r.Id == model.RotaId)
            .OrderBy(r => r.Nome).Select(r => new { r.Id, r.Nome }).ToListAsync(), "Id", "Nome", model.RotaId);
        model.Paradas = new SelectList(await _db.Paradas
            .Include(p => p.Rota)
            .Where(p => p.Ativo || p.Id == model.ParadaId)
            .OrderBy(p => p.Rota!.Nome).ThenBy(p => p.Sentido).ThenBy(p => p.Ordem)
            .Select(p => new { p.Id, Nome = $"{p.Rota!.Nome} · {(p.Sentido == SentidoViagem.Ida ? "Ida" : "Volta")} · {p.Ordem}. {p.Nome ?? p.Endereco}" }).ToListAsync(),
            "Id", "Nome", model.ParadaId);
        model.Sentidos = new[]
        {
            new SelectListItem("Ida", ((int)SentidoViagem.Ida).ToString(), model.Sentido == SentidoViagem.Ida),
            new SelectListItem("Volta", ((int)SentidoViagem.Volta).ToString(), model.Sentido == SentidoViagem.Volta)
        };
    }
}

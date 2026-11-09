using EasyVan.Data;
using EasyVan.Models;
using EasyVan.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EasyVan.Controllers;

[Authorize(Roles = "Administrador")]
public sealed class ParadasController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly AgendaRotaService _agenda;

    public ParadasController(ApplicationDbContext db, AgendaRotaService agenda)
    {
        _db = db;
        _agenda = agenda;
    }

    public async Task<IActionResult> Index()
    {
        var paradas = await _db.Paradas
            .Include(p => p.Rota)
            .OrderBy(p => p.Rota!.Nome)
            .ThenBy(p => p.Sentido)
            .ThenBy(p => p.Ordem)
            .ToListAsync();
        return View(paradas);
    }

    public IActionResult Create(int? rotaId) => RedirectToAction("Create", "Linhas");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Create(ParadaFormViewModel model) =>
        Task.FromResult<IActionResult>(RedirectToAction("Create", "Linhas")!);

    public async Task<IActionResult> Edit(int id)
    {
        var parada = await _db.Paradas.FindAsync(id);
        if (parada is null) return NotFound();

        var model = new ParadaFormViewModel
        {
            Id = parada.Id,
            RotaId = parada.RotaId,
            Sentido = parada.Sentido,
            Ordem = parada.Ordem,
            Nome = parada.Nome,
            Endereco = parada.Endereco,
            HorarioPrevisto = parada.HorarioPrevisto,
            Ativo = parada.Ativo
        };
        await PrepararFormularioAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ParadaFormViewModel model)
    {
        if (id != model.Id) return NotFound();
        var parada = await _db.Paradas.FindAsync(id);
        if (parada is null) return NotFound();
        Normalizar(model);

        var possuiVinculos = await _db.VinculosAluno.AnyAsync(v => v.ParadaId == id);
        if (possuiVinculos && (model.RotaId != parada.RotaId || model.Sentido != parada.Sentido))
            ModelState.AddModelError(string.Empty, "Esta parada possui vÃ­nculos no histÃ³rico. Rota e sentido nÃ£o podem ser alterados.");

        if (!model.Ativo && parada.Ativo && await _db.VinculosAluno.AnyAsync(v => v.ParadaId == id && v.Ativo))
            ModelState.AddModelError(nameof(model.Ativo), "Transfira os alunos com vínculo ativo antes de desativar esta parada.");

        if (ModelState.IsValid)
        {
            var resultado = await _agenda.ValidarParadaAsync(Mapear(model), ignorarId: id);
            if (!resultado.Sucesso)
                ModelState.AddModelError(string.Empty, resultado.Erro!);
        }

        if (!ModelState.IsValid)
        {
            await PrepararFormularioAsync(model);
            return View(model);
        }

        Aplicar(model, parada);
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Parada atualizada.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var parada = await _db.Paradas.Include(p => p.Rota).FirstOrDefaultAsync(p => p.Id == id);
        return parada is null ? NotFound() : View(parada);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var parada = await _db.Paradas.FindAsync(id);
        if (parada is null) return RedirectToAction(nameof(Index));

        if (await _db.VinculosAluno.AnyAsync(v => v.ParadaId == id))
        {
            TempData["Erro"] = "Esta parada possui vínculos no histórico. Desative-a em vez de excluí-la.";
            return RedirectToAction(nameof(Index));
        }

        _db.Paradas.Remove(parada);
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Parada excluída.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PrepararFormularioAsync(ParadaFormViewModel model)
    {
        model.Rotas = new SelectList(await _db.Rotas.Where(r => r.Ativo || r.Id == model.RotaId)
            .OrderBy(r => r.Nome).ToListAsync(), "Id", "Nome", model.RotaId);
        model.Sentidos = new[]
        {
            new SelectListItem("Ida", ((int)SentidoViagem.Ida).ToString(), model.Sentido == SentidoViagem.Ida),
            new SelectListItem("Volta", ((int)SentidoViagem.Volta).ToString(), model.Sentido == SentidoViagem.Volta)
        };
    }

    private static void Normalizar(ParadaFormViewModel model)
    {
        model.Nome = string.IsNullOrWhiteSpace(model.Nome) ? null : model.Nome.Trim();
        model.Endereco = ValidacaoBrasil.LimparTexto(model.Endereco, 200) ?? string.Empty;
    }

    private static Parada Mapear(ParadaFormViewModel model) => new()
    {
        RotaId = model.RotaId,
        Sentido = model.Sentido,
        Ordem = model.Ordem,
        Nome = model.Nome,
        Endereco = model.Endereco,
        HorarioPrevisto = model.HorarioPrevisto,
        Ativo = model.Ativo
    };

    private static void Aplicar(ParadaFormViewModel model, Parada parada)
    {
        parada.RotaId = model.RotaId;
        parada.Sentido = model.Sentido;
        parada.Ordem = model.Ordem;
        parada.Nome = model.Nome;
        parada.Endereco = model.Endereco;
        parada.HorarioPrevisto = model.HorarioPrevisto;
        parada.Ativo = model.Ativo;
    }
}

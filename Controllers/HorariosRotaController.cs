using EasyVan.Data;
using EasyVan.Models;
using EasyVan.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EasyVan.Controllers;

[Authorize(Roles = "Administrador")]
public sealed class HorariosRotaController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly AgendaRotaService _agenda;

    public HorariosRotaController(ApplicationDbContext db, AgendaRotaService agenda)
    {
        _db = db;
        _agenda = agenda;
    }

    public async Task<IActionResult> Index()
    {
        var horarios = await _db.HorariosRota
            .Include(h => h.Rota)
            .Include(h => h.VanPadrao)
            .OrderBy(h => h.Rota!.Nome)
            .ThenBy(h => h.Sentido)
            .ThenBy(h => h.HorarioSaida)
            .ToListAsync();
        return View(horarios);
    }

    public IActionResult Create(int? rotaId) => RedirectToAction("Create", "Linhas");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Create(HorarioRotaFormViewModel model) =>
        Task.FromResult<IActionResult>(RedirectToAction("Create", "Linhas")!);

    public async Task<IActionResult> Edit(int id)
    {
        var horario = await _db.HorariosRota.FindAsync(id);
        if (horario is null) return NotFound();
        var model = new HorarioRotaFormViewModel
        {
            Id = horario.Id,
            RotaId = horario.RotaId,
            Sentido = horario.Sentido,
            DiasSelecionados = Enum.GetValues<DiasSemana>()
                .Where(d => (int)d > 0 && ((int)d & ((int)d - 1)) == 0 && horario.DiasSemana.HasFlag(d))
                .Select(d => (int)d).ToList(),
            HorarioSaida = horario.HorarioSaida,
            VanPadraoId = horario.VanPadraoId,
            Ativo = horario.Ativo
        };
        await PrepararFormularioAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, HorarioRotaFormViewModel model)
    {
        if (id != model.Id) return NotFound();
        var existente = await _db.HorariosRota.FindAsync(id);
        if (existente is null) return NotFound();

        if (ModelState.IsValid)
        {
            var resultado = await _agenda.ValidarHorarioAsync(Mapear(model), ignorarId: id);
            if (!resultado.Sucesso)
                ModelState.AddModelError(string.Empty, resultado.Erro!);
        }

        if (!ModelState.IsValid)
        {
            await PrepararFormularioAsync(model);
            return View(model);
        }

        Aplicar(model, existente);
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Agenda atualizada.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var horario = await _db.HorariosRota.Include(h => h.Rota).Include(h => h.VanPadrao)
            .FirstOrDefaultAsync(h => h.Id == id);
        return horario is null ? NotFound() : View(horario);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var horario = await _db.HorariosRota.FindAsync(id);
        if (horario is null) return RedirectToAction(nameof(Index));
        if (await _db.Viagens.AnyAsync(v => v.HorarioRotaId == id))
        {
            TempData["Erro"] = "Esta agenda já está associada a viagens. Desative-a para preservar o histórico.";
            return RedirectToAction(nameof(Index));
        }
        _db.HorariosRota.Remove(horario);
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Agenda excluída.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PrepararFormularioAsync(HorarioRotaFormViewModel model)
    {
        model.Rotas = new SelectList(await _db.Rotas.Where(r => r.Ativo || r.Id == model.RotaId)
            .OrderBy(r => r.Nome).ToListAsync(), "Id", "Nome", model.RotaId);
        model.Vans = new SelectList(await _db.Vans.Where(v => v.Status == "Operacional" || v.Id == model.VanPadraoId)
            .OrderBy(v => v.Plate).Select(v => new { v.Id, Nome = $"{v.Plate} · {v.Capacity} lugares" }).ToListAsync(),
            "Id", "Nome", model.VanPadraoId);
        model.Sentidos = new[]
        {
            new SelectListItem("Ida", ((int)SentidoViagem.Ida).ToString(), model.Sentido == SentidoViagem.Ida),
            new SelectListItem("Volta", ((int)SentidoViagem.Volta).ToString(), model.Sentido == SentidoViagem.Volta)
        };
    }

    private static HorarioRota Mapear(HorarioRotaFormViewModel model) => new()
    {
        RotaId = model.RotaId,
        Sentido = model.Sentido,
        DiasSemana = ObterDias(model),
        HorarioSaida = model.HorarioSaida ?? TimeSpan.MinValue,
        VanPadraoId = model.VanPadraoId,
        Ativo = model.Ativo
    };

    private static void Aplicar(HorarioRotaFormViewModel model, HorarioRota horario)
    {
        horario.RotaId = model.RotaId;
        horario.Sentido = model.Sentido;
        horario.DiasSemana = ObterDias(model);
        horario.HorarioSaida = model.HorarioSaida ?? TimeSpan.MinValue;
        horario.VanPadraoId = model.VanPadraoId;
        horario.Ativo = model.Ativo;
    }

    private static DiasSemana ObterDias(HorarioRotaFormViewModel model) =>
        (DiasSemana)model.DiasSelecionados
            .Where(d => d is >= 1 and <= 64 && (d & (d - 1)) == 0)
            .Distinct()
            .Aggregate(0, (acumulado, dia) => acumulado | dia);
}

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EasyVan.Data;
using EasyVan.Models;
using EasyVan.Services;

namespace EasyVan.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class RotasController : Controller
    {
        private readonly ApplicationDbContext _db;

        public RotasController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var rotas = await _db.Rotas.OrderBy(r => r.Nome).ToListAsync();
            return View(rotas);
        }

        public async Task<IActionResult> Details(int id)
        {
            var rota = await _db.Rotas.FirstOrDefaultAsync(r => r.Id == id);
            if (rota == null) return NotFound();
            var horarios = await _db.HorariosRota.Include(h => h.VanPadrao)
                .Where(h => h.RotaId == id).OrderBy(h => h.Sentido).ThenBy(h => h.HorarioSaida).ToListAsync();
            var paradas = await _db.Paradas.Where(p => p.RotaId == id)
                .OrderBy(p => p.Sentido).ThenBy(p => p.Ordem).ToListAsync();
            return View(new RotaDetalhesViewModel { Rota = rota, Horarios = horarios, Paradas = paradas });
        }

        public IActionResult Create() => RedirectToAction("Create", "Linhas");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Create(RotaFormViewModel model) =>
            Task.FromResult<IActionResult>(RedirectToAction("Create", "Linhas")!);

        public async Task<IActionResult> Edit(int id)
        {
            var rota = await _db.Rotas.FindAsync(id);
            if (rota == null) return NotFound();
            return View(Mapear(rota));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RotaFormViewModel model)
        {
            if (id != model.Id) return NotFound();
            Normalizar(model);
            if (!ModelState.IsValid) return View(model);

            var existente = await _db.Rotas.FindAsync(id);
            if (existente == null) return NotFound();

            if (!model.Ativo && existente.Ativo
                && (await _db.HorariosRota.AnyAsync(h => h.RotaId == id && h.Ativo)
                    || await _db.Paradas.AnyAsync(p => p.RotaId == id && p.Ativo)
                    || await _db.VinculosAluno.AnyAsync(v => v.RotaId == id && v.Ativo)))
            {
                ModelState.AddModelError(nameof(model.Ativo), "Desative ou transfira as agendas, paradas e vínculos ativos antes de desativar a rota.");
                return View(model);
            }

            existente.Nome = model.Nome;
            existente.Descricao = model.Descricao;
            existente.Origem = model.Origem;
            existente.Destino = model.Destino;
            existente.Ativo = model.Ativo;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var rota = await _db.Rotas.FindAsync(id);
            if (rota == null) return NotFound();
            return View(rota);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var rota = await _db.Rotas.FindAsync(id);
            if (rota != null)
            {
                var dependencias = await _db.HorariosRota.AnyAsync(h => h.RotaId == id)
                    || await _db.Paradas.AnyAsync(p => p.RotaId == id)
                    || await _db.Viagens.AnyAsync(v => v.RotaId == id)
                    || await _db.VinculosAluno.AnyAsync(v => v.RotaId == id);
                if (dependencias)
                {
                    TempData["Erro"] = "Esta rota tem agendas, paradas, viagens ou vínculos no histórico. Desative-a em vez de excluí-la.";
                    return RedirectToAction(nameof(Index));
                }

                _db.Rotas.Remove(rota);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private static RotaFormViewModel Mapear(Rota rota) => new()
        {
            Id = rota.Id,
            Nome = rota.Nome,
            Descricao = rota.Descricao,
            Origem = rota.Origem,
            Destino = rota.Destino,
            Ativo = rota.Ativo
        };

        private static Rota Mapear(RotaFormViewModel model) => new()
        {
            Nome = model.Nome,
            Descricao = model.Descricao,
            Origem = model.Origem,
            Destino = model.Destino,
            Ativo = model.Ativo
        };

        private static void Normalizar(RotaFormViewModel model)
        {
            model.Nome = model.Nome.Trim();
            model.Origem = model.Origem.Trim();
            model.Destino = model.Destino.Trim();
            model.Descricao = string.IsNullOrWhiteSpace(model.Descricao)
                ? null
                : ValidacaoBrasil.LimparTexto(model.Descricao, 500);
        }
    }
}

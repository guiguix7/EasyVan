using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EasyVan.Data;
using EasyVan.Models;

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
            var rota = await _db.Rotas.FindAsync(id);
            if (rota == null) return NotFound();
            return View(rota);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Rota rota)
        {
            if (!ModelState.IsValid) return View(rota);

            _db.Rotas.Add(rota);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var rota = await _db.Rotas.FindAsync(id);
            if (rota == null) return NotFound();
            return View(rota);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Rota rota)
        {
            if (id != rota.Id) return NotFound();
            if (!ModelState.IsValid) return View(rota);

            var existente = await _db.Rotas.FindAsync(id);
            if (existente == null) return NotFound();

            existente.Nome = rota.Nome;
            existente.Descricao = rota.Descricao;
            existente.Origem = rota.Origem;
            existente.Destino = rota.Destino;
            existente.Ativo = rota.Ativo;

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
                _db.Rotas.Remove(rota);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

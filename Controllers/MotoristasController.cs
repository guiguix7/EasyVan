using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EasyVan.Data;
using EasyVan.Models;

namespace EasyVan.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class MotoristasController : Controller
    {
        private readonly ApplicationDbContext _db;

        public MotoristasController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var motoristas = await _db.Motoristas.OrderBy(m => m.Nome).ToListAsync();
            return View(motoristas);
        }

        public async Task<IActionResult> Details(int id)
        {
            var motorista = await _db.Motoristas.FindAsync(id);
            if (motorista == null) return NotFound();
            return View(motorista);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Motorista motorista)
        {
            if (!ModelState.IsValid) return View(motorista);

            _db.Motoristas.Add(motorista);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var motorista = await _db.Motoristas.FindAsync(id);
            if (motorista == null) return NotFound();
            return View(motorista);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Motorista motorista)
        {
            if (id != motorista.Id) return NotFound();
            if (!ModelState.IsValid) return View(motorista);

            var existente = await _db.Motoristas.FindAsync(id);
            if (existente == null) return NotFound();

            existente.Nome = motorista.Nome;
            existente.Email = motorista.Email;
            existente.Telefone = motorista.Telefone;
            existente.CNH = motorista.CNH;
            existente.Ativo = motorista.Ativo;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var motorista = await _db.Motoristas.FindAsync(id);
            if (motorista == null) return NotFound();
            return View(motorista);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var motorista = await _db.Motoristas.FindAsync(id);
            if (motorista != null)
            {
                _db.Motoristas.Remove(motorista);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

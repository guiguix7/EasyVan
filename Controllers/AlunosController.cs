using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EasyVan.Data;
using EasyVan.Models;

namespace EasyVan.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AlunosController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AlunosController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var alunos = await _db.Alunos.OrderBy(a => a.Nome).ToListAsync();
            return View(alunos);
        }

        public async Task<IActionResult> Details(int id)
        {
            var aluno = await _db.Alunos.FindAsync(id);
            if (aluno == null) return NotFound();
            return View(aluno);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Aluno aluno)
        {
            if (!ModelState.IsValid) return View(aluno);

            _db.Alunos.Add(aluno);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var aluno = await _db.Alunos.FindAsync(id);
            if (aluno == null) return NotFound();
            return View(aluno);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Aluno aluno)
        {
            if (id != aluno.Id) return NotFound();
            if (!ModelState.IsValid) return View(aluno);

            var existente = await _db.Alunos.FindAsync(id);
            if (existente == null) return NotFound();

            existente.Nome = aluno.Nome;
            existente.Email = aluno.Email;
            existente.Telefone = aluno.Telefone;
            existente.Endereco = aluno.Endereco;
            existente.Ativo = aluno.Ativo;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var aluno = await _db.Alunos.FindAsync(id);
            if (aluno == null) return NotFound();
            return View(aluno);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var aluno = await _db.Alunos.FindAsync(id);
            if (aluno != null)
            {
                _db.Alunos.Remove(aluno);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

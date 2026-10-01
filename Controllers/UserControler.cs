// Controlador para gerenciar os usuários (Controller)
using Microsoft.AspNetCore.Mvc;
using EasyVan.Models;
using EasyVan.Data;
using Microsoft.EntityFrameworkCore;

namespace EasyVan.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly ApplicationDbContext _db;

        public UsuariosController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var usuarios = await _db.Usuarios.ToListAsync();
            return View(usuarios);
        }

        public async Task<IActionResult> Details(int id)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        public IActionResult Create()
        {
            return View();
        }

        private static string ObterRotaPorPerfil(string? role)
        {
            var perfil = Usuarios.NormalizeRole(role);

            return perfil switch
            {
                "Administrador" => "Administrador",
                "Motorista" => "Motorista",
                _ => "Aluno"
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Usuarios usuario)
        {
            if (!ModelState.IsValid)
            {
                return View(usuario);
            }

            usuario.RoleManager = Usuarios.NormalizeRole(usuario.RoleManager);

            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Usuarios usuario)
        {
            if (!ModelState.IsValid) return View(usuario);

            var existente = await _db.Usuarios.FindAsync(usuario.Id);
            if (existente == null) return NotFound();

            existente.Nome = usuario.Nome;
            existente.Email = usuario.Email;
            existente.RoleManager = usuario.RoleManager;
            existente.PasswordHasher = usuario.PasswordHasher;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                _db.Usuarios.Remove(usuario);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // Simple login action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Home/Index.cshtml", model);
            }

            var user = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == model.Email && u.PasswordHasher == model.Password);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Credenciais inválidas.");
                return View("~/Views/Home/Index.cshtml", model);
            }

            user.RoleManager = Usuarios.NormalizeRole(user.RoleManager);
            var perfil = ObterRotaPorPerfil(user.RoleManager);

            return RedirectToAction(perfil, "Home");
        }
    }
}
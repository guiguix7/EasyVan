// Controlador para gerenciar os usuários (Controller)
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EasyVan.Data;
using EasyVan.Models;
using EasyVan.Services;
using Microsoft.EntityFrameworkCore;

namespace EasyVan.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IPasswordService _senha;

        public UsuariosController(ApplicationDbContext db, IPasswordService senha)
        {
            _db = db;
            _senha = senha;
        }

        // Lista de usuários: só administradores. Se ainda não houver nenhum usuário cadastrado,
        // redireciona para o cadastro (primeiro cadastro é livre, para criar o primeiro Admin).
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Index()
        {
            var temUsuario = await _db.Usuarios.AnyAsync();
            if (!temUsuario)
            {
                return RedirectToAction(nameof(Create));
            }

            var usuarios = await _db.Usuarios.ToListAsync();
            return View(usuarios);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Details(int id)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        // Cadastro livre (sem [Authorize]) para permitir criar o primeiro usuário/Admin do sistema.
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
            usuario.PasswordHasher = _senha.Hash(usuario.PasswordHasher);

            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        [Authorize(Roles = "Administrador")]
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
            if (!string.IsNullOrWhiteSpace(usuario.PasswordHasher))
            {
                existente.PasswordHasher = _senha.Hash(usuario.PasswordHasher);
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        [Authorize(Roles = "Administrador")]
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

        // Login simples com autenticação por cookie.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Home/Index.cshtml", model);
            }

            var user = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null || !_senha.Verificar(model.Password, user.PasswordHasher))
            {
                ModelState.AddModelError(string.Empty, "Credenciais inválidas.");
                return View("~/Views/Home/Index.cshtml", model);
            }

            user.RoleManager = Usuarios.NormalizeRole(user.RoleManager);
            var perfil = ObterRotaPorPerfil(user.RoleManager);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Nome),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, perfil),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction(perfil, "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}

// Controlador para gerenciar usuários e autenticação.
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Index()
        {
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

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Usuarios usuario)
        {
            // Atribuição de perfil é confiável somente quando feita por um administrador autenticado.
            usuario.RoleManager = User.IsInRole("Administrador")
                ? Usuarios.NormalizeRole(usuario.RoleManager)
                : "Aluno";
            usuario.Email = Usuarios.NormalizeEmail(usuario.Email);

            if (!ModelState.IsValid) return View(usuario);

            if (await _db.Usuarios.AnyAsync(u => u.Email.ToLower() == usuario.Email))
            {
                ModelState.AddModelError(nameof(usuario.Email), "Este e-mail já está cadastrado.");
                return View(usuario);
            }

            usuario.PasswordHasher = _senha.Hash(usuario.PasswordHasher);
            _db.Usuarios.Add(usuario);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(nameof(usuario.Email), "Não foi possível cadastrar este e-mail. Verifique se ele já está em uso.");
                usuario.PasswordHasher = string.Empty;
                return View(usuario);
            }

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

            var emailNormalizado = Usuarios.NormalizeEmail(usuario.Email);
            if (await _db.Usuarios.AnyAsync(u => u.Id != usuario.Id && u.Email.ToLower() == emailNormalizado))
            {
                ModelState.AddModelError(nameof(usuario.Email), "Este e-mail já está cadastrado.");
                return View(usuario);
            }

            existente.Nome = usuario.Nome.Trim();
            existente.Email = emailNormalizado;
            existente.RoleManager = Usuarios.NormalizeRole(usuario.RoleManager);
            if (!string.IsNullOrWhiteSpace(usuario.PasswordHasher))
                existente.PasswordHasher = _senha.Hash(usuario.PasswordHasher);

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Home/Index.cshtml", model);

            var email = Usuarios.NormalizeEmail(model.Email);
            var user = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || !_senha.Verificar(model.Password, user.PasswordHasher))
            {
                ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
                return View("~/Views/Home/Index.cshtml", model);
            }

            if (_senha.PrecisaAtualizar(user.PasswordHasher))
            {
                user.PasswordHasher = _senha.Hash(model.Password);
                await _db.SaveChangesAsync();
            }

            user.RoleManager = Usuarios.NormalizeRole(user.RoleManager);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Nome),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.RoleManager),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            return RedirectToAction(user.RoleManager, "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}

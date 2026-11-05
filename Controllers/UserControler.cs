using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using EasyVan.Data;
using EasyVan.Models;
using EasyVan.Services;

namespace EasyVan.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class UsuariosController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IPasswordService _senha;

        public UsuariosController(ApplicationDbContext db, IPasswordService senha)
        {
            _db = db;
            _senha = senha;
        }

        public async Task<IActionResult> Index()
        {
            var usuarios = await _db.Usuarios.OrderBy(u => u.Nome).ToListAsync();
            return View(usuarios);
        }

        public async Task<IActionResult> Details(int id)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        public IActionResult Create() => View(new Usuarios());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Usuarios usuario)
        {
            usuario.Email = Usuarios.NormalizeEmail(usuario.Email);

            if (!Usuarios.IsSupportedRole(usuario.RoleManager))
                ModelState.AddModelError(nameof(usuario.RoleManager), "Selecione um perfil de acesso válido.");
            else
                usuario.RoleManager = Usuarios.NormalizeRole(usuario.RoleManager);

            if (!ModelState.IsValid) return View(usuario);

            if (await _db.Usuarios.AnyAsync(u => u.Email == usuario.Email))
            {
                ModelState.AddModelError(nameof(usuario.Email), "Este e-mail já está cadastrado.");
                usuario.PasswordHasher = string.Empty;
                return View(usuario);
            }

            usuario.Nome = usuario.Nome.Trim();
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

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();
            usuario.RoleManager = Usuarios.NormalizeRole(usuario.RoleManager);
            usuario.PasswordHasher = string.Empty;
            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Usuarios usuario)
        {
            if (id != usuario.Id) return NotFound();

            if (string.IsNullOrWhiteSpace(usuario.PasswordHasher))
                ModelState.Remove(nameof(usuario.PasswordHasher));

            if (!Usuarios.IsSupportedRole(usuario.RoleManager))
                ModelState.AddModelError(nameof(usuario.RoleManager), "Selecione um perfil de acesso válido.");

            var existente = await _db.Usuarios.FindAsync(id);
            if (existente == null) return NotFound();

            var novoEmail = Usuarios.NormalizeEmail(usuario.Email);
            if (await _db.Usuarios.AnyAsync(u => u.Id != id && u.Email == novoEmail))
                ModelState.AddModelError(nameof(usuario.Email), "Este e-mail já está cadastrado.");

            var papelAtual = Usuarios.NormalizeRole(existente.RoleManager);
            var novoPapel = Usuarios.IsSupportedRole(usuario.RoleManager)
                ? Usuarios.NormalizeRole(usuario.RoleManager)
                : papelAtual;

            if (papelAtual == "Administrador" && novoPapel != "Administrador")
            {
                if (UsuarioAtual(id))
                {
                    ModelState.AddModelError(nameof(usuario.RoleManager), "Você não pode remover o próprio acesso de administrador.");
                }
                else
                {
                    var outrosAdministradores = await _db.Usuarios
                        .Where(u => u.Id != id)
                        .ToListAsync();

                    if (!outrosAdministradores.Any(u => Usuarios.NormalizeRole(u.RoleManager) == "Administrador"))
                        ModelState.AddModelError(nameof(usuario.RoleManager), "Não é possível remover o último administrador do sistema.");
                }
            }

            if (!ModelState.IsValid)
            {
                usuario.PasswordHasher = string.Empty;
                return View(usuario);
            }

            existente.Nome = usuario.Nome.Trim();
            existente.Email = novoEmail;
            existente.RoleManager = novoPapel;

            if (!string.IsNullOrWhiteSpace(usuario.PasswordHasher))
                existente.PasswordHasher = _senha.Hash(usuario.PasswordHasher);

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            if (UsuarioAtual(id) || Usuarios.NormalizeRole(usuario.RoleManager) == "Administrador")
                return Forbid();

            return View(usuario);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            if (UsuarioAtual(id) || Usuarios.NormalizeRole(usuario.RoleManager) == "Administrador")
                return Forbid();

            _db.Usuarios.Remove(usuario);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
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
            var properties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                properties);

            return RedirectToAction(user.RoleManager, "Home");
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        private bool UsuarioAtual(int id)
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId)
                && usuarioId == id;
        }
    }
}
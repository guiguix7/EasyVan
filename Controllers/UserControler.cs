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

            if (!ModelState.IsValid)
            {
                ModelState.Remove(nameof(usuario.PasswordHasher));
                usuario.PasswordHasher = string.Empty;
                return View(usuario);
            }

            if (await _db.Usuarios.AnyAsync(u => u.Email == usuario.Email))
            {
                ModelState.AddModelError(nameof(usuario.Email), "Este e-mail já está cadastrado.");
                ModelState.Remove(nameof(usuario.PasswordHasher));
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
                ModelState.Remove(nameof(usuario.PasswordHasher));
                usuario.PasswordHasher = string.Empty;
                return View(usuario);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            return View(new UsuarioEditViewModel
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                RoleManager = Usuarios.NormalizeRole(usuario.RoleManager)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UsuarioEditViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (!Usuarios.IsSupportedRole(model.RoleManager))
                ModelState.AddModelError(nameof(model.RoleManager), "Selecione um perfil de acesso válido.");

            var existente = await _db.Usuarios.FindAsync(id);
            if (existente == null) return NotFound();

            var novoEmail = Usuarios.NormalizeEmail(model.Email);
            if (await _db.Usuarios.AnyAsync(u => u.Id != id && u.Email == novoEmail))
                ModelState.AddModelError(nameof(model.Email), "Este e-mail já está cadastrado.");

            var papelAtual = Usuarios.NormalizeRole(existente.RoleManager);
            var novoPapel = Usuarios.IsSupportedRole(model.RoleManager)
                ? Usuarios.NormalizeRole(model.RoleManager)
                : papelAtual;

            var temAluno = await _db.Alunos.AnyAsync(a => a.UsuarioId == id);
            var temMotorista = await _db.Motoristas.AnyAsync(m => m.UsuarioId == id);
            if ((temAluno && novoPapel != "Aluno") || (temMotorista && novoPapel != "Motorista"))
            {
                ModelState.AddModelError(nameof(model.RoleManager), "Este usuário possui um perfil vinculado. Altere o vínculo antes de mudar o perfil de acesso.");
            }

            if (papelAtual == "Administrador" && novoPapel != "Administrador")
            {
                if (UsuarioAtual(id))
                {
                    ModelState.AddModelError(nameof(model.RoleManager), "Você não pode remover o próprio acesso de administrador.");
                }
                else
                {
                    var outrosAdministradores = await _db.Usuarios
                        .Where(u => u.Id != id)
                        .ToListAsync();

                    if (!outrosAdministradores.Any(u => Usuarios.NormalizeRole(u.RoleManager) == "Administrador"))
                        ModelState.AddModelError(nameof(model.RoleManager), "Não é possível remover o último administrador do sistema.");
                }
            }

            if (!ModelState.IsValid) return View(model);

            existente.Nome = model.Nome.Trim();
            existente.Email = novoEmail;
            existente.RoleManager = novoPapel;

            if (!string.IsNullOrWhiteSpace(model.NovaSenha))
                existente.PasswordHasher = _senha.Hash(model.NovaSenha);

            var aluno = await _db.Alunos.FirstOrDefaultAsync(a => a.UsuarioId == id);
            if (aluno != null)
            {
                aluno.Nome = existente.Nome;
                aluno.Email = existente.Email;
            }

            var motorista = await _db.Motoristas.FirstOrDefaultAsync(m => m.UsuarioId == id);
            if (motorista != null)
            {
                motorista.Nome = existente.Nome;
                motorista.Email = existente.Email;
            }

            await _db.SaveChangesAsync();
            TempData["Sucesso"] = "Usuário atualizado. A senha só é alterada quando uma nova é informada.";
            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        public IActionResult Registrar()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(new RegistroAlunoViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Registrar(RegistroAlunoViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            model.Email = Usuarios.NormalizeEmail(model.Email);
            model.Nome = model.Nome.Trim();
            model.Telefone = ValidacaoBrasil.NormalizarTelefone(model.Telefone);
            model.Endereco = ValidacaoBrasil.LimparTexto(model.Endereco, 200);

            if (!ValidacaoBrasil.NomeValido(model.Nome))
                ModelState.AddModelError(nameof(model.Nome), "Informe um nome válido, com pelo menos 3 letras.");

            if (await _db.Usuarios.AnyAsync(u => u.Email == model.Email))
                ModelState.AddModelError(nameof(model.Email), "Este e-mail já está cadastrado. Entre com sua conta.");

            if (!ModelState.IsValid) return View(model);

            var usuario = new Usuarios
            {
                Nome = model.Nome,
                Email = model.Email,
                RoleManager = "Aluno",
                PasswordHasher = _senha.Hash(model.Senha),
                Tema = "claro"
            };

            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync();

            _db.Alunos.Add(new Aluno
            {
                Nome = model.Nome,
                Email = model.Email,
                Telefone = model.Telefone,
                Endereco = model.Endereco,
                Ativo = true,
                UsuarioId = usuario.Id
            });

            await _db.SaveChangesAsync();
            TempData["Sucesso"] = "Conta criada. Entre com seu e-mail e senha.";
            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View("~/Views/Home/Index.cshtml", new LoginViewModel());
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
            {
                ModelState.Remove(nameof(model.Password));
                model.Password = string.Empty;
                return View("~/Views/Home/Index.cshtml", model);
            }

            var email = Usuarios.NormalizeEmail(model.Email);
            var user = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || !_senha.Verificar(model.Password, user.PasswordHasher))
            {
                ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
                ModelState.Remove(nameof(model.Password));
                model.Password = string.Empty;
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
                new Claim("FotoPerfil", user.FotoPerfil ?? string.Empty),
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

            TemaUi.Gravar(Response, user.Tema, Request.IsHttps);

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
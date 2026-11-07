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
        private readonly PerfilUsuarioService _perfil;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(
            ApplicationDbContext db,
            IPasswordService senha,
            PerfilUsuarioService perfil,
            ILogger<UsuariosController> logger)
        {
            _db = db;
            _senha = senha;
            _perfil = perfil;
            _logger = logger;
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

        public IActionResult Create() => View(new UsuarioCreateViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UsuarioCreateViewModel model)
        {
            Normalizar(model);

            if (!Usuarios.IsSupportedRole(model.RoleManager))
                ModelState.AddModelError(nameof(model.RoleManager), "Selecione um perfil de acesso válido.");
            else
                model.RoleManager = Usuarios.NormalizeRole(model.RoleManager);

            var papel = Usuarios.IsSupportedRole(model.RoleManager)
                ? Usuarios.NormalizeRole(model.RoleManager)
                : null;

            if (papel == "Motorista" && !ValidacaoBrasil.CnhValida(model.Cnh))
                ModelState.AddModelError(nameof(model.Cnh), "Informe uma CNH válida com 11 dígitos para o motorista.");

            if (!ModelState.IsValid)
            {
                model.PasswordHasher = string.Empty;
                model.ConfirmarSenha = string.Empty;
                return View(model);
            }

            if (await _db.Usuarios.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Este e-mail já está cadastrado.");
                model.PasswordHasher = string.Empty;
                model.ConfirmarSenha = string.Empty;
                return View(model);
            }

            var usuario = new Usuarios
            {
                Nome = model.Nome.Trim(),
                Email = model.Email,
                RoleManager = papel!,
                PasswordHasher = _senha.Hash(model.PasswordHasher),
                Tema = "claro"
            };

            await using var transacao = await _db.Database.BeginTransactionAsync();

            try
            {
                _db.Usuarios.Add(usuario);

                var perfil = await _perfil.GarantirPerfilAsync(
                    usuario,
                    new DadosPerfilUsuario(model.Telefone, model.Endereco, model.Cnh, model.Ativo));

                if (!perfil.Sucesso)
                {
                    await transacao.RollbackAsync();
                    ModelState.AddModelError(string.Empty, perfil.Erro!);
                    model.PasswordHasher = string.Empty;
                    model.ConfirmarSenha = string.Empty;
                    return View(model);
                }

                await _db.SaveChangesAsync();
                await transacao.CommitAsync();
            }
            catch (DbUpdateException ex)
            {
                await transacao.RollbackAsync();
                _logger.LogError(ex, "Falha ao criar conta e perfil para {Email}.", model.Email);
                ModelState.AddModelError(nameof(model.Email), "Não foi possível cadastrar este e-mail. Verifique se ele já está em uso.");
                model.PasswordHasher = string.Empty;
                model.ConfirmarSenha = string.Empty;
                return View(model);
            }
            catch (Exception ex)
            {
                await transacao.RollbackAsync();
                _logger.LogError(ex, "Falha inesperada ao criar conta para {Email}.", model.Email);
                ModelState.AddModelError(string.Empty, "Não foi possível criar a conta. Tente novamente.");
                model.PasswordHasher = string.Empty;
                model.ConfirmarSenha = string.Empty;
                return View(model);
            }

            TempData["Sucesso"] = papel == "Administrador"
                ? "Usuário administrador criado."
                : $"Usuário criado e perfil de {papel!.ToLowerInvariant()} vinculado automaticamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            var aluno = await _db.Alunos.AsNoTracking().FirstOrDefaultAsync(a => a.UsuarioId == id);
            var motorista = await _db.Motoristas.AsNoTracking().FirstOrDefaultAsync(m => m.UsuarioId == id);

            return View(new UsuarioEditViewModel
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                RoleManager = Usuarios.NormalizeRole(usuario.RoleManager),
                Telefone = aluno?.Telefone ?? motorista?.Telefone,
                Endereco = aluno?.Endereco,
                Cnh = motorista?.CNH,
                Ativo = aluno?.Ativo ?? motorista?.Ativo ?? true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UsuarioEditViewModel model)
        {
            if (id != model.Id) return NotFound();
            Normalizar(model);

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

            var alunoExistente = await _db.Alunos.FirstOrDefaultAsync(a => a.UsuarioId == id);
            var motoristaExistente = await _db.Motoristas.FirstOrDefaultAsync(m => m.UsuarioId == id);
            if ((alunoExistente != null && novoPapel != "Aluno") || (motoristaExistente != null && novoPapel != "Motorista"))
            {
                ModelState.AddModelError(nameof(model.RoleManager), "Este usuário possui um perfil vinculado. Altere o vínculo antes de mudar o perfil de acesso.");
            }

            if (novoPapel == "Motorista"
                && (motoristaExistente == null || !string.Equals(motoristaExistente.CNH, model.Cnh, StringComparison.Ordinal))
                && !ValidacaoBrasil.CnhValida(model.Cnh))
            {
                ModelState.AddModelError(nameof(model.Cnh), "Informe uma CNH válida com 11 dígitos para o motorista.");
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

            await using var transacao = await _db.Database.BeginTransactionAsync();

            try
            {
                var perfil = await _perfil.GarantirPerfilAsync(
                    existente,
                    new DadosPerfilUsuario(model.Telefone, model.Endereco, model.Cnh, model.Ativo));

                if (!perfil.Sucesso)
                {
                    await transacao.RollbackAsync();
                    ModelState.AddModelError(string.Empty, perfil.Erro!);
                    return View(model);
                }

                await _db.SaveChangesAsync();
                await transacao.CommitAsync();
            }
            catch (DbUpdateException ex)
            {
                await transacao.RollbackAsync();
                _logger.LogError(ex, "Falha ao atualizar conta e perfil do usuário {UsuarioId}.", id);
                ModelState.AddModelError(string.Empty, "Não foi possível atualizar o usuário. Verifique os dados e tente novamente.");
                return View(model);
            }
            catch (Exception ex)
            {
                await transacao.RollbackAsync();
                _logger.LogError(ex, "Falha inesperada ao atualizar o usuário {UsuarioId}.", id);
                ModelState.AddModelError(string.Empty, "Não foi possível atualizar o usuário. Tente novamente.");
                return View(model);
            }

            TempData["Sucesso"] = "Usuário atualizado. A senha só é alterada quando uma nova é informada.";
            return RedirectToAction(nameof(Index));
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

        private static void Normalizar(UsuarioCreateViewModel model)
        {
            model.Nome = (model.Nome ?? string.Empty).Trim();
            model.Email = Usuarios.NormalizeEmail(model.Email);
            model.Telefone = ValidacaoBrasil.NormalizarTelefone(model.Telefone);
            model.Endereco = ValidacaoBrasil.LimparTexto(model.Endereco, 200);
            model.Cnh = ValidacaoBrasil.NormalizarCnh(model.Cnh);
        }

        private static void Normalizar(UsuarioEditViewModel model)
        {
            model.Nome = (model.Nome ?? string.Empty).Trim();
            model.Email = Usuarios.NormalizeEmail(model.Email);
            model.Telefone = ValidacaoBrasil.NormalizarTelefone(model.Telefone);
            model.Endereco = ValidacaoBrasil.LimparTexto(model.Endereco, 200);
            model.Cnh = ValidacaoBrasil.NormalizarCnh(model.Cnh);
        }

        private bool UsuarioAtual(int id)
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId)
                && usuarioId == id;
        }
    }
}
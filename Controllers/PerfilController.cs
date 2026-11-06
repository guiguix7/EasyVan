using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EasyVan.Data;
using EasyVan.Models;
using EasyVan.Services;

namespace EasyVan.Controllers
{
    [Authorize]
    public class PerfilController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IFotoPerfilService _fotos;

        public PerfilController(ApplicationDbContext db, IFotoPerfilService fotos)
        {
            _db = db;
            _fotos = fotos;
        }

        public async Task<IActionResult> Editar()
        {
            var usuario = await ObterUsuarioAsync();
            if (usuario == null) return Challenge();

            return View(new PerfilViewModel
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                FotoAtual = usuario.FotoPerfil
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(3 * 1024 * 1024)]
        public async Task<IActionResult> Editar(PerfilViewModel model)
        {
            var usuario = await ObterUsuarioAsync();
            if (usuario == null) return Challenge();

            model.Id = usuario.Id;
            model.Nome = (model.Nome ?? string.Empty).Trim();
            model.Email = Usuarios.NormalizeEmail(model.Email);
            model.FotoAtual = usuario.FotoPerfil;

            if (!ValidacaoBrasil.NomeValido(model.Nome))
                ModelState.AddModelError(nameof(model.Nome), "Informe um nome válido, com pelo menos 3 letras.");

            if (await _db.Usuarios.AnyAsync(u => u.Id != usuario.Id && u.Email == model.Email))
                ModelState.AddModelError(nameof(model.Email), "Este e-mail já está cadastrado em outra conta.");

            if (model.Foto is { Length: > 0 } && model.RemoverFoto)
                ModelState.AddModelError(nameof(model.Foto), "Escolha entre enviar uma nova foto ou remover a atual.");

            if (!ModelState.IsValid) return View(model);

            string? novaFoto = null;
            if (model.Foto is { Length: > 0 })
            {
                try
                {
                    novaFoto = await _fotos.SalvarAsync(model.Foto);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(nameof(model.Foto), ex.Message);
                    return View(model);
                }
            }

            if (novaFoto != null)
            {
                _fotos.Remover(usuario.FotoPerfil);
                usuario.FotoPerfil = novaFoto;
            }
            else if (model.RemoverFoto)
            {
                _fotos.Remover(usuario.FotoPerfil);
                usuario.FotoPerfil = null;
            }

            usuario.Nome = model.Nome;
            usuario.Email = model.Email;

            var aluno = await _db.Alunos.FirstOrDefaultAsync(a => a.UsuarioId == usuario.Id);
            if (aluno != null)
            {
                aluno.Nome = usuario.Nome;
                aluno.Email = usuario.Email;
            }

            var motorista = await _db.Motoristas.FirstOrDefaultAsync(m => m.UsuarioId == usuario.Id);
            if (motorista != null)
            {
                motorista.Nome = usuario.Nome;
                motorista.Email = usuario.Email;
            }

            await _db.SaveChangesAsync();
            TempData["Sucesso"] = "Perfil atualizado.";
            return RedirectToAction(nameof(Editar));
        }

        [HttpGet]
        public async Task<IActionResult> Foto(int id)
        {
            var usuarioId = ObterUsuarioId();
            if (!User.IsInRole("Administrador") && usuarioId != id)
                return Forbid();

            var usuario = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            var caminho = _fotos.ObterCaminho(usuario?.FotoPerfil);
            var contentType = _fotos.ObterContentType(usuario?.FotoPerfil);
            if (caminho == null || contentType == null) return NotFound();

            Response.Headers["Cache-Control"] = "private, max-age=86400";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return PhysicalFile(caminho, contentType);
        }

        public async Task<IActionResult> Configuracoes()
        {
            var usuario = await ObterUsuarioAsync();
            if (usuario == null) return Challenge();

            return View(new ConfiguracoesViewModel
            {
                Tema = TemaUi.Normalizar(usuario.Tema),
                Nome = usuario.Nome,
                Email = usuario.Email
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Configuracoes(ConfiguracoesViewModel model)
        {
            var usuario = await ObterUsuarioAsync();
            if (usuario == null) return Challenge();

            model.Nome = usuario.Nome;
            model.Email = usuario.Email;
            if (!ModelState.IsValid) return View(model);

            usuario.Tema = TemaUi.Normalizar(model.Tema);
            await _db.SaveChangesAsync();
            TemaUi.Gravar(Response, usuario.Tema, Request.IsHttps);
            TempData["Sucesso"] = "Preferências salvas.";
            return RedirectToAction(nameof(Configuracoes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlternarTema(string? retornoUrl)
        {
            var usuario = await ObterUsuarioAsync();
            if (usuario == null) return Challenge();

            var temaAtual = TemaUi.Normalizar(usuario.Tema);
            var novoTema = temaAtual == TemaUi.Escuro ? TemaUi.Claro : TemaUi.Escuro;
            usuario.Tema = novoTema;
            await _db.SaveChangesAsync();
            TemaUi.Gravar(Response, novoTema, Request.IsHttps);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return NoContent();

            if (Url.IsLocalUrl(retornoUrl))
                return Redirect(retornoUrl!);

            return RedirectToAction("Index", "Home");
        }

        private int? ObterUsuarioId()
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
        }

        private async Task<Usuarios?> ObterUsuarioAsync()
        {
            var id = ObterUsuarioId();
            return id.HasValue ? await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id.Value) : null;
        }
    }
}

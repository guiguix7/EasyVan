using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EasyVan.Data;
using EasyVan.Models;
using EasyVan.Services;

namespace EasyVan.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AlunosController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IPasswordService _senha;

        public AlunosController(ApplicationDbContext db, IPasswordService senha)
        {
            _db = db;
            _senha = senha;
        }

        public async Task<IActionResult> Index()
        {
            var alunos = await _db.Alunos.Include(a => a.Usuario).OrderBy(a => a.Nome).ToListAsync();
            return View(alunos);
        }

        public async Task<IActionResult> Details(int id)
        {
            var aluno = await _db.Alunos.Include(a => a.Usuario).FirstOrDefaultAsync(a => a.Id == id);
            if (aluno == null) return NotFound();
            return View(aluno);
        }

        public IActionResult Create() => View(new AlunoFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AlunoFormViewModel model)
        {
            Normalizar(model);
            if (!ModelState.IsValid) return View(model);

            var email = Usuarios.NormalizeEmail(model.Email);
            var conta = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
            if (conta != null)
            {
                if (Usuarios.NormalizeRole(conta.RoleManager) != "Aluno")
                    ModelState.AddModelError(nameof(model.Email), "Este e-mail pertence a uma conta de outro perfil.");
                else if (await _db.Alunos.AnyAsync(a => a.UsuarioId == conta.Id))
                    ModelState.AddModelError(nameof(model.Email), "Esta conta já está vinculada a outro aluno.");
            }
            else if (string.IsNullOrWhiteSpace(model.Senha))
            {
                ModelState.AddModelError(nameof(model.Senha), "Informe uma senha para criar a conta de acesso do aluno.");
            }

            if (!ModelState.IsValid) return View(model);

            await using var transacao = await _db.Database.BeginTransactionAsync();

            if (conta == null)
            {
                conta = new Usuarios
                {
                    Nome = model.Nome.Trim(),
                    Email = email,
                    RoleManager = "Aluno",
                    PasswordHasher = _senha.Hash(model.Senha!),
                    Tema = "claro"
                };
                _db.Usuarios.Add(conta);
                await _db.SaveChangesAsync();
            }

            _db.Alunos.Add(new Aluno
            {
                Nome = model.Nome.Trim(),
                Email = email,
                Telefone = model.Telefone,
                Endereco = model.Endereco,
                Ativo = model.Ativo,
                UsuarioId = conta.Id
            });

            await _db.SaveChangesAsync();
            await transacao.CommitAsync();

            TempData["Sucesso"] = "Aluno e conta de acesso cadastrados.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var aluno = await _db.Alunos.Include(a => a.Usuario).FirstOrDefaultAsync(a => a.Id == id);
            if (aluno == null) return NotFound();

            return View(new AlunoFormViewModel
            {
                Id = aluno.Id,
                Nome = aluno.Nome,
                Email = aluno.Email,
                Telefone = aluno.Telefone,
                Endereco = aluno.Endereco,
                Ativo = aluno.Ativo,
                ContaVinculada = aluno.UsuarioId.HasValue,
                EmailConta = aluno.Usuario?.Email
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AlunoFormViewModel model)
        {
            if (id != model.Id) return NotFound();
            Normalizar(model);

            var existente = await _db.Alunos.Include(a => a.Usuario).FirstOrDefaultAsync(a => a.Id == id);
            if (existente == null) return NotFound();

            model.ContaVinculada = existente.UsuarioId.HasValue;
            model.EmailConta = existente.Usuario?.Email;
            var email = Usuarios.NormalizeEmail(model.Email);
            var conta = existente.Usuario;

            if (conta == null)
            {
                conta = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
                if (conta != null)
                {
                    if (Usuarios.NormalizeRole(conta.RoleManager) != "Aluno")
                        ModelState.AddModelError(nameof(model.Email), "Este e-mail pertence a uma conta de outro perfil.");
                    else if (await _db.Alunos.AnyAsync(a => a.UsuarioId == conta.Id && a.Id != id))
                        ModelState.AddModelError(nameof(model.Email), "Esta conta já está vinculada a outro aluno.");
                }
                else if (string.IsNullOrWhiteSpace(model.Senha))
                {
                    ModelState.AddModelError(nameof(model.Senha), "Este aluno ainda não tem conta. Informe uma senha para criá-la.");
                }
            }
            else
            {
                if (await _db.Usuarios.AnyAsync(u => u.Id != conta.Id && u.Email == email))
                    ModelState.AddModelError(nameof(model.Email), "Este e-mail já está cadastrado em outra conta.");
            }

            if (!ModelState.IsValid) return View(model);

            await using var transacao = await _db.Database.BeginTransactionAsync();

            if (conta == null)
            {
                conta = new Usuarios
                {
                    Nome = model.Nome.Trim(),
                    Email = email,
                    RoleManager = "Aluno",
                    PasswordHasher = _senha.Hash(model.Senha!),
                    Tema = "claro"
                };
                _db.Usuarios.Add(conta);
                await _db.SaveChangesAsync();
            }
            else
            {
                conta.Nome = model.Nome.Trim();
                conta.Email = email;
                if (!string.IsNullOrWhiteSpace(model.Senha))
                    conta.PasswordHasher = _senha.Hash(model.Senha);
            }

            existente.Nome = model.Nome.Trim();
            existente.Email = email;
            existente.Telefone = model.Telefone;
            existente.Endereco = model.Endereco;
            existente.Ativo = model.Ativo;
            existente.UsuarioId = conta.Id;

            await _db.SaveChangesAsync();
            await transacao.CommitAsync();

            TempData["Sucesso"] = "Aluno atualizado.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var aluno = await _db.Alunos.Include(a => a.Usuario).FirstOrDefaultAsync(a => a.Id == id);
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
                if (aluno.UsuarioId.HasValue)
                {
                    TempData["Erro"] = "Este aluno possui conta de acesso. Desative o cadastro em vez de excluí-lo para não deixar a conta órfã.";
                    return RedirectToAction(nameof(Index));
                }

                if (await _db.PresencasViagem.AnyAsync(p => p.AlunoId == id))
                {
                    TempData["Erro"] = "Este aluno possui presenças registradas e não pode ser excluído. Desative o cadastro.";
                    return RedirectToAction(nameof(Index));
                }

                _db.Alunos.Remove(aluno);
                await _db.SaveChangesAsync();
                TempData["Sucesso"] = "Aluno excluído.";
            }

            return RedirectToAction(nameof(Index));
        }

        private static void Normalizar(AlunoFormViewModel model)
        {
            model.Nome = (model.Nome ?? string.Empty).Trim();
            model.Email = Usuarios.NormalizeEmail(model.Email);
            model.Telefone = ValidacaoBrasil.NormalizarTelefone(model.Telefone);
            model.Endereco = ValidacaoBrasil.LimparTexto(model.Endereco, 200);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EasyVan.Data;
using EasyVan.Models;
using EasyVan.Services;

namespace EasyVan.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class MotoristasController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IPasswordService _senha;

        public MotoristasController(ApplicationDbContext db, IPasswordService senha)
        {
            _db = db;
            _senha = senha;
        }

        public async Task<IActionResult> Index()
        {
            var motoristas = await _db.Motoristas.Include(m => m.Usuario).OrderBy(m => m.Nome).ToListAsync();
            return View(motoristas);
        }

        public async Task<IActionResult> Details(int id)
        {
            var motorista = await _db.Motoristas.Include(m => m.Usuario).FirstOrDefaultAsync(m => m.Id == id);
            if (motorista == null) return NotFound();
            return View(motorista);
        }

        public IActionResult Create() => View(new MotoristaFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MotoristaFormViewModel model)
        {
            Normalizar(model);
            if (!ValidacaoBrasil.CnhValida(model.CNH))
                ModelState.AddModelError(nameof(model.CNH), "Informe uma CNH válida com 11 dígitos.");

            if (!ModelState.IsValid) return View(model);

            var email = Usuarios.NormalizeEmail(model.Email);
            var conta = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
            if (conta != null)
            {
                if (Usuarios.NormalizeRole(conta.RoleManager) != "Motorista")
                    ModelState.AddModelError(nameof(model.Email), "Este e-mail pertence a uma conta de outro perfil.");
                else if (await _db.Motoristas.AnyAsync(m => m.UsuarioId == conta.Id))
                    ModelState.AddModelError(nameof(model.Email), "Esta conta já está vinculada a outro motorista.");
            }
            else if (string.IsNullOrWhiteSpace(model.Senha))
            {
                ModelState.AddModelError(nameof(model.Senha), "Informe uma senha para criar a conta de acesso do motorista.");
            }

            if (await _db.Motoristas.AnyAsync(m => m.Email == email))
                ModelState.AddModelError(nameof(model.Email), "Já existe um motorista com este e-mail. Edite o cadastro existente.");

            if (await _db.Alunos.AnyAsync(a => a.Email == email))
                ModelState.AddModelError(nameof(model.Email), "Este e-mail já está associado a um aluno.");

            if (!ModelState.IsValid) return View(model);

            await using var transacao = await _db.Database.BeginTransactionAsync();

            if (conta == null)
            {
                conta = new Usuarios
                {
                    Nome = model.Nome.Trim(),
                    Email = email,
                    RoleManager = "Motorista",
                    PasswordHasher = _senha.Hash(model.Senha!),
                    Tema = "claro",
                    Ativo = model.Ativo
                };
                _db.Usuarios.Add(conta);
                await _db.SaveChangesAsync();
            }
            else
            {
                conta.Ativo = model.Ativo;
            }

            _db.Motoristas.Add(new Motorista
            {
                Nome = model.Nome.Trim(),
                Email = email,
                Telefone = model.Telefone,
                CNH = model.CNH,
                Ativo = model.Ativo,
                UsuarioId = conta.Id
            });

            await _db.SaveChangesAsync();
            await transacao.CommitAsync();

            TempData["Sucesso"] = "Motorista e conta de acesso cadastrados.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var motorista = await _db.Motoristas.Include(m => m.Usuario).FirstOrDefaultAsync(m => m.Id == id);
            if (motorista == null) return NotFound();

            return View(new MotoristaFormViewModel
            {
                Id = motorista.Id,
                Nome = motorista.Nome,
                Email = motorista.Email,
                Telefone = motorista.Telefone,
                CNH = motorista.CNH,
                Ativo = motorista.Ativo,
                ContaVinculada = motorista.UsuarioId.HasValue,
                EmailConta = motorista.Usuario?.Email
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MotoristaFormViewModel model)
        {
            if (id != model.Id) return NotFound();
            Normalizar(model);

            var existente = await _db.Motoristas.Include(m => m.Usuario).FirstOrDefaultAsync(m => m.Id == id);
            if (existente == null) return NotFound();

            model.ContaVinculada = existente.UsuarioId.HasValue;
            model.EmailConta = existente.Usuario?.Email;

            // CNHs legadas podem estar inválidas; só exigimos o algoritmo quando o valor muda.
            if (!string.Equals(existente.CNH, model.CNH, StringComparison.Ordinal) && !ValidacaoBrasil.CnhValida(model.CNH))
                ModelState.AddModelError(nameof(model.CNH), "Informe uma CNH válida com 11 dígitos.");

            var email = Usuarios.NormalizeEmail(model.Email);
            var conta = existente.Usuario;
            if (conta == null)
            {
                conta = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
                if (conta != null)
                {
                    if (Usuarios.NormalizeRole(conta.RoleManager) != "Motorista")
                        ModelState.AddModelError(nameof(model.Email), "Este e-mail pertence a uma conta de outro perfil.");
                    else if (await _db.Motoristas.AnyAsync(m => m.UsuarioId == conta.Id && m.Id != id))
                        ModelState.AddModelError(nameof(model.Email), "Esta conta já está vinculada a outro motorista.");
                }
                else if (string.IsNullOrWhiteSpace(model.Senha))
                {
                    ModelState.AddModelError(nameof(model.Senha), "Este motorista ainda não tem conta. Informe uma senha para criá-la.");
                }
            }
            else if (await _db.Usuarios.AnyAsync(u => u.Id != conta.Id && u.Email == email))
            {
                ModelState.AddModelError(nameof(model.Email), "Este e-mail já está cadastrado em outra conta.");
            }

            if (await _db.Motoristas.AnyAsync(m => m.Id != id && m.Email == email))
                ModelState.AddModelError(nameof(model.Email), "Já existe outro motorista com este e-mail.");

            if (await _db.Alunos.AnyAsync(a => a.Email == email))
                ModelState.AddModelError(nameof(model.Email), "Este e-mail já está associado a um aluno.");

            if (!ModelState.IsValid) return View(model);

            await using var transacao = await _db.Database.BeginTransactionAsync();

            if (conta == null)
            {
                conta = new Usuarios
                {
                    Nome = model.Nome.Trim(),
                    Email = email,
                    RoleManager = "Motorista",
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
                conta.Ativo = model.Ativo;
                if (!string.IsNullOrWhiteSpace(model.Senha))
                    conta.PasswordHasher = _senha.Hash(model.Senha);
            }

            existente.Nome = model.Nome.Trim();
            existente.Email = email;
            existente.Telefone = model.Telefone;
            existente.CNH = model.CNH;
            existente.Ativo = model.Ativo;
            existente.UsuarioId = conta.Id;

            await _db.SaveChangesAsync();
            await transacao.CommitAsync();

            TempData["Sucesso"] = "Motorista atualizado.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var motorista = await _db.Motoristas.Include(m => m.Usuario).FirstOrDefaultAsync(m => m.Id == id);
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
                if (motorista.UsuarioId.HasValue)
                {
                    TempData["Erro"] = "Este motorista possui conta de acesso. Desative o cadastro em vez de excluí-lo para não deixar a conta órfã.";
                    return RedirectToAction(nameof(Index));
                }

                if (await _db.Viagens.AnyAsync(v => v.MotoristaId == id))
                {
                    TempData["Erro"] = "Este motorista possui viagens registradas e não pode ser excluído. Desative o cadastro.";
                    return RedirectToAction(nameof(Index));
                }

                _db.Motoristas.Remove(motorista);
                await _db.SaveChangesAsync();
                TempData["Sucesso"] = "Motorista excluído.";
            }

            return RedirectToAction(nameof(Index));
        }

        private static void Normalizar(MotoristaFormViewModel model)
        {
            model.Nome = (model.Nome ?? string.Empty).Trim();
            model.Email = Usuarios.NormalizeEmail(model.Email);
            model.Telefone = ValidacaoBrasil.NormalizarTelefone(model.Telefone);
            model.CNH = ValidacaoBrasil.NormalizarCnh(model.CNH);
        }
    }
}

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EasyVan.Data;
using EasyVan.Models;
using System.Security.Claims;

namespace EasyVan.Controllers
{
    [Authorize(Roles = "Administrador,Motorista")]
    public class ViagensController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ViagensController(ApplicationDbContext db)
        {
            _db = db;
        }

        private int? UsuarioIdLogado()
        {
            var valor = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return string.IsNullOrEmpty(valor) ? (int?)null : int.Parse(valor);
        }

        private string PapelLogado()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }

        // Lista de viagens. Admin vê todas; Motorista vê apenas as suas.
        public async Task<IActionResult> Index()
        {
            var papel = PapelLogado();
            var usuarioId = UsuarioIdLogado();

            IQueryable<Viagem> consulta = _db.Viagens
                .Include(v => v.Van)
                .Include(v => v.Motorista)
                .Include(v => v.Rota)
                .OrderByDescending(v => v.Data)
                .ThenByDescending(v => v.Id);

            if (papel == "Motorista" && usuarioId.HasValue)
            {
                consulta = consulta.Where(v => v.MotoristaId == usuarioId.Value);
            }

            var viagens = await consulta.ToListAsync();
            ViewData["Papel"] = papel;
            ViewData["UsuarioId"] = usuarioId;
            return View(viagens);
        }

        public async Task<IActionResult> Details(int id)
        {
            var viagem = await _db.Viagens
                .Include(v => v.Van)
                .Include(v => v.Motorista)
                .Include(v => v.Rota)
                .FirstOrDefaultAsync(v => v.Id == id);
            if (viagem == null) return NotFound();

            // Restrição: motorista só vê viagens próprias; Admin vê todas.
            if (PapelLogado() == "Motorista" && viagem.MotoristaId != UsuarioIdLogado())
            {
                return RedirectToAction("AcessoRestrito", "Home");
            }

            var alunosDaViagem = await _db.PresencasViagem
                .Where(p => p.ViagemId == id)
                .Include(p => p.Aluno)
                .OrderBy(p => p.Aluno.Nome)
                .ToListAsync();

            ViewData["AlunosDaViagem"] = alunosDaViagem;
            return View(viagem);
        }

        // Nova viagem (Admin ou Motorista).
        public async Task<IActionResult> Create()
        {
            ViewData["Vans"] = await _db.Vans.OrderBy(v => v.Plate).ToListAsync();
            ViewData["Motoristas"] = await _db.Motoristas.Where(m => m.Ativo).OrderBy(m => m.Nome).ToListAsync();
            ViewData["Rotas"] = await _db.Rotas.Where(r => r.Ativo).OrderBy(r => r.Nome).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Viagem viagem)
        {
            // Motorista só pode criar viagem para si mesmo.
            if (PapelLogado() == "Motorista")
            {
                var usuarioId = UsuarioIdLogado();
                if (usuarioId.HasValue)
                {
                    viagem.MotoristaId = usuarioId.Value;
                }
            }

            if (!ModelState.IsValid)
            {
                ViewData["Vans"] = await _db.Vans.OrderBy(v => v.Plate).ToListAsync();
                ViewData["Motoristas"] = await _db.Motoristas.Where(m => m.Ativo).OrderBy(m => m.Nome).ToListAsync();
                ViewData["Rotas"] = await _db.Rotas.Where(r => r.Ativo).OrderBy(r => r.Nome).ToListAsync();
                return View(viagem);
            }

            _db.Viagens.Add(viagem);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Iniciar viagem: Agendada -> EmAndamento, registra HoraInicio.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(int id)
        {
            var viagem = await _db.Viagens.FindAsync(id);
            if (viagem == null) return NotFound();

            if (PapelLogado() == "Motorista" && viagem.MotoristaId != UsuarioIdLogado())
            {
                return RedirectToAction("AcessoRestrito", "Home");
            }

            if (viagem.Status != "Agendada")
            {
                TempData["Erro"] = "Só é possível iniciar uma viagem com status Agendada.";
                return RedirectToAction(nameof(Details), new { id });
            }

            viagem.Status = "EmAndamento";
            viagem.HoraInicio = DateTime.Now.TimeOfDay;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        // Finalizar viagem: EmAndamento -> Finalizada, registra HoraFim.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Finish(int id)
        {
            var viagem = await _db.Viagens.FindAsync(id);
            if (viagem == null) return NotFound();

            if (PapelLogado() == "Motorista" && viagem.MotoristaId != UsuarioIdLogado())
            {
                return RedirectToAction("AcessoRestrito", "Home");
            }

            if (viagem.Status != "EmAndamento")
            {
                TempData["Erro"] = "Só é possível finalizar uma viagem em andamento.";
                return RedirectToAction(nameof(Details), new { id });
            }

            viagem.Status = "Finalizada";
            viagem.HoraFim = DateTime.Now.TimeOfDay;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        // Marcar presença de um aluno na viagem.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarPresenca(int viagemId, int alunoId, bool presente, string? observacao)
        {
            var viagem = await _db.Viagens.FindAsync(viagemId);
            if (viagem == null) return NotFound();

            if (PapelLogado() == "Motorista" && viagem.MotoristaId != UsuarioIdLogado())
            {
                return RedirectToAction("AcessoRestrito", "Home");
            }

            if (viagem.Status != "EmAndamento")
            {
                TempData["Erro"] = "A presença só pode ser marcada enquanto a viagem está em andamento.";
                return RedirectToAction(nameof(Details), new { id = viagemId });
            }

            var existe = await _db.PresencasViagem.FirstOrDefaultAsync(p => p.ViagemId == viagemId && p.AlunoId == alunoId);
            if (existe != null)
            {
                existe.Presente = presente;
                existe.Observacao = observacao;
            }
            else
            {
                _db.PresencasViagem.Add(new PresencaViagem
                {
                    ViagemId = viagemId,
                    AlunoId = alunoId,
                    Presente = presente,
                    Observacao = observacao
                });
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = viagemId });
        }

        // Inicializa a lista de presença para todos os alunos ativos (útil ao iniciar a viagem).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InicializarPresenca(int id)
        {
            var viagem = await _db.Viagens.FindAsync(id);
            if (viagem == null) return NotFound();

            if (PapelLogado() == "Motorista" && viagem.MotoristaId != UsuarioIdLogado())
            {
                return RedirectToAction("AcessoRestrito", "Home");
            }

            if (viagem.Status != "EmAndamento")
            {
                TempData["Erro"] = "A presença só pode ser inicializada enquanto a viagem está em andamento.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var alunos = await _db.Alunos.Where(a => a.Ativo).ToListAsync();
            foreach (var aluno in alunos)
            {
                var jaExiste = await _db.PresencasViagem.AnyAsync(p => p.ViagemId == id && p.AlunoId == aluno.Id);
                if (!jaExiste)
                {
                    _db.PresencasViagem.Add(new PresencaViagem
                    {
                        ViagemId = id,
                        AlunoId = aluno.Id,
                        Presente = false
                    });
                }
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}

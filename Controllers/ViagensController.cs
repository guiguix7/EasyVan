using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EasyVan.Data;
using EasyVan.Models;
using EasyVan.Services;

namespace EasyVan.Controllers
{
    [Authorize(Roles = "Administrador,Motorista")]
    public class ViagensController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ValidacaoOperacional _validacao;
        private readonly IAuditoriaService _auditoria;

        public ViagensController(ApplicationDbContext db, ValidacaoOperacional validacao, IAuditoriaService auditoria)
        {
            _db = db;
            _validacao = validacao;
            _auditoria = auditoria;
        }

        private string? NomeUsuarioLogado()
        {
            return User.Identity?.IsAuthenticated == true ? User.Identity.Name : null;
        }

        private int? UsuarioIdLogado()
        {
            var valor = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return string.IsNullOrEmpty(valor) ? null : int.Parse(valor);
        }

        private string PapelLogado() => User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        private async Task<Motorista?> MotoristaDoUsuarioAsync()
        {
            var usuarioId = UsuarioIdLogado();
            if (!usuarioId.HasValue) return null;

            return await _db.Motoristas.FirstOrDefaultAsync(m => m.UsuarioId == usuarioId.Value);
        }

        // Lista de viagens. Admin vê todas; Motorista vê apenas as suas.
        public async Task<IActionResult> Index(int? mes, int? ano)
        {
            var papel = PapelLogado();
            var motorista = papel == "Motorista" ? await MotoristaDoUsuarioAsync() : null;

            IQueryable<Viagem> consulta = _db.Viagens
                .Include(v => v.Van)
                .Include(v => v.Motorista)
                .Include(v => v.Rota)
                .OrderByDescending(v => v.Data)
                .ThenByDescending(v => v.Id);

            if (papel == "Motorista")
            {
                if (motorista == null)
                {
                    ViewData["Papel"] = papel;
                    ViewData["MotoristaId"] = null;
                    ViewData["Calendario"] = CriarCalendario(Array.Empty<Viagem>(), mes, ano);
                    return View(new List<Viagem>());
                }

                consulta = consulta.Where(v => v.MotoristaId == motorista.Id);
            }

            var viagens = await consulta.ToListAsync();
            ViewData["Papel"] = papel;
            ViewData["MotoristaId"] = motorista?.Id;
            ViewData["Calendario"] = CriarCalendario(viagens, mes, ano);
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

            if (PapelLogado() == "Motorista")
            {
                var motorista = await MotoristaDoUsuarioAsync();
                if (motorista == null || viagem.MotoristaId != motorista.Id)
                {
                    return RedirectToAction("AcessoRestrito", "Home");
                }
            }

            var alunosDaViagem = await _db.PresencasViagem
                .Where(p => p.ViagemId == id)
                .Include(p => p.Aluno)
                .OrderBy(p => p.Aluno!.Nome)
                .ToListAsync();

            ViewData["AlunosDaViagem"] = alunosDaViagem;
            ViewData["Papel"] = PapelLogado();
            return View(viagem);
        }

        // Nova viagem (Admin ou Motorista).
        public async Task<IActionResult> Create()
        {
            var model = new ViagemFormViewModel { Data = DateTime.Today };
            await PrepararFormularioAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ViagemFormViewModel model)
        {
            if (PapelLogado() == "Motorista")
            {
                var motorista = await MotoristaDoUsuarioAsync();
                if (motorista == null)
                {
                    ModelState.AddModelError(string.Empty, "Seu usuário ainda não está vinculado a um perfil de motorista. Solicite a associação ao administrador.");
                }
                else
                {
                    model.MotoristaId = motorista.Id;
                }
            }

            await PrepararFormularioAsync(model);

            if (!await _db.Vans.AnyAsync(v => v.Id == model.VanId))
                ModelState.AddModelError(nameof(model.VanId), "Selecione uma van cadastrada.");
            if (!await _db.Motoristas.AnyAsync(m => m.Id == model.MotoristaId && m.Ativo))
                ModelState.AddModelError(nameof(model.MotoristaId), "Selecione um motorista ativo.");
            if (!await _db.Rotas.AnyAsync(r => r.Id == model.RotaId && r.Ativo))
                ModelState.AddModelError(nameof(model.RotaId), "Selecione uma rota ativa.");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var fusoBrasil = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            var dataLocal = DateTime.SpecifyKind(model.Data.Date, DateTimeKind.Unspecified);

            var viagem = new Viagem
            {
                VanId = model.VanId,
                MotoristaId = model.MotoristaId,
                RotaId = model.RotaId,
                Data = TimeZoneInfo.ConvertTimeToUtc(dataLocal, fusoBrasil),
                HoraInicio = string.IsNullOrWhiteSpace(model.HoraInicio) ? null : TimeSpan.ParseExact(model.HoraInicio, @"hh\:mm", CultureInfo.InvariantCulture),
                HoraFim = string.IsNullOrWhiteSpace(model.HoraFim) ? null : TimeSpan.ParseExact(model.HoraFim, @"hh\:mm", CultureInfo.InvariantCulture),
                Observacao = Services.ValidacaoBrasil.LimparTexto(model.Observacao, 500),
                Status = "Agendada",
                DataCriacao = DateTime.UtcNow
            };

            _db.Viagens.Add(viagem);
            await _db.SaveChangesAsync();

            await CriarPresencasAsync(viagem.Id);
            TempData["Sucesso"] = "Viagem agendada com sucesso.";

            _auditoria.Registrar(
                UsuarioIdLogado(),
                NomeUsuarioLogado(),
                "Criacao",
                "Viagem",
                viagem.Id,
                $"Viagem #{viagem.Id} criada: {viagem.Status}",
                $"VanId: {viagem.VanId}, MotoristaId: {viagem.MotoristaId}, RotaId: {viagem.RotaId}, Data: {viagem.Data:dd/MM/yyyy}");

            return RedirectToAction(nameof(Details), new { id = viagem.Id });
        }

        // Iniciar viagem: Confirmada/Agendada -> EmAndamento, registra HoraInicio.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(int id)
        {
            var viagem = await _db.Viagens.FindAsync(id);
            if (viagem == null) return NotFound();

            if (!await PodeOperarAsync(viagem))
                return RedirectToAction("AcessoRestrito", "Home");

            var transicao = _validacao.ValidarTransicaoStatus(viagem.Status, "EmAndamento");
            if (!transicao.Valido)
            {
                TempData["Erro"] = transicao.Descricao;
                return RedirectToAction(nameof(Details), new { id });
            }

            // Validações operacionais antes de iniciar
            if (viagem.VanId == 0)
            {
                TempData["Erro"] = "Não é possível iniciar uma viagem sem van associada.";
                return RedirectToAction(nameof(Details), new { id });
            }
            if (viagem.MotoristaId == 0)
            {
                TempData["Erro"] = "Não é possível iniciar uma viagem sem motorista associado.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var van = await _db.Vans.FindAsync(viagem.VanId);
            if (van == null)
            {
                TempData["Erro"] = "Van associada não encontrada.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var motorista = await _db.Motoristas.FindAsync(viagem.MotoristaId);
            if (motorista == null)
            {
                TempData["Erro"] = "Motorista associado não encontrado.";
                return RedirectToAction(nameof(Details), new { id });
            }

            viagem.Status = "EmAndamento";
            viagem.HoraInicio = DateTime.Now.TimeOfDay;
            await _db.SaveChangesAsync();

            _auditoria.Registrar(
                UsuarioIdLogado(),
                NomeUsuarioLogado(),
                "MudancaEstado",
                "Viagem",
                viagem.Id,
                $"Viagem #{viagem.Id} iniciada: {viagem.Status} → EmAndamento",
                $"Van: {van.Plate}, Motorista: {motorista.Nome}, Data: {viagem.Data:dd/MM/yyyy}");

            TempData["Sucesso"] = "Viagem iniciada com sucesso.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Finalizar viagem: EmAndamento -> Finalizada, registra HoraFim.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Finish(int id)
        {
            var viagem = await _db.Viagens.FindAsync(id);
            if (viagem == null) return NotFound();

            if (!await PodeOperarAsync(viagem))
                return RedirectToAction("AcessoRestrito", "Home");

            var transicao = _validacao.ValidarTransicaoStatus(viagem.Status, "Finalizada");
            if (!transicao.Valido)
            {
                TempData["Erro"] = transicao.Descricao;
                return RedirectToAction(nameof(Details), new { id });
            }

            viagem.Status = "Finalizada";
            viagem.HoraFim = DateTime.Now.TimeOfDay;
            await _db.SaveChangesAsync();

            _auditoria.Registrar(
                UsuarioIdLogado(),
                NomeUsuarioLogado(),
                "MudancaEstado",
                "Viagem",
                viagem.Id,
                $"Viagem #{viagem.Id} finalizada: {viagem.Status} → Finalizada",
                $"Data: {viagem.Data:dd/MM/yyyy}");

            TempData["Sucesso"] = "Viagem finalizada com sucesso.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Confirmar viagem: Planejada/Agendada -> Confirmada.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirmar(int id)
        {
            var viagem = await _db.Viagens.FindAsync(id);
            if (viagem == null) return NotFound();

            if (!await PodeOperarAsync(viagem))
                return RedirectToAction("AcessoRestrito", "Home");

            var transicao = _validacao.ValidarTransicaoStatus(viagem.Status, "Confirmada");
            if (!transicao.Valido)
            {
                TempData["Erro"] = transicao.Descricao;
                return RedirectToAction(nameof(Details), new { id });
            }

            // Validações antes da confirmação
            if (viagem.VanId == 0)
            {
                TempData["Erro"] = "Não é possível confirmar uma viagem sem van associada.";
                return RedirectToAction(nameof(Details), new { id });
            }
            if (viagem.MotoristaId == 0)
            {
                TempData["Erro"] = "Não é possível confirmar uma viagem sem motorista associado.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var rotasExistem = await _db.Rotas.AnyAsync(r => r.Id == viagem.RotaId);
            if (!rotasExistem)
            {
                TempData["Erro"] = "Rota associada não encontrada.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var conflitos = _validacao.ValidarNovaViagem(
                viagem.VanId, viagem.MotoristaId, viagem.RotaId,
                viagem.Data, viagem.HoraInicio, viagem.HoraFim, viagem.Id);

            var conflitosImpeditivos = conflitos.Where(c => !c.Valido).ToList();
            if (conflitosImpeditivos.Any())
            {
                foreach (var conflito in conflitosImpeditivos)
                {
                    ModelState.AddModelError(string.Empty, conflito.Descricao);
                }
                return RedirectToAction(nameof(Details), new { id });
            }

            var van = await _db.Vans.FindAsync(viagem.VanId);
            var motorista = await _db.Motoristas.FindAsync(viagem.MotoristaId);

            viagem.Status = "Confirmada";
            await _db.SaveChangesAsync();

            _auditoria.Registrar(
                UsuarioIdLogado(),
                NomeUsuarioLogado(),
                "MudancaEstado",
                "Viagem",
                viagem.Id,
                $"Viagem #{viagem.Id} confirmada: {viagem.Status} → Confirmada",
                $"Van: {van?.Plate}, Motorista: {motorista?.Nome}, Data: {viagem.Data:dd/MM/yyyy}");

            TempData["Sucesso"] = "Viagem confirmada com sucesso.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Cancelar viagem: estados ativos -> Cancelada.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var viagem = await _db.Viagens.FindAsync(id);
            if (viagem == null) return NotFound();

            if (!await PodeOperarAsync(viagem))
                return RedirectToAction("AcessoRestrito", "Home");

            if (viagem.Status is "Finalizada" or "Cancelada")
            {
                TempData["Erro"] = $"Não é possível cancelar uma viagem já {viagem.Status.ToLower()}.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var statusAnterior = viagem.Status;
            viagem.Status = "Cancelada";
            await _db.SaveChangesAsync();

            _auditoria.Registrar(
                UsuarioIdLogado(),
                NomeUsuarioLogado(),
                "Cancelamento",
                "Viagem",
                viagem.Id,
                $"Viagem #{viagem.Id} cancelada: {statusAnterior} → Cancelada",
                $"Data: {viagem.Data:dd/MM/yyyy}");

            TempData["Sucesso"] = "Viagem cancelada com sucesso.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Marcar presença de um aluno na viagem.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarPresenca(int viagemId, int alunoId, bool presente, string? observacao)
        {
            var viagem = await _db.Viagens.FindAsync(viagemId);
            if (viagem == null) return NotFound();

            if (!await PodeOperarAsync(viagem))
                return RedirectToAction("AcessoRestrito", "Home");

            if (viagem.Status != "EmAndamento")
            {
                TempData["Erro"] = "A presença só pode ser marcada enquanto a viagem está em andamento.";
                return RedirectToAction(nameof(Details), new { id = viagemId });
            }

            var existe = await _db.PresencasViagem.FirstOrDefaultAsync(p => p.ViagemId == viagemId && p.AlunoId == alunoId);
            if (existe != null)
            {
                existe.Presente = presente;
                existe.Observacao = Services.ValidacaoBrasil.LimparTexto(observacao, 200);
            }
            else
            {
                _db.PresencasViagem.Add(new PresencaViagem
                {
                    ViagemId = viagemId,
                    AlunoId = alunoId,
                    Presente = presente,
                    Observacao = Services.ValidacaoBrasil.LimparTexto(observacao, 200)
                });
            }

            await _db.SaveChangesAsync();

            var aluno = await _db.Alunos.FindAsync(alunoId);
            _auditoria.Registrar(
                UsuarioIdLogado(),
                NomeUsuarioLogado(),
                "Presenca",
                "PresencaViagem",
                existe?.Id ?? 0,
                $"Presença {(presente ? "registrada" : "ausência registrada")} para aluno #{alunoId} na viagem #{viagemId}",
                $"Aluno: {aluno?.Nome}, Presente: {presente}");

            return RedirectToAction(nameof(Details), new { id = viagemId });
        }

        // Inicializa a lista de presença para todos os alunos ativos (útil ao iniciar a viagem).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InicializarPresenca(int id)
        {
            var viagem = await _db.Viagens.FindAsync(id);
            if (viagem == null) return NotFound();

            if (!await PodeOperarAsync(viagem))
                return RedirectToAction("AcessoRestrito", "Home");

            if (viagem.Status != "EmAndamento")
            {
                TempData["Erro"] = "A presença só pode ser inicializada enquanto a viagem está em andamento.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await CriarPresencasAsync(id);
            TempData["Sucesso"] = "Lista de presença preparada.";

            _auditoria.Registrar(
                UsuarioIdLogado(),
                NomeUsuarioLogado(),
                "Atualizacao",
                "Viagem",
                id,
                $"Lista de presença inicializada para viagem #{id}",
                $"Data: {DateTime.UtcNow:dd/MM/yyyy HH:mm}");

            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task<bool> PodeOperarAsync(Viagem viagem)
        {
            if (PapelLogado() != "Motorista") return true;

            var motorista = await MotoristaDoUsuarioAsync();
            return motorista != null && viagem.MotoristaId == motorista.Id;
        }

        private async Task CriarPresencasAsync(int viagemId)
        {
            var alunosAtivos = await _db.Alunos.Where(a => a.Ativo).Select(a => a.Id).ToListAsync();
            var existentes = await _db.PresencasViagem
                .Where(p => p.ViagemId == viagemId)
                .Select(p => p.AlunoId)
                .ToListAsync();

            foreach (var alunoId in alunosAtivos.Except(existentes))
            {
                _db.PresencasViagem.Add(new PresencaViagem
                {
                    ViagemId = viagemId,
                    AlunoId = alunoId,
                    Presente = false,
                    StatusConfirmacao = null
                });
            }

            await _db.SaveChangesAsync();
        }

        private async Task PrepararFormularioAsync(ViagemFormViewModel model)
        {
            var papel = PapelLogado();
            var motorista = papel == "Motorista" ? await MotoristaDoUsuarioAsync() : null;

            var vans = await _db.Vans.OrderBy(v => v.Plate).ToListAsync();
            var rotas = await _db.Rotas.Where(r => r.Ativo).OrderBy(r => r.Nome).ToListAsync();
            var motoristas = papel == "Motorista"
                ? (motorista == null ? new List<Motorista>() : new List<Motorista> { motorista })
                : await _db.Motoristas.Where(m => m.Ativo).OrderBy(m => m.Nome).ToListAsync();

            if (motorista != null && model.MotoristaId == 0)
            {
                model.MotoristaId = motorista.Id;
            }

            model.Vans = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                vans.Select(v => new { v.Id, Nome = $"{v.Plate} · {v.Capacity} lugares" }),
                "Id", "Nome", model.VanId);
            model.Motoristas = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(motoristas, "Id", "Nome", model.MotoristaId);
            model.Rotas = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                rotas.Select(r => new { r.Id, Nome = $"{r.Nome} ({r.Origem} → {r.Destino})" }),
                "Id", "Nome", model.RotaId);
        }

        private static CalendarioViagensViewModel CriarCalendario(IReadOnlyCollection<Viagem> viagens, int? mes, int? ano)
        {
            var hojeLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")).Date;
            var mesFinal = mes is >= 1 and <= 12 ? mes.Value : hojeLocal.Month;
            var anoFinal = ano is >= 1 and <= 9999 ? ano.Value : hojeLocal.Year;
            var referencia = new DateTime(anoFinal, mesFinal, 1);

            var inicio = referencia.AddDays(-(int)referencia.DayOfWeek);
            var dias = new List<CalendarioDiaViewModel>(42);

            for (var i = 0; i < 42; i++)
            {
                var dia = inicio.AddDays(i);
                var doMes = dia.Month == referencia.Month && dia.Year == referencia.Year;
                var viagensDoDia = doMes
                    ? viagens.Where(v => ParaDataLocal(v.Data).Date == dia.Date)
                        .OrderBy(v => v.HoraInicio ?? TimeSpan.MaxValue)
                        .ThenBy(v => v.Id)
                        .ToList()
                    : new List<Viagem>();

                dias.Add(new CalendarioDiaViewModel
                {
                    Data = dia,
                    DoMes = doMes,
                    Viagens = viagensDoDia
                });
            }

            return new CalendarioViagensViewModel
            {
                Mes = referencia.Month,
                Ano = referencia.Year,
                NomeMes = referencia.ToString("MMMM 'de' yyyy", new CultureInfo("pt-BR")),
                MesAnterior = referencia.AddMonths(-1).ToString("yyyy-MM"),
                MesSeguinte = referencia.AddMonths(1).ToString("yyyy-MM"),
                Dias = dias
            };
        }

        private static DateTime ParaDataLocal(DateTime data)
        {
            var utc = data.Kind == DateTimeKind.Utc ? data : DateTime.SpecifyKind(data, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")).Date;
        }
    }
}

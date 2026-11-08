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
        private readonly PresencaViagemService _presencas;
        private readonly IAuditoriaService _auditoria;
        private readonly ILogger<ViagensController> _logger;

        public ViagensController(
            ApplicationDbContext db,
            ValidacaoOperacional validacao,
            PresencaViagemService presencas,
            IAuditoriaService auditoria,
            ILogger<ViagensController> logger)
        {
            _db = db;
            _validacao = validacao;
            _presencas = presencas;
            _auditoria = auditoria;
            _logger = logger;
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
                .Include(v => v.Rota);

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

            // A ordenação por TimeSpan não é traduzida pelo SQLite (coluna TEXT);
            // ordenamos em memória para manter o mesmo resultado em SQLite e PostgreSQL.
            var viagens = (await consulta.ToListAsync())
                .OrderBy(v => v.Data)
                .ThenBy(v => v.HoraInicio ?? TimeSpan.MaxValue)
                .ThenBy(v => v.Id)
                .ToList();
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
            var hoje = ViagemAgenda.AgoraLocal().Date;
            var model = new ViagemFormViewModel
            {
                Data = hoje,
                DataChegada = hoje,
                HoraInicio = "07:00",
                HoraFim = "08:00",
                Sentido = SentidoViagem.Ida
            };
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

            if (!model.Sentido.HasValue)
                ModelState.AddModelError(nameof(model.Sentido), "Selecione o sentido da viagem.");
            if (!await _db.Vans.AnyAsync(v => v.Id == model.VanId))
                ModelState.AddModelError(nameof(model.VanId), "Selecione uma van cadastrada.");
            if (!await _db.Motoristas.AnyAsync(m => m.Id == model.MotoristaId && m.Ativo))
                ModelState.AddModelError(nameof(model.MotoristaId), "Selecione um motorista ativo e cadastrado.");
            if (!await _db.Rotas.AnyAsync(r => r.Id == model.RotaId && r.Ativo))
                ModelState.AddModelError(nameof(model.RotaId), "Selecione uma rota ativa.");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var errosAgenda = ViagemAgenda.ValidarPlanejamento(
                model.Data,
                model.HoraInicio,
                model.DataChegada,
                model.HoraFim,
                exigirFuturo: true);

            foreach (var erro in errosAgenda)
            {
                ModelState.AddModelError(string.Empty, erro);
            }

            if (errosAgenda.Count > 0)
            {
                return View(model);
            }

            if (!ValidacaoBrasil.HorarioValido(model.HoraInicio, out var inicio)
                || !ValidacaoBrasil.HorarioValido(model.HoraFim, out var fim))
            {
                ModelState.AddModelError(string.Empty, "Use horários válidos no formato HH:mm.");
                return View(model);
            }

            await using var transacao = await _db.Database.BeginTransactionAsync();

            try
            {
                var viagem = new Viagem
                {
                    VanId = model.VanId,
                    MotoristaId = model.MotoristaId,
                    RotaId = model.RotaId,
                    Sentido = model.Sentido,
                    Data = DateTime.SpecifyKind(model.Data.Date, DateTimeKind.Unspecified),
                    DataChegada = DateTime.SpecifyKind(model.DataChegada.Date, DateTimeKind.Unspecified),
                    HoraInicio = inicio,
                    HoraFim = fim,
                    Observacao = ValidacaoBrasil.LimparTexto(model.Observacao, 500),
                    Status = "Agendada",
                    DataCriacao = DateTime.UtcNow
                };

                _db.Viagens.Add(viagem);
                await _db.SaveChangesAsync();

                var geracao = await CriarPresencasAsync(viagem.Id);

                _auditoria.Registrar(
                    UsuarioIdLogado(),
                    NomeUsuarioLogado(),
                    "Criacao",
                    "Viagem",
                    viagem.Id,
                    $"Viagem criada para {ViagemAgenda.FormatarData(viagem.Data)} às {inicio:hh\\:mm}",
                    $"VanId: {viagem.VanId}, MotoristaId: {viagem.MotoristaId}, RotaId: {viagem.RotaId}");

                await _db.SaveChangesAsync();
                await transacao.CommitAsync();

                TempData["Sucesso"] = geracao.UsouFallback
                    ? "Viagem agendada. Não há vínculos de alunos para esta rota e sentido; a lista de presença usou todos os alunos ativos."
                    : "Viagem agendada com sucesso.";
                return RedirectToAction(nameof(Details), new { id = viagem.Id });
            }
            catch (Exception ex)
            {
                await transacao.RollbackAsync();
                _logger.LogError(ex, "Falha ao criar viagem. VanId={VanId}, MotoristaId={MotoristaId}, RotaId={RotaId}",
                    model.VanId, model.MotoristaId, model.RotaId);
                ModelState.AddModelError(string.Empty, "Não foi possível agendar a viagem. Verifique os dados e tente novamente.");
                return View(model);
            }
        }

        // Edição de viagem ainda não iniciada. Datas passadas continuam bloqueadas.
        public async Task<IActionResult> Edit(int id)
        {
            var viagem = await _db.Viagens.FindAsync(id);
            if (viagem == null) return NotFound();

            if (!await PodeOperarAsync(viagem))
                return RedirectToAction("AcessoRestrito", "Home");

            if (viagem.Status is not ("Agendada" or "Confirmada"))
            {
                TempData["Erro"] = "Somente viagens agendadas ou confirmadas podem ser editadas.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var model = new ViagemFormViewModel
            {
                Id = viagem.Id,
                VanId = viagem.VanId ?? 0,
                MotoristaId = viagem.MotoristaId ?? 0,
                RotaId = viagem.RotaId,
                Sentido = viagem.Sentido,
                Data = viagem.Data.Date,
                DataChegada = (viagem.DataChegada ?? viagem.Data).Date,
                HoraInicio = viagem.HoraInicio?.ToString(@"hh\:mm"),
                HoraFim = viagem.HoraFim?.ToString(@"hh\:mm"),
                Observacao = viagem.Observacao
            };

            await PrepararFormularioAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ViagemFormViewModel model)
        {
            var viagem = await _db.Viagens.FindAsync(id);
            if (viagem == null) return NotFound();

            if (!await PodeOperarAsync(viagem))
                return RedirectToAction("AcessoRestrito", "Home");

            if (viagem.Status is not ("Agendada" or "Confirmada"))
            {
                TempData["Erro"] = "Somente viagens agendadas ou confirmadas podem ser editadas.";
                return RedirectToAction(nameof(Details), new { id });
            }

            model.Id = id;

            if (PapelLogado() == "Motorista")
            {
                var motorista = await MotoristaDoUsuarioAsync();
                if (motorista == null)
                {
                    ModelState.AddModelError(string.Empty, "Seu usuário ainda não está vinculado a um perfil de motorista.");
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
                ModelState.AddModelError(nameof(model.MotoristaId), "Selecione um motorista ativo e cadastrado.");
            if (!await _db.Rotas.AnyAsync(r => r.Id == model.RotaId && r.Ativo))
                ModelState.AddModelError(nameof(model.RotaId), "Selecione uma rota ativa.");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var errosAgenda = ViagemAgenda.ValidarPlanejamento(
                model.Data,
                model.HoraInicio,
                model.DataChegada,
                model.HoraFim,
                exigirFuturo: true);

            foreach (var erro in errosAgenda)
            {
                ModelState.AddModelError(string.Empty, erro);
            }

            if (errosAgenda.Count > 0
                || !ValidacaoBrasil.HorarioValido(model.HoraInicio, out var inicio)
                || !ValidacaoBrasil.HorarioValido(model.HoraFim, out var fim))
            {
                return View(model);
            }

            var dataAnterior = viagem.Data;
            var horarioAnterior = viagem.HoraInicio;

            viagem.VanId = model.VanId;
            viagem.MotoristaId = model.MotoristaId;
            viagem.RotaId = model.RotaId;
            viagem.Sentido = model.Sentido;
            viagem.Data = DateTime.SpecifyKind(model.Data.Date, DateTimeKind.Unspecified);
            viagem.DataChegada = DateTime.SpecifyKind(model.DataChegada.Date, DateTimeKind.Unspecified);
            viagem.HoraInicio = inicio;
            viagem.HoraFim = fim;
            viagem.Observacao = ValidacaoBrasil.LimparTexto(model.Observacao, 500);

            try
            {
                _auditoria.Registrar(
                    UsuarioIdLogado(),
                    NomeUsuarioLogado(),
                    "Edicao",
                    "Viagem",
                    viagem.Id,
                    $"Viagem editada para {ViagemAgenda.FormatarData(viagem.Data)} às {inicio:hh\\:mm}",
                    $"Antes: {dataAnterior:dd/MM/yyyy} {horarioAnterior:hh\\:mm}; depois: {viagem.Data:dd/MM/yyyy} {inicio:hh\\:mm}");

                await _db.SaveChangesAsync();
                TempData["Sucesso"] = "Viagem atualizada com sucesso.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao editar viagem {ViagemId}", id);
                ModelState.AddModelError(string.Empty, "Não foi possível atualizar a viagem. Tente novamente.");
                return View(model);
            }
        }

        // Iniciar viagem: Confirmada/Agendada -> EmAndamento, registra o instante real.
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
            if (!viagem.VanId.HasValue || viagem.VanId.Value == 0)
            {
                TempData["Erro"] = "Não é possível iniciar uma viagem sem van associada.";
                return RedirectToAction(nameof(Details), new { id });
            }
            if (!viagem.MotoristaId.HasValue || viagem.MotoristaId.Value == 0)
            {
                TempData["Erro"] = "Não é possível iniciar uma viagem sem motorista associado.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var van = await _db.Vans.FindAsync(viagem.VanId.Value);
            if (van == null)
            {
                TempData["Erro"] = "Van associada não encontrada.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var motorista = await _db.Motoristas.FindAsync(viagem.MotoristaId.Value);
            if (motorista == null)
            {
                TempData["Erro"] = "Motorista associado não encontrado.";
                return RedirectToAction(nameof(Details), new { id });
            }

            viagem.Status = "EmAndamento";
            viagem.InicioReal = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _auditoria.Registrar(
                UsuarioIdLogado(),
                NomeUsuarioLogado(),
                "MudancaEstado",
                "Viagem",
                viagem.Id,
                $"Viagem iniciada: {viagem.Status} → EmAndamento",
                $"Van: {van.Plate}, Motorista: {motorista.Nome}, Início real: {viagem.InicioReal:O}");

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
            viagem.FimReal = DateTime.UtcNow;
            if (viagem.InicioReal == null || viagem.FimReal < viagem.InicioReal)
            {
                viagem.InicioReal = viagem.FimReal;
            }
            await _db.SaveChangesAsync();

            _auditoria.Registrar(
                UsuarioIdLogado(),
                NomeUsuarioLogado(),
                "MudancaEstado",
                "Viagem",
                viagem.Id,
                $"Viagem finalizada: {viagem.Status} → Finalizada",
                $"Início real: {viagem.InicioReal:O}, Fim real: {viagem.FimReal:O}");

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
            if (!viagem.VanId.HasValue || viagem.VanId.Value == 0)
            {
                TempData["Erro"] = "Não é possível confirmar uma viagem sem van associada.";
                return RedirectToAction(nameof(Details), new { id });
            }
            if (!viagem.MotoristaId.HasValue || viagem.MotoristaId.Value == 0)
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
                viagem.VanId.Value, viagem.MotoristaId.Value, viagem.RotaId,
                viagem.Data, viagem.HoraInicio, viagem.HoraFim, viagem.Id);

            var conflitosImpeditivos = conflitos.Where(c => !c.Valido).ToList();
            if (conflitosImpeditivos.Any())
            {
                TempData["Erro"] = string.Join(" ", conflitosImpeditivos.Select(c => c.Descricao));
                return RedirectToAction(nameof(Details), new { id });
            }

            var van = await _db.Vans.FindAsync(viagem.VanId.Value);
            var motorista = await _db.Motoristas.FindAsync(viagem.MotoristaId.Value);

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

            var geracao = await CriarPresencasAsync(id);
            TempData["Sucesso"] = geracao.UsouFallback
                ? "Lista de presença preparada com todos os alunos ativos (não há vínculos para esta rota e sentido)."
                : "Lista de presença preparada.";

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

        private Task<ResultadoGeracaoPresencas> CriarPresencasAsync(int viagemId)
            => _presencas.GerarAsync(viagemId);

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
                vans.Select(v => new { v.Id, Nome = $"{VanUi.Identificacao(v)} · {v.Capacity} lugares" }),
                "Id", "Nome", model.VanId);
            model.Motoristas = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(motoristas, "Id", "Nome", model.MotoristaId);
            model.Rotas = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                rotas.Select(r => new { r.Id, Nome = $"{r.Nome} ({r.Origem} → {r.Destino})" }),
                "Id", "Nome", model.RotaId);
            model.Sentidos = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                new[] { SentidoViagem.Ida, SentidoViagem.Volta },
                model.Sentido);
        }

        private static CalendarioViagensViewModel CriarCalendario(IReadOnlyCollection<Viagem> viagens, int? mes, int? ano)
        {
            var hojeLocal = ViagemAgenda.AgoraLocal().Date;
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
                    ? viagens.Where(v => v.Data.Date == dia.Date)
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
    }
}

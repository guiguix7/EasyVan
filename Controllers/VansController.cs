using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EasyVan.Data;
using EasyVan.Models;

namespace Gerenciamento_de_Van.Controllers
{
    public class VansController : Controller
    {
        private readonly ApplicationDbContext _db;

        public VansController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET /Vans?busca=&status=&capacidadeMin=&capacidadeMax=
        [Authorize]
        public async Task<IActionResult> Index([FromQuery] VanFiltro filtro)
        {
            var statusDisponiveis = await _db.Vans
                .Select(v => v.Status)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            ValidarFiltro(filtro, statusDisponiveis);

            // Critério inválido é ignorado (a mensagem de erro já foi registrada); os demais continuam valendo.
            var busca = CampoValido(nameof(VanFiltro.Busca)) ? filtro.Busca : null;
            var status = CampoValido(nameof(VanFiltro.Status)) ? filtro.Status : null;
            var capacidadeMin = CampoValido(nameof(VanFiltro.CapacidadeMin)) ? filtro.CapacidadeMin : null;
            var capacidadeMax = CampoValido(nameof(VanFiltro.CapacidadeMax)) ? filtro.CapacidadeMax : null;

            if (capacidadeMin.HasValue && capacidadeMax.HasValue && capacidadeMin > capacidadeMax)
            {
                ModelState.AddModelError(nameof(VanFiltro.CapacidadeMin), "A capacidade mínima não pode ser maior que a máxima.");
                capacidadeMin = null;
                capacidadeMax = null;
            }

            var vans = await _db.Vans.ToListAsync();
            var resultado = AplicarFiltros(vans, busca, status, capacidadeMin, capacidadeMax);

            return View(new VanIndexViewModel
            {
                Filtro = filtro,
                Vans = resultado,
                TotalGeral = vans.Count,
                StatusDisponiveis = statusDisponiveis
            });
        }

        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var van = await _db.Vans
                .Include(v => v.Motorista)
                .Include(v => v.Rota)
                .FirstOrDefaultAsync(v => v.Id == id);
            if (van == null) return NotFound();
            return View(van);
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create()
        {
            var model = new VanFormViewModel { Horarios = new List<string> { "07:00" } };
            return View(await PrepararFormularioAsync(model));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create(VanFormViewModel model)
        {
            await PrepararFormularioAsync(model);
            if (!ModelState.IsValid) return View(model);

            var motorista = await _db.Motoristas.FindAsync(model.MotoristaId);
            var rota = await _db.Rotas.FindAsync(model.RotaId);
            if (motorista == null)
                ModelState.AddModelError(nameof(model.MotoristaId), "Selecione um motorista cadastrado.");
            if (rota == null)
                ModelState.AddModelError(nameof(model.RotaId), "Selecione uma rota cadastrada.");
            if (!ModelState.IsValid) return View(model);

            var placa = model.Plate.Trim().ToUpperInvariant();
            if (await _db.Vans.AnyAsync(v => v.Plate == placa))
            {
                ModelState.AddModelError(nameof(model.Plate), "Já existe uma van com esta placa.");
                return View(model);
            }

            var van = new Van();
            AplicarFormulario(van, model, motorista!, rota!);
            _db.Vans.Add(van);
            await _db.SaveChangesAsync();

            TempData["Sucesso"] = "Van cadastrada com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id)
        {
            var van = await _db.Vans
                .Include(v => v.Motorista)
                .Include(v => v.Rota)
                .FirstOrDefaultAsync(v => v.Id == id);
            if (van == null) return NotFound();

            var model = new VanFormViewModel
            {
                Id = van.Id,
                Plate = van.Plate,
                Modelo = van.Modelo ?? string.Empty,
                Capacity = van.Capacity,
                Status = van.Status,
                Description = van.Description,
                MotoristaId = van.MotoristaId,
                RotaId = van.RotaId,
                Horarios = ExtrairHorarios(van.Schedule)
            };

            // Registros antigos guardavam apenas o nome. Quando possível, sugerimos o vínculo real.
            if (!model.MotoristaId.HasValue && !string.IsNullOrWhiteSpace(van.Driver))
            {
                model.MotoristaId = await _db.Motoristas
                    .Where(m => m.Nome == van.Driver)
                    .Select(m => (int?)m.Id)
                    .FirstOrDefaultAsync();
            }

            if (!model.RotaId.HasValue && !string.IsNullOrWhiteSpace(van.Route))
            {
                model.RotaId = await _db.Rotas
                    .Where(r => r.Nome == van.Route)
                    .Select(r => (int?)r.Id)
                    .FirstOrDefaultAsync();
            }

            return View(await PrepararFormularioAsync(model));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VanFormViewModel model)
        {
            if (id != model.Id) return NotFound();
            await PrepararFormularioAsync(model);
            if (!ModelState.IsValid) return View(model);

            var existente = await _db.Vans.FindAsync(id);
            if (existente == null) return NotFound();

            var motorista = await _db.Motoristas.FindAsync(model.MotoristaId);
            var rota = await _db.Rotas.FindAsync(model.RotaId);
            if (motorista == null)
                ModelState.AddModelError(nameof(model.MotoristaId), "Selecione um motorista cadastrado.");
            if (rota == null)
                ModelState.AddModelError(nameof(model.RotaId), "Selecione uma rota cadastrada.");
            if (!ModelState.IsValid) return View(model);

            var placa = model.Plate.Trim().ToUpperInvariant();
            if (await _db.Vans.AnyAsync(v => v.Plate == placa && v.Id != id))
            {
                ModelState.AddModelError(nameof(model.Plate), "Já existe uma van com esta placa.");
                return View(model);
            }

            AplicarFormulario(existente, model, motorista!, rota!);
            await _db.SaveChangesAsync();

            TempData["Sucesso"] = "Van atualizada com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            var van = await _db.Vans.FindAsync(id);
            if (van == null) return NotFound();
            return View(van);
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var van = await _db.Vans.FindAsync(id);
            if (van != null)
            {
                if (await _db.Viagens.AnyAsync(v => v.VanId == id))
                {
                    TempData["Erro"] = "Esta van possui viagens registradas e não pode ser excluída. Altere o status para Manutenção ou mantenha o histórico.";
                    return RedirectToAction(nameof(Index));
                }

                _db.Vans.Remove(van);
                await _db.SaveChangesAsync();
                TempData["Sucesso"] = "Van excluída.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<VanFormViewModel> PrepararFormularioAsync(VanFormViewModel model)
        {
            var motoristas = await _db.Motoristas
                .Where(m => m.Ativo || m.Id == model.MotoristaId)
                .OrderBy(m => m.Nome)
                .ToListAsync();

            var rotas = await _db.Rotas
                .Where(r => r.Ativo || r.Id == model.RotaId)
                .OrderBy(r => r.Nome)
                .ToListAsync();

            model.Motoristas = new SelectList(motoristas, "Id", "Nome", model.MotoristaId);
            model.Rotas = new SelectList(
                rotas.Select(r => new { r.Id, Nome = $"{r.Nome} ({r.Origem} → {r.Destino})" }),
                "Id",
                "Nome",
                model.RotaId);

            if (model.Horarios.Count == 0)
            {
                model.Horarios.Add(string.Empty);
            }

            return model;
        }

        private static void AplicarFormulario(Van van, VanFormViewModel model, Motorista motorista, Rota rota)
        {
            var horarios = model.Horarios
                .Where(h => !string.IsNullOrWhiteSpace(h))
                .Select(h => h.Trim())
                .Distinct()
                .OrderBy(h => TimeSpan.ParseExact(h, @"hh\:mm", CultureInfo.InvariantCulture))
                .ToList();

            van.Plate = model.Plate.Trim().ToUpperInvariant();
            van.Modelo = model.Modelo.Trim();
            van.Capacity = model.Capacity;
            van.Status = model.Status;
            van.Description = model.Description?.Trim() ?? string.Empty;
            van.MotoristaId = motorista.Id;
            van.RotaId = rota.Id;
            van.Driver = motorista.Nome;
            van.Route = rota.Nome;
            van.Schedule = string.Join(" / ", horarios);
        }

        private static List<string> ExtrairHorarios(string? schedule)
        {
            if (string.IsNullOrWhiteSpace(schedule))
            {
                return new List<string> { string.Empty };
            }

            var horarios = schedule
                .Split(new[] { '/', ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(h => h.Trim())
                .Where(h => h.Length > 0)
                .ToList();

            return horarios.Count > 0 ? horarios : new List<string> { string.Empty };
        }

        private void ValidarFiltro(VanFiltro filtro, IReadOnlyCollection<string> statusDisponiveis)
        {
            if (filtro.Busca != null && filtro.Busca.Trim().Length > VanFiltro.TamanhoMaximoBusca)
            {
                ModelState.AddModelError(nameof(VanFiltro.Busca), $"A busca deve ter no máximo {VanFiltro.TamanhoMaximoBusca} caracteres.");
            }

            var status = filtro.Status?.Trim();
            if (!string.IsNullOrEmpty(status) && !statusDisponiveis.Contains(status))
            {
                ModelState.AddModelError(nameof(VanFiltro.Status), "Status inválido.");
            }

            SubstituirMensagemDeErro(nameof(VanFiltro.CapacidadeMin), "A capacidade mínima deve ser um número inteiro entre 0 e 1000.");
            SubstituirMensagemDeErro(nameof(VanFiltro.CapacidadeMax), "A capacidade máxima deve ser um número inteiro entre 0 e 1000.");
        }

        private void SubstituirMensagemDeErro(string campo, string mensagem)
        {
            if (ModelState.TryGetValue(campo, out var entrada) && entrada.Errors.Count > 0)
            {
                entrada.Errors.Clear();
                ModelState.AddModelError(campo, mensagem);
            }
        }

        private bool CampoValido(string campo)
        {
            return !ModelState.TryGetValue(campo, out var entrada) || entrada.Errors.Count == 0;
        }

        // Todos os critérios informados são combinados com E lógico. Critério vazio não filtra.
        private static List<Van> AplicarFiltros(List<Van> vans, string? busca, string? status, int? capacidadeMin, int? capacidadeMax)
        {
            IEnumerable<Van> consulta = vans;

            var termos = NormalizarTexto(busca).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (termos.Length > 0)
            {
                consulta = consulta.Where(v =>
                {
                    var texto = MontarTextoDeBusca(v);
                    return termos.All(termo => texto.Contains(termo, StringComparison.Ordinal));
                });
            }

            var statusSelecionado = status?.Trim();
            if (!string.IsNullOrEmpty(statusSelecionado))
            {
                consulta = consulta.Where(v => string.Equals(v.Status, statusSelecionado, StringComparison.OrdinalIgnoreCase));
            }

            if (capacidadeMin.HasValue)
            {
                consulta = consulta.Where(v => v.Capacity >= capacidadeMin.Value);
            }

            if (capacidadeMax.HasValue)
            {
                consulta = consulta.Where(v => v.Capacity <= capacidadeMax.Value);
            }

            return consulta.ToList();
        }

        // Campos pesquisáveis: placa (com e sem hífen), modelo, motorista, rota e descrição.
        private static string MontarTextoDeBusca(Van van)
        {
            var placa = NormalizarTexto(van.Plate);
            return string.Join(' ', placa, placa.Replace("-", string.Empty), NormalizarTexto(van.Modelo), NormalizarTexto(van.Driver), NormalizarTexto(van.Route), NormalizarTexto(van.Description));
        }

        // Minúsculas e sem acentos: "João" -> "joao", "Manutenção" -> "manutencao".
        private static string NormalizarTexto(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return string.Empty;
            }

            var decomposto = texto.Trim().Normalize(NormalizationForm.FormD);
            var resultado = new StringBuilder(decomposto.Length);

            foreach (var caractere in decomposto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
                {
                    resultado.Append(caractere);
                }
            }

            return resultado.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }
    }
}

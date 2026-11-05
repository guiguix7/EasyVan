using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            var van = await _db.Vans.FirstOrDefaultAsync(v => v.Id == id);
            if (van == null) return NotFound();
            return View(van);
        }

        [Authorize(Roles = "Administrador")]
        public IActionResult Create()
        {
            return View(new Van());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create(Van van)
        {
            if (!ModelState.IsValid) return View(van);

            _db.Vans.Add(van);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id)
        {
            var van = await _db.Vans.FindAsync(id);
            if (van == null) return NotFound();
            return View(van);
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Van van)
        {
            if (id != van.Id) return NotFound();
            if (!ModelState.IsValid) return View(van);

            var existente = await _db.Vans.FindAsync(id);
            if (existente == null) return NotFound();

            existente.Plate = van.Plate;
            existente.Driver = van.Driver;
            existente.Capacity = van.Capacity;
            existente.Route = van.Route;
            existente.Schedule = van.Schedule;
            existente.Status = van.Status;
            existente.Description = van.Description;

            await _db.SaveChangesAsync();
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
                _db.Vans.Remove(van);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
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

        // Campos pesquisáveis: placa (com e sem hífen), motorista, rota e descrição.
        private static string MontarTextoDeBusca(Van van)
        {
            var placa = NormalizarTexto(van.Plate);
            return string.Join(' ', placa, placa.Replace("-", string.Empty), NormalizarTexto(van.Driver), NormalizarTexto(van.Route), NormalizarTexto(van.Description));
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

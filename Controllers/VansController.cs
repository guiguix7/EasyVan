using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using EasyVan.Models;

namespace Gerenciamento_de_Van.Controllers
{
    public class VansController : Controller
    {
        private static readonly List<Van> _vans = new List<Van>
        {
            new Van { Id = 1, Plate = "ABC-1234", Driver = "João Silva", Capacity = 12, Route = "Centro - Universidade", Schedule = "07:00 / 17:30", Status = "Operacional", Description = "Van para transporte de alunos da universidade." },
            new Van { Id = 2, Plate = "DEF-5678", Driver = "Maria Oliveira", Capacity = 10, Route = "Zona Leste - Centro", Schedule = "06:30 / 18:00", Status = "Manutenção", Description = "Van em manutenção preventiva. Retorno previsto em 3 dias." },
            new Van { Id = 3, Plate = "TUF-6769", Driver = "Aurudo da Silva", Capacity = 15, Route = "Centro - Distrito Industrial", Schedule = "08:00 / 19:00", Status = "Operacional", Description = "Van de maior capacidade para trajetos longos." }
        };

        // GET /Vans?busca=&status=&capacidadeMin=&capacidadeMax=
        public IActionResult Index([FromQuery] VanFiltro filtro)
        {
            var statusDisponiveis = _vans
                .Select(v => v.Status)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();

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

            var vans = AplicarFiltros(_vans, busca, status, capacidadeMin, capacidadeMax);

            return View(new VanIndexViewModel
            {
                Filtro = filtro,
                Vans = vans,
                TotalGeral = _vans.Count,
                StatusDisponiveis = statusDisponiveis
            });
        }

        public IActionResult Details(int id)
        {
            var van = _vans.FirstOrDefault(v => v.Id == id);
            if (van == null) return NotFound();
            return View(van);
        }

        private void ValidarFiltro(VanFiltro filtro, IReadOnlyCollection<string> statusDisponiveis)
        {
            if (filtro.Busca != null && filtro.Busca.Trim().Length > VanFiltro.TamanhoMaximoBusca)
            {
                ModelState.AddModelError(nameof(VanFiltro.Busca), $"A busca deve ter no máximo {VanFiltro.TamanhoMaximoBusca} caracteres.");
            }

            var status = filtro.Status?.Trim();
            if (!string.IsNullOrEmpty(status) && !statusDisponiveis.Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(VanFiltro.Status), "Status inválido.");
            }

            // Falhas de binding (ex.: "abc" ou número gigante) vêm com mensagem padrão em inglês; troca pela nossa.
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
        private static List<Van> AplicarFiltros(IEnumerable<Van> vans, string? busca, string? status, int? capacidadeMin, int? capacidadeMax)
        {
            var consulta = vans;

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

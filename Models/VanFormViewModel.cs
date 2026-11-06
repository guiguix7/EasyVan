using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EasyVan.Models
{
    public class VanFormViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe a placa.")]
        [StringLength(10, ErrorMessage = "A placa deve ter no máximo 10 caracteres.")]
        [Display(Name = "Placa")]
        public string Plate { get; set; } = string.Empty;

        [Range(1, 1000, ErrorMessage = "A capacidade deve estar entre 1 e 1000 passageiros.")]
        [Display(Name = "Capacidade")]
        public int Capacity { get; set; }

        [Required(ErrorMessage = "Informe o status da van.")]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Operacional";

        [Required(ErrorMessage = "Selecione o motorista responsável.")]
        [Display(Name = "Motorista")]
        public int? MotoristaId { get; set; }

        [Required(ErrorMessage = "Selecione a rota da van.")]
        [Display(Name = "Rota")]
        public int? RotaId { get; set; }

        [Display(Name = "Horários")]
        public List<string> Horarios { get; set; } = new();

        [StringLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres.")]
        [Display(Name = "Descrição")]
        public string? Description { get; set; }

        public IEnumerable<SelectListItem> Motoristas { get; set; } = Array.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> Rotas { get; set; } = Array.Empty<SelectListItem>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var horarios = Horarios
                .Where(h => !string.IsNullOrWhiteSpace(h))
                .Select(h => h.Trim())
                .ToList();

            if (horarios.Count == 0)
            {
                yield return new ValidationResult("Informe ao menos um horário.", new[] { nameof(Horarios) });
                yield break;
            }

            if (horarios.Count > 10)
            {
                yield return new ValidationResult("Informe no máximo 10 horários.", new[] { nameof(Horarios) });
            }

            var convertidos = new List<TimeSpan>();
            foreach (var horario in horarios)
            {
                if (!Services.ValidacaoBrasil.HorarioValido(horario, out var valor))
                {
                    yield return new ValidationResult("Use horários válidos no formato HH:mm.", new[] { nameof(Horarios) });
                }
                else
                {
                    convertidos.Add(valor);
                }
            }

            if (convertidos.Count != convertidos.Distinct().Count())
            {
                yield return new ValidationResult("Não repita o mesmo horário na van.", new[] { nameof(Horarios) });
            }
        }
    }
}

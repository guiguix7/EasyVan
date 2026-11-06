using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EasyVan.Models
{
    public class ViagemFormViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Selecione a van.")]
        [Display(Name = "Van")]
        public int VanId { get; set; }

        [Required(ErrorMessage = "Selecione o motorista.")]
        [Display(Name = "Motorista")]
        public int MotoristaId { get; set; }

        [Required(ErrorMessage = "Selecione a rota.")]
        [Display(Name = "Rota")]
        public int RotaId { get; set; }

        [Required(ErrorMessage = "Informe a data da viagem.")]
        [DataType(DataType.Date)]
        [Display(Name = "Data")]
        public DateTime Data { get; set; } = DateTime.Today;

        [Display(Name = "Início")]
        public string? HoraInicio { get; set; }

        [Display(Name = "Término")]
        public string? HoraFim { get; set; }

        [StringLength(500, ErrorMessage = "A observação deve ter no máximo 500 caracteres.")]
        [Display(Name = "Observação")]
        public string? Observacao { get; set; }

        public IEnumerable<SelectListItem> Vans { get; set; } = Array.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> Motoristas { get; set; } = Array.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> Rotas { get; set; } = Array.Empty<SelectListItem>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var temInicio = !string.IsNullOrWhiteSpace(HoraInicio);
            var temFim = !string.IsNullOrWhiteSpace(HoraFim);

            if (!temInicio && !temFim)
            {
                yield break;
            }

            if (!temInicio || !temFim)
            {
                yield return new ValidationResult("Informe o horário de início e o de término juntos.", new[] { nameof(HoraInicio), nameof(HoraFim) });
                yield break;
            }

            if (!Services.ValidacaoBrasil.HorarioValido(HoraInicio, out var inicio)
                || !Services.ValidacaoBrasil.HorarioValido(HoraFim, out var fim))
            {
                yield return new ValidationResult("Use horários válidos no formato HH:mm.", new[] { nameof(HoraInicio), nameof(HoraFim) });
                yield break;
            }

            if (fim <= inicio)
            {
                yield return new ValidationResult("O término deve ser posterior ao início.", new[] { nameof(HoraFim) });
            }
        }
    }
}

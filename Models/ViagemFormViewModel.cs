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

        [Required(ErrorMessage = "Informe a data prevista de partida.")]
        [DataType(DataType.Date)]
        [Display(Name = "Partida prevista")]
        public DateTime Data { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Informe a data prevista de chegada.")]
        [DataType(DataType.Date)]
        [Display(Name = "Chegada prevista")]
        public DateTime DataChegada { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Informe o horário previsto de partida.")]
        [Display(Name = "Horário de partida")]
        public string? HoraInicio { get; set; }

        [Required(ErrorMessage = "Informe o horário previsto de chegada.")]
        [Display(Name = "Horário de chegada")]
        public string? HoraFim { get; set; }

        [StringLength(500, ErrorMessage = "A observação deve ter no máximo 500 caracteres.")]
        [Display(Name = "Observação")]
        public string? Observacao { get; set; }

        public IEnumerable<SelectListItem> Vans { get; set; } = Array.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> Motoristas { get; set; } = Array.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> Rotas { get; set; } = Array.Empty<SelectListItem>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Data == default)
            {
                yield return new ValidationResult("Informe a data prevista de partida.", new[] { nameof(Data) });
                yield break;
            }

            if (DataChegada == default)
            {
                yield return new ValidationResult("Informe a data prevista de chegada.", new[] { nameof(DataChegada) });
                yield break;
            }

            var temInicio = !string.IsNullOrWhiteSpace(HoraInicio);
            var temFim = !string.IsNullOrWhiteSpace(HoraFim);

            if (!temInicio || !temFim)
            {
                yield return new ValidationResult("Informe o horário previsto de partida e o de chegada.", new[] { nameof(HoraInicio), nameof(HoraFim) });
                yield break;
            }

            if (!Services.ValidacaoBrasil.HorarioValido(HoraInicio, out var inicio)
                || !Services.ValidacaoBrasil.HorarioValido(HoraFim, out var fim))
            {
                yield return new ValidationResult("Use horários válidos no formato HH:mm.", new[] { nameof(HoraInicio), nameof(HoraFim) });
                yield break;
            }

            var partida = Data.Date.Add(inicio);
            var chegada = DataChegada.Date.Add(fim);
            if (chegada <= partida)
            {
                yield return new ValidationResult("A chegada prevista deve ser posterior à partida prevista.", new[] { nameof(DataChegada), nameof(HoraFim) });
            }
        }
    }
}

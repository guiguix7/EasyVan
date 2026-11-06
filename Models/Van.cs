using System.ComponentModel.DataAnnotations;

namespace EasyVan.Models
{
    public class Van
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe a placa.")]
        [StringLength(10, ErrorMessage = "A placa deve ter no máximo 10 caracteres.")]
        public string Plate { get; set; } = string.Empty;

        [StringLength(120, ErrorMessage = "O nome do motorista deve ter no máximo 120 caracteres.")]
        public string Driver { get; set; } = string.Empty;

        [Range(1, 1000, ErrorMessage = "A capacidade deve estar entre 1 e 1000 passageiros.")]
        public int Capacity { get; set; }

        [StringLength(160, ErrorMessage = "A rota deve ter no máximo 160 caracteres.")]
        public string Route { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "A lista de horários deve ter no máximo 100 caracteres.")]
        public string Schedule { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o status da van.")]
        [StringLength(20)]
        public string Status { get; set; } = "Operacional";

        [StringLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres.")]
        public string Description { get; set; } = string.Empty;

        public int? MotoristaId { get; set; }
        public Motorista? Motorista { get; set; }

        public int? RotaId { get; set; }
        public Rota? Rota { get; set; }
    }
}

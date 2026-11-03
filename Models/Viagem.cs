using System.ComponentModel.DataAnnotations;

namespace EasyVan.Models
{
    public class Viagem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "A van é obrigatória.")]
        public int VanId { get; set; }

        [Required(ErrorMessage = "O motorista é obrigatório.")]
        public int MotoristaId { get; set; }

        [Required(ErrorMessage = "A rota é obrigatória.")]
        public int RotaId { get; set; }

        [Required(ErrorMessage = "A data é obrigatória.")]
        public DateTime Data { get; set; } = DateTime.Today;

        public TimeSpan? HoraInicio { get; set; }

        public TimeSpan? HoraFim { get; set; }

        [Required(ErrorMessage = "O status é obrigatório.")]
        [StringLength(20)]
        public string Status { get; set; } = "Agendada"; // Agendada, EmAndamento, Finalizada, Cancelada

        public string? Observacao { get; set; }

        public DateTime DataCriacao { get; set; } = DateTime.Now;

        // Propriedades de navegação
        public Van? Van { get; set; }
        public Motorista? Motorista { get; set; }
        public Rota? Rota { get; set; }
    }
}

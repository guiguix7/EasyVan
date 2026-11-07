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

        // Data prevista de partida, armazenada como meia-noite UTC do dia local.
        [Required(ErrorMessage = "A data é obrigatória.")]
        public DateTime Data { get; set; } = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified);

        // Horários previstos (planejamento), independentes dos horários reais.
        public TimeSpan? HoraInicio { get; set; }

        public TimeSpan? HoraFim { get; set; }

        // Data prevista de chegada; quando nula, usa a mesma data da partida.
        public DateTime? DataChegada { get; set; }

        // Instantes reais registrados pelo servidor em UTC.
        public DateTime? InicioReal { get; set; }

        public DateTime? FimReal { get; set; }

        [Required(ErrorMessage = "O status é obrigatório.")]
        [StringLength(20)]
        public string Status { get; set; } = "Agendada"; // Agendada, EmAndamento, Finalizada, Cancelada

        public string? Observacao { get; set; }

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        // Propriedades de navegação
        public Van? Van { get; set; }
        public Motorista? Motorista { get; set; }
        public Rota? Rota { get; set; }
        public ICollection<PresencaViagem> Presencas { get; set; } = new List<PresencaViagem>();
    }
}

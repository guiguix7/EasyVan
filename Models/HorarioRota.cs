using System.ComponentModel.DataAnnotations;

namespace EasyVan.Models
{
    public class HorarioRota : IOrganizacaoScoped
    {
        public int Id { get; set; }
        public int OrganizacaoId { get; set; } = 1;

        [Required(ErrorMessage = "A rota é obrigatória.")]
        public int RotaId { get; set; }

        [Required(ErrorMessage = "O sentido é obrigatório.")]
        public SentidoViagem Sentido { get; set; }

        [Required(ErrorMessage = "Informe ao menos um dia da semana.")]
        public DiasSemana DiasSemana { get; set; }

        [Required(ErrorMessage = "O horário de saída é obrigatório.")]
        public TimeSpan HorarioSaida { get; set; }

        public int? VanPadraoId { get; set; }

        public bool Ativo { get; set; } = true;

        public Rota? Rota { get; set; }
        public Van? VanPadrao { get; set; }
        public ICollection<Viagem> Viagens { get; set; } = new List<Viagem>();
    }
}

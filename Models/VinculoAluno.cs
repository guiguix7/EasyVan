using System.ComponentModel.DataAnnotations;

namespace EasyVan.Models
{
    public class VinculoAluno : IOrganizacaoScoped
    {
        public int Id { get; set; }
        public int OrganizacaoId { get; set; } = 1;

        [Required(ErrorMessage = "O aluno é obrigatório.")]
        public int AlunoId { get; set; }

        [Required(ErrorMessage = "A rota é obrigatória.")]
        public int RotaId { get; set; }

        [Required(ErrorMessage = "A parada é obrigatória.")]
        public int ParadaId { get; set; }

        [Required(ErrorMessage = "O sentido é obrigatório.")]
        public SentidoViagem Sentido { get; set; }

        public bool Ativo { get; set; } = true;

        public DateTime? DataInicio { get; set; }

        public DateTime? DataFim { get; set; }

        [StringLength(300)]
        public string? Observacao { get; set; }

        public Aluno? Aluno { get; set; }
        public Rota? Rota { get; set; }
        public Parada? Parada { get; set; }
    }
}

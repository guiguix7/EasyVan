using System.ComponentModel.DataAnnotations;

namespace EasyVan.Models
{
    public class Parada : IOrganizacaoScoped
    {
        public int Id { get; set; }
        public int OrganizacaoId { get; set; } = 1;

        [Required(ErrorMessage = "A rota é obrigatória.")]
        public int RotaId { get; set; }

        [Required(ErrorMessage = "O sentido é obrigatório.")]
        public SentidoViagem Sentido { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A ordem da parada deve ser maior que zero.")]
        public int Ordem { get; set; }

        [StringLength(120)]
        public string? Nome { get; set; }

        [Required(ErrorMessage = "O endereço é obrigatório.")]
        [StringLength(200)]
        public string Endereco { get; set; } = string.Empty;

        public TimeSpan? HorarioPrevisto { get; set; }

        public bool Ativo { get; set; } = true;

        public Rota? Rota { get; set; }
        public ICollection<VinculoAluno> Vinculos { get; set; } = new List<VinculoAluno>();
    }
}

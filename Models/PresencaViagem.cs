using System.ComponentModel.DataAnnotations;

namespace EasyVan.Models
{
    public class PresencaViagem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "A viagem é obrigatória.")]
        public int ViagemId { get; set; }

        [Required(ErrorMessage = "O aluno é obrigatório.")]
        public int AlunoId { get; set; }

        public bool Presente { get; set; }

        public string? Observacao { get; set; }

        // Propriedades de navegação
        public Viagem? Viagem { get; set; }
        public Aluno? Aluno { get; set; }
    }
}

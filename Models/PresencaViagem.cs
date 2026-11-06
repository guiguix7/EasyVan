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

        /// <summary>
        /// Resposta do aluno: null = ainda não respondeu, true = vou, false = não vou.
        /// É separada de <see cref="Presente"/>, que registra a presença efetiva no embarque.
        /// </summary>
        public bool? StatusConfirmacao { get; set; }

        public DateTime? ConfirmadoEm { get; set; }

        public string? Observacao { get; set; }

        // Propriedades de navegação
        public Viagem? Viagem { get; set; }
        public Aluno? Aluno { get; set; }
    }
}

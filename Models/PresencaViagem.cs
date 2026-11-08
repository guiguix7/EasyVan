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
        /// Resposta do aluno. Separada de <see cref="Presente"/>, que registra o embarque efetivo.
        /// </summary>
        public StatusPresenca Status { get; set; } = StatusPresenca.SemResposta;

        public DateTime? RespondidoEm { get; set; }

        /// <summary>
        /// Coluna legada mantida até a migração de limpeza. Use <see cref="Status"/> no código novo.
        /// </summary>
        public bool? StatusConfirmacao { get; set; }

        /// <summary>
        /// Coluna legada mantida até a migração de limpeza. Use <see cref="RespondidoEm"/> no código novo.
        /// </summary>
        public DateTime? ConfirmadoEm { get; set; }

        public string? Observacao { get; set; }

        // Propriedades de navegação
        public Viagem? Viagem { get; set; }
        public Aluno? Aluno { get; set; }
    }
}

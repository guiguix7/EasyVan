using System.ComponentModel.DataAnnotations;

namespace EasyVan.Models
{
    /// <summary>
    /// Registro de auditoria de operações relevantes no sistema.
    /// Não armazena senhas, hashes, tokens nem dados pessoais desnecessários.
    /// </summary>
    public class LogOperacao : IOrganizacaoScoped
    {
        public int Id { get; set; }
        public int OrganizacaoId { get; set; } = 1;

        public int? UsuarioId { get; set; }

        [StringLength(120)]
        public string? UsuarioNome { get; set; }

        [Required]
        [StringLength(50)]
        public string TipoOperacao { get; set; } = string.Empty; // Criacao, Atualizacao, MudancaEstado, Cancelamento, Exclusao, Presenca

        [Required]
        [StringLength(50)]
        public string Entidade { get; set; } = string.Empty; // Viagem, Van, Motorista, Aluno, Rota, Usuario

        public int? EntidadeId { get; set; }

        [StringLength(500)]
        public string? Descricao { get; set; }

        public DateTime DataOperacao { get; set; } = DateTime.UtcNow;

        [StringLength(200)]
        public string? InformacoesComplementares { get; set; }

        // Propriedade de navegação (opcional, sem carregamento obrigatório)
        public Usuarios? Usuario { get; set; }
    }
}

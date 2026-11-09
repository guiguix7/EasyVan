using System.ComponentModel.DataAnnotations;

namespace EasyVan.Models
{
    public class Aluno : IOrganizacaoScoped
    {
        public int Id { get; set; }
        public int OrganizacaoId { get; set; } = 1;

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(120, ErrorMessage = "O nome deve ter no máximo 120 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(160)]
        public string? Escola { get; set; }

        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Informe um email válido.")]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        [TelefoneBrasil]
        public string? Telefone { get; set; }

        [StringLength(200, ErrorMessage = "O endereço deve ter no máximo 200 caracteres.")]
        public string? Endereco { get; set; }

        public bool Ativo { get; set; } = true;

        public int? UsuarioId { get; set; }
        public Usuarios? Usuario { get; set; }
    }
}

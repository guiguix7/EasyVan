using System.ComponentModel.DataAnnotations;
using EasyVan.Services;

namespace EasyVan.Models
{
    public class UsuarioEditViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(120)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecione o perfil.")]
        public string RoleManager { get; set; } = "Aluno";

        [TelefoneBrasil]
        [StringLength(20)]
        public string? Telefone { get; set; }

        [StringLength(200)]
        public string? Endereco { get; set; }

        [StringLength(20)]
        [Display(Name = "CNH")]
        public string? Cnh { get; set; }

        public bool Ativo { get; set; } = true;

        [StringLength(20, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 20 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nova senha")]
        public string? NovaSenha { get; set; }

        [StringLength(20, MinimumLength = 6, ErrorMessage = "A confirmação deve ter entre 6 e 20 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar nova senha")]
        public string? ConfirmarSenha { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(NovaSenha) && string.IsNullOrWhiteSpace(ConfirmarSenha))
            {
                yield break;
            }

            if (string.IsNullOrWhiteSpace(NovaSenha) || string.IsNullOrWhiteSpace(ConfirmarSenha))
            {
                yield return new ValidationResult("Informe e confirme a nova senha juntas.", new[] { nameof(NovaSenha), nameof(ConfirmarSenha) });
                yield break;
            }

            if (!string.Equals(NovaSenha, ConfirmarSenha, StringComparison.Ordinal))
            {
                yield return new ValidationResult("A confirmação não confere com a nova senha.", new[] { nameof(ConfirmarSenha) });
            }
        }
    }
}

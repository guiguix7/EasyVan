using System.ComponentModel.DataAnnotations;
using EasyVan.Services;

namespace EasyVan.Models
{
    public class RegistroAlunoViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Informe seu nome completo.")]
        [StringLength(120)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [TelefoneBrasil]
        [StringLength(20)]
        public string? Telefone { get; set; }

        [StringLength(200)]
        public string? Endereco { get; set; }

        [Required(ErrorMessage = "Crie uma senha.")]
        [StringLength(20, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 20 caracteres.")]
        [DataType(DataType.Password)]
        public string Senha { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme a senha.")]
        [DataType(DataType.Password)]
        public string ConfirmarSenha { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.Equals(Senha, ConfirmarSenha, StringComparison.Ordinal))
            {
                yield return new ValidationResult("A confirmação não confere com a senha.", new[] { nameof(ConfirmarSenha) });
            }

            if (!ValidacaoBrasil.NomeValido(Nome))
            {
                yield return new ValidationResult("Informe um nome válido, com pelo menos 3 letras.", new[] { nameof(Nome) });
            }
        }
    }
}

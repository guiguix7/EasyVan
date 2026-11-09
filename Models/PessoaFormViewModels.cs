using System.ComponentModel.DataAnnotations;
using EasyVan.Services;

namespace EasyVan.Models
{
    public class AlunoFormViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(120)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Informe um email válido.")]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [TelefoneBrasil]
        [StringLength(20)]
        public string? Telefone { get; set; }

        [StringLength(200)]
        public string? Endereco { get; set; }

        [StringLength(160, ErrorMessage = "O nome da escola deve ter no máximo 160 caracteres.")]
        public string? Escola { get; set; }

        public bool Ativo { get; set; } = true;

        public bool ContaVinculada { get; set; }
        public string? EmailConta { get; set; }

        [StringLength(20, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 20 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha de acesso")]
        public string? Senha { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar senha")]
        public string? ConfirmarSenha { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!ValidacaoBrasil.NomeValido(Nome))
                yield return new ValidationResult("Informe um nome válido, com pelo menos 3 letras.", new[] { nameof(Nome) });

            if (!string.IsNullOrWhiteSpace(Senha) || !string.IsNullOrWhiteSpace(ConfirmarSenha))
            {
                if (string.IsNullOrWhiteSpace(Senha) || string.IsNullOrWhiteSpace(ConfirmarSenha))
                {
                    yield return new ValidationResult("Informe e confirme a senha juntas.", new[] { nameof(Senha), nameof(ConfirmarSenha) });
                }
                else if (!string.Equals(Senha, ConfirmarSenha, StringComparison.Ordinal))
                {
                    yield return new ValidationResult("A confirmação não confere com a senha.", new[] { nameof(ConfirmarSenha) });
                }
            }
        }
    }

    public class MotoristaFormViewModel : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(120)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Informe um email válido.")]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [TelefoneBrasil]
        [StringLength(20)]
        public string? Telefone { get; set; }

        [StringLength(20)]
        public string? CNH { get; set; }

        public bool Ativo { get; set; } = true;

        public bool ContaVinculada { get; set; }
        public string? EmailConta { get; set; }

        [StringLength(20, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 20 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha de acesso")]
        public string? Senha { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar senha")]
        public string? ConfirmarSenha { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!ValidacaoBrasil.NomeValido(Nome))
                yield return new ValidationResult("Informe um nome válido, com pelo menos 3 letras.", new[] { nameof(Nome) });

            if (!string.IsNullOrWhiteSpace(Senha) || !string.IsNullOrWhiteSpace(ConfirmarSenha))
            {
                if (string.IsNullOrWhiteSpace(Senha) || string.IsNullOrWhiteSpace(ConfirmarSenha))
                {
                    yield return new ValidationResult("Informe e confirme a senha juntas.", new[] { nameof(Senha), nameof(ConfirmarSenha) });
                }
                else if (!string.Equals(Senha, ConfirmarSenha, StringComparison.Ordinal))
                {
                    yield return new ValidationResult("A confirmação não confere com a senha.", new[] { nameof(ConfirmarSenha) });
                }
            }
        }
    }
}

using System.ComponentModel.DataAnnotations;
using EasyVan.Services;

namespace EasyVan.Models
{
    public class UsuarioCreateViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(120)]
        [Display(Name = "Nome completo")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecione um perfil de acesso.")]
        public string RoleManager { get; set; } = "Aluno";

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [StringLength(20, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 20 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string PasswordHasher { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme a senha.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar senha")]
        public string ConfirmarSenha { get; set; } = string.Empty;

        [TelefoneBrasil]
        [StringLength(20)]
        public string? Telefone { get; set; }

        [StringLength(200)]
        public string? Endereco { get; set; }

        [StringLength(20)]
        [Display(Name = "CNH")]
        public string? Cnh { get; set; }

        public bool Ativo { get; set; } = true;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!ValidacaoBrasil.NomeValido(Nome))
                yield return new ValidationResult("Informe um nome válido, com pelo menos 3 letras.", new[] { nameof(Nome) });

            if (!string.Equals(PasswordHasher, ConfirmarSenha, StringComparison.Ordinal))
                yield return new ValidationResult("A confirmação não confere com a senha.", new[] { nameof(ConfirmarSenha) });
        }
    }
}

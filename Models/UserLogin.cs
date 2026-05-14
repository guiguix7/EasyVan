using System.ComponentModel.DataAnnotations;

namespace EasyVan.Models
{
    public class UserLogin
    {
        [Required(ErrorMessage = "O nome de usuário é obrigatório.")]
        public string Username { get; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Informe um email válido.")]
        public string Email { get; } = string.Empty;

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter ao menos 6 caracteres.")]
        [DataType(DataType.Password)]
        public string Password { get; } = string.Empty;
    }
}
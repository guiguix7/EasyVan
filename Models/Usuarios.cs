// Pega os dados do usuário e valida as informações, como nome, email e senha.
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EasyVan.Models
{
    public class Usuarios
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Informe um email válido.")]
        public string Email { get; set; } = string.Empty;

        public string RoleManager { get; set; } = "Aluno";

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter ao menos 6 caracteres.")]
        [DataType(DataType.Password)]
        public string PasswordHasher { get; set; } = string.Empty;

        public static string NormalizeRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return "Aluno";
            }

            var normalized = role.Trim();

            if (normalized.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Administrador", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Adminstrador", StringComparison.OrdinalIgnoreCase))
            {
                return "Administrador";
            }

            if (normalized.Equals("Driver", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Motorista", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Motorisa", StringComparison.OrdinalIgnoreCase))
            {
                return "Motorista";
            }

            if (normalized.Equals("User", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Aluno", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Usuario", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Usuario Comum", StringComparison.OrdinalIgnoreCase))
            {
                return "Aluno";
            }

            return "Aluno";
        }
    }
}

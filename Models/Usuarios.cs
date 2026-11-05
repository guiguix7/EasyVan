using System.ComponentModel.DataAnnotations;

namespace EasyVan.Models
{
    public class Usuarios
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(120)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;

        public string RoleManager { get; set; } = "Aluno";

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [StringLength(20, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 20 caracteres.")]
        [DataType(DataType.Password)]
        public string PasswordHasher { get; set; } = string.Empty;

        public static bool IsSupportedRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role)) return false;

            return role.Trim().Equals("Administrador", StringComparison.OrdinalIgnoreCase)
                || role.Trim().Equals("Motorista", StringComparison.OrdinalIgnoreCase)
                || role.Trim().Equals("Aluno", StringComparison.OrdinalIgnoreCase);
        }

        public static string NormalizeRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role)) return "Aluno";
            var normalized = role.Trim();

            if (normalized.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Administrador", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Adminstrador", StringComparison.OrdinalIgnoreCase))
                return "Administrador";

            if (normalized.Equals("Driver", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Motorista", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Motorisa", StringComparison.OrdinalIgnoreCase))
                return "Motorista";

            if (normalized.Equals("User", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Aluno", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Usuario", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Usuario Comum", StringComparison.OrdinalIgnoreCase))
                return "Aluno";

            return "Aluno";
        }

        public static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();
    }
}
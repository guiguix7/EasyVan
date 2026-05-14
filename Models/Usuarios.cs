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
        public string Email { get; set; } = "Aluno";

        public string RoleManager { get; set; } = "Aluno";

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter ao menos 6 caracteres.")]
        [DataType(DataType.Password)]
        
        [Description("Usuario Cadastrado com Sucesso!")]
        public string PasswordHasher { get; set; } = string.Empty;
    }
}

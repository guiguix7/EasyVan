using System.ComponentModel.DataAnnotations;

namespace EasyVan.Models
{
    public class Motorista
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Informe um email válido.")]
        public string Email { get; set; } = string.Empty;

        public string? Telefone { get; set; }

        public string? CNH { get; set; }

        public bool Ativo { get; set; } = true;
    }
}

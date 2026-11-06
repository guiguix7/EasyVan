using System.ComponentModel.DataAnnotations;

namespace EasyVan.Models
{
    public class ConfiguracoesViewModel
    {
        [Required]
        [RegularExpression("claro|escuro", ErrorMessage = "Escolha o tema claro ou escuro.")]
        public string Tema { get; set; } = "claro";

        public string Nome { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }
}

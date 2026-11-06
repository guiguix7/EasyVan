using System.ComponentModel.DataAnnotations;

namespace EasyVan.Models
{
    public class PerfilViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(120)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;

        public string? FotoAtual { get; set; }

        [Display(Name = "Nova foto")]
        public IFormFile? Foto { get; set; }

        [Display(Name = "Remover foto atual")]
        public bool RemoverFoto { get; set; }
    }
}

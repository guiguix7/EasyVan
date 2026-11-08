using System.ComponentModel.DataAnnotations;

namespace EasyVan.Models
{
    public class Organizacao
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome da organização é obrigatório.")]
        [StringLength(120)]
        public string Nome { get; set; } = string.Empty;

        public bool Ativo { get; set; } = true;
    }
}

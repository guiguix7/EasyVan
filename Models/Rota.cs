using System.ComponentModel.DataAnnotations;

namespace EasyVan.Models
{
    public class Rota
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome da rota é obrigatório.")]
        public string Nome { get; set; } = string.Empty;

        public string? Descricao { get; set; }

        [Required(ErrorMessage = "A origem é obrigatória.")]
        public string Origem { get; set; } = string.Empty;

        [Required(ErrorMessage = "O destino é obrigatório.")]
        public string Destino { get; set; } = string.Empty;

        public bool Ativo { get; set; } = true;
    }
}

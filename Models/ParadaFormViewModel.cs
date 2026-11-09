using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EasyVan.Models;

public sealed class ParadaFormViewModel
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecione uma rota.")]
    [Display(Name = "Rota")]
    public int RotaId { get; set; }

    [EnumDataType(typeof(SentidoViagem), ErrorMessage = "Selecione um sentido válido.")]
    [Display(Name = "Sentido")]
    public SentidoViagem Sentido { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A ordem da parada deve ser maior que zero.")]
    public int Ordem { get; set; } = 1;

    [StringLength(120)]
    public string? Nome { get; set; }

    [Required(ErrorMessage = "O endereço é obrigatório.")]
    [StringLength(200)]
    public string Endereco { get; set; } = string.Empty;

    [Display(Name = "Horário previsto (opcional)")]
    public TimeSpan? HorarioPrevisto { get; set; }

    public bool Ativo { get; set; } = true;

    public IEnumerable<SelectListItem> Rotas { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Sentidos { get; set; } = Array.Empty<SelectListItem>();
}

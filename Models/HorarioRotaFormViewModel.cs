using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EasyVan.Models;

public sealed class HorarioRotaFormViewModel
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecione uma rota.")]
    [Display(Name = "Rota")]
    public int RotaId { get; set; }

    [EnumDataType(typeof(SentidoViagem), ErrorMessage = "Selecione um sentido válido.")]
    [Display(Name = "Sentido")]
    public SentidoViagem Sentido { get; set; }

    [Display(Name = "Dias da semana")]
    [MinLength(1, ErrorMessage = "Selecione ao menos um dia da semana.")]
    public List<int> DiasSelecionados { get; set; } = new();

    [Required(ErrorMessage = "Informe o horário de saída.")]
    [Display(Name = "Horário de saída")]
    public TimeSpan? HorarioSaida { get; set; }

    [Display(Name = "Van padrão (opcional)")]
    public int? VanPadraoId { get; set; }

    public bool Ativo { get; set; } = true;

    public IEnumerable<SelectListItem> Rotas { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Vans { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Sentidos { get; set; } = Array.Empty<SelectListItem>();
}

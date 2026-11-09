using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EasyVan.Models;

public sealed class VinculoAlunoFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecione um aluno.")]
    [Display(Name = "Aluno")]
    public int AlunoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecione uma rota.")]
    [Display(Name = "Rota")]
    public int RotaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecione uma parada.")]
    [Display(Name = "Parada")]
    public int ParadaId { get; set; }

    [EnumDataType(typeof(SentidoViagem), ErrorMessage = "Selecione um sentido válido.")]
    [Display(Name = "Sentido")]
    public SentidoViagem Sentido { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Início do vínculo")]
    public DateTime? DataInicio { get; set; }

    [StringLength(300)]
    public string? Observacao { get; set; }

    public IEnumerable<SelectListItem> Alunos { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Rotas { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Paradas { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Sentidos { get; set; } = Array.Empty<SelectListItem>();
}

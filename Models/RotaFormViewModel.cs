using System.ComponentModel.DataAnnotations;

namespace EasyVan.Models;

public sealed class RotaFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome da rota é obrigatório.")]
    [StringLength(120)]
    public string Nome { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Descricao { get; set; }

    [Required(ErrorMessage = "A origem é obrigatória.")]
    [StringLength(200)]
    public string Origem { get; set; } = string.Empty;

    [Required(ErrorMessage = "O destino é obrigatório.")]
    [StringLength(200)]
    public string Destino { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;
}

public sealed class RotaDetalhesViewModel
{
    public required Rota Rota { get; init; }
    public IReadOnlyList<HorarioRota> Horarios { get; init; } = Array.Empty<HorarioRota>();
    public IReadOnlyList<Parada> Paradas { get; init; } = Array.Empty<Parada>();
}

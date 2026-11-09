using System.ComponentModel.DataAnnotations;

namespace EasyVan.Models;

public sealed class LinhaFormViewModel : IValidatableObject
{
    public int Id { get; set; }
    [Required, StringLength(120)] public string Nome { get; set; } = string.Empty;
    [Required, StringLength(300)] public string Destino { get; set; } = string.Empty;
    [Range(-90, 90)] public double DestinoLat { get; set; }
    [Range(-180, 180)] public double DestinoLng { get; set; }
    [Required, StringLength(300)] public string Origem { get; set; } = string.Empty;
    [Range(-90, 90)] public double OrigemLat { get; set; }
    [Range(-180, 180)] public double OrigemLng { get; set; }
    public DiasSemana DiasSemana { get; set; } = DiasSemana.DiasUteis;
    public List<int> DiasSelecionados { get; set; } = new();
    public TimeSpan HorarioChegadaIda { get; set; } = new(7, 0, 0);
    public TimeSpan HorarioSaidaVolta { get; set; } = new(17, 0, 0);
    public bool TemIda { get; set; } = true;
    public bool TemVolta { get; set; } = true;
    public bool VoltaTerminaNoUltimoAluno { get; set; }
    [Range(0, 60)] public int FolgaMinutos { get; set; } = 5;
    public List<int> PassageirosIds { get; set; } = new();
    public List<int> VansIds { get; set; } = new();
    public Dictionary<int, int?> MotoristasPorVan { get; set; } = new();
    public Dictionary<int, int?> VanPorPassageiro { get; set; } = new();
    public Dictionary<int, PontoEmbarqueFormViewModel> PontosEmbarque { get; set; } = new();
    public bool Ativa { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DiasSemana == DiasSemana.Nenhum)
            yield return new ValidationResult("Selecione ao menos um dia da semana.", new[] { nameof(DiasSemana) });
        if (!TemIda && !TemVolta)
            yield return new ValidationResult("Selecione ida, volta ou ambos os sentidos.", new[] { nameof(TemIda), nameof(TemVolta) });
        if (TemIda && (HorarioChegadaIda < TimeSpan.Zero || HorarioChegadaIda >= TimeSpan.FromDays(1)))
            yield return new ValidationResult("Informe o horário de chegada da ida.", new[] { nameof(HorarioChegadaIda) });
        if (TemVolta && (HorarioSaidaVolta < TimeSpan.Zero || HorarioSaidaVolta >= TimeSpan.FromDays(1)))
            yield return new ValidationResult("Informe o horário de saída da volta.", new[] { nameof(HorarioSaidaVolta) });
    }
}

public sealed record LinhaIndexItemViewModel(Linha Linha, int Passageiros, int Capacidade);
public sealed record OpcaoVanViewModel(Van Van, int? MotoristaId);
public sealed class PontoEmbarqueFormViewModel
{
    [StringLength(300)] public string? Endereco { get; set; }
    [Range(-90, 90)] public double? Latitude { get; set; }
    [Range(-180, 180)] public double? Longitude { get; set; }
}

public sealed record AlunoLinhaOpcaoViewModel(int Id, string Nome, string? Escola);

public sealed class ParadaMotoristaViewModel
{
    public int Ordem { get; init; }
    public string Endereco { get; init; } = string.Empty;
    public DateTimeOffset EtaPrevisto { get; init; }
    public IReadOnlyList<string> Passageiros { get; init; } = Array.Empty<string>();
}

public sealed class LinhaDetalheViewModel
{
    public required Viagem Viagem { get; init; }
    public IReadOnlyList<ViagemParada> Paradas { get; init; } = Array.Empty<ViagemParada>();
    public string? AvisoRoteirizacao { get; init; }
    public IReadOnlyList<TrechoGoogleMapsViewModel> Trechos { get; init; } = Array.Empty<TrechoGoogleMapsViewModel>();
}

public sealed record TrechoGoogleMapsViewModel(int Numero, int Total, string Url, string Botao);

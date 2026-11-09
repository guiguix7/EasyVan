using System.ComponentModel.DataAnnotations;

namespace EasyVan.Models;

public sealed class Linha : IOrganizacaoScoped
{
    public int Id { get; set; }
    public int OrganizacaoId { get; set; } = 1;
    public int RotaLegadaId { get; set; }

    [Required, StringLength(120)] public string Nome { get; set; } = string.Empty;
    [Required, StringLength(300)] public string Destino { get; set; } = string.Empty;
    public double DestinoLat { get; set; }
    public double DestinoLng { get; set; }
    [Required, StringLength(300)] public string Origem { get; set; } = string.Empty;
    public double OrigemLat { get; set; }
    public double OrigemLng { get; set; }
    public DiasSemana DiasSemana { get; set; }
    public TimeSpan HorarioChegadaIda { get; set; }
    public TimeSpan HorarioSaidaVolta { get; set; }
    public bool TemIda { get; set; } = true;
    public bool TemVolta { get; set; } = true;
    public bool VoltaTerminaNoUltimoAluno { get; set; }
    public int FolgaMinutos { get; set; } = 5;
    public bool Ativa { get; set; } = true;
    public ICollection<LinhaVan> Vans { get; set; } = new List<LinhaVan>();
    public ICollection<LinhaPassageiro> Passageiros { get; set; } = new List<LinhaPassageiro>();
}

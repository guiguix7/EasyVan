namespace EasyVan.Models;

public sealed class ViagemParada : IOrganizacaoScoped
{
    public int Id { get; set; }
    public int OrganizacaoId { get; set; } = 1;
    public int ViagemId { get; set; }
    public int UsuarioId { get; set; }
    public int Ordem { get; set; }
    public DateTimeOffset EtaPrevisto { get; set; }
    public string? EnderecoEmbarque { get; set; }
    public double? LatitudeEmbarque { get; set; }
    public double? LongitudeEmbarque { get; set; }
    public Viagem? Viagem { get; set; }
    public Usuarios? Usuario { get; set; }
}

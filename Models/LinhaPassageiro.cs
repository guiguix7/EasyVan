namespace EasyVan.Models;

public sealed class LinhaPassageiro : IOrganizacaoScoped
{
    public int Id { get; set; }
    public int OrganizacaoId { get; set; } = 1;
    public int LinhaId { get; set; }
    public int UsuarioId { get; set; }
    public int VanId { get; set; }
    public string? EnderecoEmbarque { get; set; }
    public double? LatitudeEmbarque { get; set; }
    public double? LongitudeEmbarque { get; set; }
    public Linha? Linha { get; set; }
    public Usuarios? Usuario { get; set; }
    public Van? Van { get; set; }
}

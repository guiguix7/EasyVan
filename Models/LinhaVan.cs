namespace EasyVan.Models;

public sealed class LinhaVan : IOrganizacaoScoped
{
    public int Id { get; set; }
    public int OrganizacaoId { get; set; } = 1;
    public int LinhaId { get; set; }
    public int VanId { get; set; }
    public int MotoristaId { get; set; }
    public Linha? Linha { get; set; }
    public Van? Van { get; set; }
    public Motorista? Motorista { get; set; }
}

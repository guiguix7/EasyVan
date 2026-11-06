namespace EasyVan.Models;

public class MapaRotaViewModel
{
    public string Origem { get; set; } = string.Empty;

    public string Destino { get; set; } = string.Empty;

    public string? Titulo { get; set; }

    public bool TemTrajeto =>
        !string.IsNullOrWhiteSpace(Origem) && !string.IsNullOrWhiteSpace(Destino);
}

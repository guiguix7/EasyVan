namespace EasyVan.Models;

public class MapaRotaViewModel
{
    public string Origem { get; set; } = string.Empty;

    public string Destino { get; set; } = string.Empty;

    public string? Titulo { get; set; }

    public IReadOnlyList<string> Paradas { get; set; } = Array.Empty<string>();

    public bool TemTrajeto =>
        !string.IsNullOrWhiteSpace(Origem) && !string.IsNullOrWhiteSpace(Destino);
}

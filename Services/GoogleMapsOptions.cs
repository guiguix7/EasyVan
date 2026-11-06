namespace EasyVan.Services;

public class GoogleMapsOptions
{
    public string ApiKey { get; init; } = string.Empty;

    public bool Configurado => !string.IsNullOrWhiteSpace(ApiKey);
}

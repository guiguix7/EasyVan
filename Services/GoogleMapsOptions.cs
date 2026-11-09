namespace EasyVan.Services;

public class GoogleMapsOptions
{
    public string ApiKey { get; init; } = string.Empty;
    public string BrowserApiKey { get; init; } = string.Empty;

    public bool Configurado => !string.IsNullOrWhiteSpace(ApiKey);
    public bool BrowserConfigurado => !string.IsNullOrWhiteSpace(BrowserApiKey);
}

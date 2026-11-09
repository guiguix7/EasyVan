using System.Globalization;
using System.Text.Json;

namespace EasyVan.Services;

public sealed class GoogleGeocodificador : IGeocodificador
{
    private readonly HttpClient _http;
    private readonly GoogleMapsOptions _options;

    public GoogleGeocodificador(HttpClient http, GoogleMapsOptions options)
    {
        _http = http;
        _options = options;
    }

    public async Task<(string Endereco, Coordenada Coordenada)> GeocodificarAsync(string endereco, CancellationToken cancellationToken = default)
    {
        if (!_options.Configurado) throw new InvalidOperationException("Configure GoogleMaps__ApiKey ou informe as coordenadas sugeridas pelo autocomplete.");
        var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(endereco)}&region=br";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("X-Goog-Api-Key", _options.ApiKey);
        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var root = json.RootElement;
        if (!string.Equals(root.GetProperty("status").GetString(), "OK", StringComparison.Ordinal))
            throw new InvalidOperationException("Não foi possível localizar esse endereço. Confira o endereço exato.");
        var resultado = root.GetProperty("results")[0];
        var local = resultado.GetProperty("geometry").GetProperty("location");
        return (resultado.GetProperty("formatted_address").GetString() ?? endereco,
            new Coordenada(local.GetProperty("lat").GetDouble(), local.GetProperty("lng").GetDouble()));
    }
}

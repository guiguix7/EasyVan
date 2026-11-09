using System.Net.Http.Json;
using System.Text.Json;
using System.Globalization;

namespace EasyVan.Services;

public sealed class GoogleRoteirizador : IRoteirizador
{
    private readonly HttpClient _http;
    private readonly GoogleMapsOptions _options;

    public GoogleRoteirizador(HttpClient http, GoogleMapsOptions options)
    {
        _http = http;
        _options = options;
    }

    public async Task<ResultadoRoteirizacao> CalcularAsync(Coordenada origem, Coordenada destino,
        IReadOnlyList<ParadaRoteavel> paradas, DateTimeOffset partidaEstimada, bool otimizarOrdem,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Configurado) throw new InvalidOperationException("Google Maps não configurado.");
        if (paradas.Count > 25) throw new ArgumentOutOfRangeException(nameof(paradas), "Routes API aceita até 25 paradas intermediárias.");

        var requisicao = new
        {
            origin = Waypoint(origem),
            destination = Waypoint(destino),
            intermediates = paradas.Select(p => Waypoint(p.Local)).ToArray(),
            travelMode = "DRIVE",
            departureTime = partidaEstimada.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            optimizeWaypointOrder = otimizarOrdem && paradas.Count > 1
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://routes.googleapis.com/directions/v2:computeRoutes")
        {
            Content = JsonContent.Create(requisicao)
        };
        request.Headers.TryAddWithoutValidation("X-Goog-Api-Key", _options.ApiKey);
        request.Headers.TryAddWithoutValidation("X-Goog-FieldMask",
            "routes.distanceMeters,routes.duration,routes.legs.distanceMeters,routes.legs.duration,routes.optimizedIntermediateWaypointIndex");

        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var route = json.RootElement.GetProperty("routes")[0];
        var optimizedIndices = route.TryGetProperty("optimizedIntermediateWaypointIndex", out var orderElement)
            ? orderElement.EnumerateArray().Select(x => x.GetInt32()).ToArray()
            : Enumerable.Range(0, paradas.Count).ToArray();
        var orderedStops = optimizedIndices.Select(i => paradas[i]).ToArray();
        var legs = route.GetProperty("legs").EnumerateArray().ToArray();
        var eta = partidaEstimada;
        var resultadoParadas = new List<ParadaCalculada>();
        for (var i = 0; i < orderedStops.Length; i++)
        {
            var leg = legs[i];
            eta = eta.AddSeconds(ParseDuration(leg.GetProperty("duration").GetString()));
            resultadoParadas.Add(new ParadaCalculada(orderedStops[i].UsuarioId, i + 1, eta,
                leg.GetProperty("distanceMeters").GetInt32()));
        }

        return new ResultadoRoteirizacao(resultadoParadas,
            route.GetProperty("distanceMeters").GetInt32(),
            ParseDuration(route.GetProperty("duration").GetString()), partidaEstimada);
    }

    private static object Waypoint(Coordenada ponto) => new
    {
        location = new { latLng = new { latitude = ponto.Lat, longitude = ponto.Lng } }
    };

    private static long ParseDuration(string? valor) =>
        long.TryParse(valor?.TrimEnd('s'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var segundos)
            ? segundos : throw new InvalidOperationException("Resposta de duração inválida do Routes API.");
}

using System.Globalization;
using EasyVan.Models;

namespace EasyVan.Services;

public static class GoogleMapsLinkService
{
    public static IReadOnlyList<TrechoGoogleMapsViewModel> CriarTrechos(
        Linha linha, SentidoViagem sentido, IReadOnlyList<ViagemParada> paradas, int maxWaypoints = 3)
    {
        if (maxWaypoints is < 1 or > 9) throw new ArgumentOutOfRangeException(nameof(maxWaypoints));
        var pontos = paradas.OrderBy(p => p.Ordem)
            .Where(p => p.Usuario?.Lat.HasValue == true && p.Usuario.Lng.HasValue)
            .Select(p => Coordenada(p.Usuario!.Lat!.Value, p.Usuario.Lng!.Value)).ToList();
        var volta = sentido == SentidoViagem.Volta;
        var destino = volta && linha.VoltaTerminaNoUltimoAluno && pontos.Count > 0
            ? pontos[^1]
            : Coordenada(volta ? linha.OrigemLat : linha.DestinoLat, volta ? linha.OrigemLng : linha.DestinoLng);
        if (volta && linha.VoltaTerminaNoUltimoAluno && pontos.Count > 0) pontos.RemoveAt(pontos.Count - 1);
        pontos.Add(destino);
        var trechos = new List<TrechoGoogleMapsViewModel>();
        var inicio = 0;
        string? origemAnterior = null;
        while (inicio < pontos.Count)
        {
            var fim = Math.Min(inicio + maxWaypoints, pontos.Count - 1);
            var intermediarios = pontos.Skip(inicio).Take(fim - inicio).ToArray();
            var destinoTrecho = pontos[fim];
            var link = "https://www.google.com/maps/dir/?api=1&destination=" + Uri.EscapeDataString(destinoTrecho)
                + (intermediarios.Length == 0 ? string.Empty : "&waypoints=" + string.Join("%7C", intermediarios.Select(Uri.EscapeDataString)))
                + "&travelmode=driving";
            if (!string.IsNullOrEmpty(origemAnterior)) link += "&origin=" + Uri.EscapeDataString(origemAnterior);
            trechos.Add(new TrechoGoogleMapsViewModel(trechos.Count + 1, 0, link,
                trechos.Count == 0 ? "Abrir no Google Maps" : "Próximo trecho"));
            origemAnterior = destinoTrecho;
            inicio = fim + 1;
        }
        return trechos.Select(t => t with { Total = trechos.Count }).ToList();
    }

    private static string Coordenada(double lat, double lng) =>
        lat.ToString("0.######", CultureInfo.InvariantCulture) + "," + lng.ToString("0.######", CultureInfo.InvariantCulture);
}

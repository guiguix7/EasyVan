namespace EasyVan.Services;

public sealed class FakeRoteirizador : IRoteirizador
{
    public Task<ResultadoRoteirizacao> CalcularAsync(Coordenada origem, Coordenada destino,
        IReadOnlyList<ParadaRoteavel> paradas, DateTimeOffset partidaEstimada, bool otimizarOrdem,
        CancellationToken cancellationToken = default)
    {
        var ordenadas = new List<ParadaRoteavel>();
        if (otimizarOrdem)
        {
            var restantes = paradas.ToList();
            var pontoAtual = origem;
            while (restantes.Count > 0)
            {
                var proxima = restantes.OrderBy(p => Distancia(pontoAtual, p.Local)).First();
                ordenadas.Add(proxima);
                restantes.Remove(proxima);
                pontoAtual = proxima.Local;
            }
        }
        else
        {
            ordenadas.AddRange(paradas);
        }

        var eta = partidaEstimada;
        var anteriores = origem;
        var calculadas = new List<ParadaCalculada>();
        var distanciaTotal = 0;
        foreach (var (parada, indice) in ordenadas.Select((p, i) => (p, i)))
        {
            var distancia = Distancia(anteriores, parada.Local);
            distanciaTotal += distancia;
            eta = eta.AddSeconds(distancia / (30d * 1000 / 3600));
            calculadas.Add(new ParadaCalculada(parada.UsuarioId, indice + 1, eta, distancia));
            anteriores = parada.Local;
        }
        distanciaTotal += Distancia(anteriores, destino);
        var duracao = (long)Math.Ceiling(distanciaTotal / (30d * 1000 / 3600));
        return Task.FromResult(new ResultadoRoteirizacao(calculadas, distanciaTotal, duracao, partidaEstimada));
    }

    private static int Distancia(Coordenada a, Coordenada b)
    {
        const double raioTerra = 6_371_000;
        var lat1 = GrausParaRadianos(a.Lat);
        var lat2 = GrausParaRadianos(b.Lat);
        var deltaLat = lat2 - lat1;
        var deltaLng = GrausParaRadianos(b.Lng - a.Lng);
        var h = Math.Pow(Math.Sin(deltaLat / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(deltaLng / 2), 2);
        return (int)Math.Round(raioTerra * 2 * Math.Asin(Math.Sqrt(h)));
    }

    private static double GrausParaRadianos(double valor) => valor * Math.PI / 180;
}

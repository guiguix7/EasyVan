namespace EasyVan.Services;

public sealed record Coordenada(double Lat, double Lng);
public sealed record ParadaRoteavel(int UsuarioId, string Nome, Coordenada Local);
public sealed record ParadaCalculada(int UsuarioId, int Ordem, DateTimeOffset EtaPrevisto, int DistanciaTrechoMetros);
public sealed record ResultadoRoteirizacao(IReadOnlyList<ParadaCalculada> Paradas, int DistanciaMetros, long DuracaoSegundos, DateTimeOffset SaidaSugerida);

public interface IRoteirizador
{
    Task<ResultadoRoteirizacao> CalcularAsync(
        Coordenada origem,
        Coordenada destino,
        IReadOnlyList<ParadaRoteavel> paradas,
        DateTimeOffset partidaEstimada,
        bool otimizarOrdem,
        CancellationToken cancellationToken = default);
}

public interface IGeocodificador
{
    Task<(string Endereco, Coordenada Coordenada)> GeocodificarAsync(string endereco, CancellationToken cancellationToken = default);
}

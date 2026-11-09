using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EasyVan.Data;
using EasyVan.Models;
using Microsoft.EntityFrameworkCore;

namespace EasyVan.Services;

public sealed record ResultadoRecalculoRota(bool Encontrada, bool Reutilizada, string? Aviso);

public sealed class RoteirizacaoService
{
    private readonly ApplicationDbContext _db;
    private readonly IRoteirizador _roteirizador;
    private readonly TimeProvider _time;
    private readonly PrazoPresencaOptions _prazo;
    private readonly ILogger<RoteirizacaoService> _logger;

    public RoteirizacaoService(ApplicationDbContext db, IRoteirizador roteirizador, TimeProvider time,
        PrazoPresencaOptions prazo, ILogger<RoteirizacaoService> logger)
    {
        _db = db; _roteirizador = roteirizador; _time = time; _prazo = prazo; _logger = logger;
    }

    public async Task<ResultadoRecalculoRota> RecalcularSeNecessarioAsync(int viagemId, CancellationToken ct = default)
    {
        var viagem = await _db.Viagens.Include(v => v.Linha).Include(v => v.Presencas!).ThenInclude(p => p.Aluno)
            .FirstOrDefaultAsync(v => v.Id == viagemId, ct);
        if (viagem?.Linha is null) return new ResultadoRecalculoRota(viagem is not null, false, null);

        var linha = viagem.Linha;
        var candidatos = await _db.LinhasPassageiros.Where(p => p.LinhaId == linha.Id && p.VanId == viagem.VanId)
            .Include(p => p.Usuario).ToListAsync(ct);
        var confirmados = viagem.Presencas!
            .Where(p => p.Status == StatusPresenca.Confirmado)
            .Select(p => p.UsuarioId ?? p.Aluno?.UsuarioId)
            .Where(id => id.HasValue).Select(id => id!.Value).ToHashSet();
        var paradas = candidatos.Where(p => confirmados.Contains(p.UsuarioId)
                && p.LatitudeEmbarque.HasValue && p.LongitudeEmbarque.HasValue)
            .Select(p => new ParadaRoteavel(p.UsuarioId, p.Usuario?.Nome ?? "Passageiro",
                new Coordenada(p.LatitudeEmbarque!.Value, p.LongitudeEmbarque!.Value)))
            .OrderBy(p => p.UsuarioId).ToArray();
        var hash = CriarHashConfirmados(confirmados, viagem.Data, viagem.Sentido ?? SentidoViagem.Ida);

        if (viagem.HashConfirmados == hash && viagem.StatusRoteirizacao is "Calculada" or "SemConfirmados")
            return new ResultadoRecalculoRota(true, true, null);
        if (viagem.RoteirizacaoFalhouEm.HasValue && viagem.RoteirizacaoFalhouEm.Value > _time.GetUtcNow().AddMinutes(-5)
            && viagem.HashConfirmados == hash && viagem.StatusRoteirizacao == "Fallback")
            return new ResultadoRecalculoRota(true, true, "Usando a ordem aproximada; a consulta ao serviço de rotas falhou recentemente.");
        if (viagem.RoteirizacaoFalhouEm.HasValue && viagem.RoteirizacaoFalhouEm.Value > _time.GetUtcNow().AddMinutes(-5)
            && viagem.HashConfirmados != hash)
            return new ResultadoRecalculoRota(true, true, "O serviço de rotas falhou recentemente; mantendo a última rota válida.");

        var paradasAntigas = await _db.ViagensParadas.Where(p => p.ViagemId == viagemId).ToListAsync(ct);
        var pontosExtremosLocalizados = TemCoordenada(linha.OrigemLat, linha.OrigemLng)
            && TemCoordenada(linha.DestinoLat, linha.DestinoLng);
        if (paradas.Length == 0 || !pontosExtremosLocalizados)
        {
            _db.ViagensParadas.RemoveRange(paradasAntigas);
            viagem.HashConfirmados = hash;
            viagem.CalculadaEm = _time.GetUtcNow();
            viagem.RoteirizacaoFalhouEm = null;
            viagem.StatusRoteirizacao = confirmados.Count == 0 ? "SemConfirmados" : "SemPontosOperacionais";
            viagem.DistanciaMetros = null;
            viagem.DuracaoSegundos = null;
            viagem.HorarioSaidaSugerido = null;
            await _db.SaveChangesAsync(ct);
            return new ResultadoRecalculoRota(true, false, confirmados.Count == 0
                ? "Nenhum passageiro confirmou; não há rota a calcular."
                : "Há endereços sem localização suficiente para calcular a rota. Peça à administração para configurar a localização no Google Maps.");
        }

        try
        {
            var partidaLocal = DateOnly.FromDateTime(viagem.Data);
            Coordenada origem;
            Coordenada destino;
            ParadaRoteavel? destinoAluno = null;
            DateTimeOffset partida;
            var sentidoIda = (viagem.Sentido ?? SentidoViagem.Ida) == SentidoViagem.Ida;
            if (sentidoIda)
            {
                origem = new Coordenada(linha.OrigemLat, linha.OrigemLng);
                destino = new Coordenada(linha.DestinoLat, linha.DestinoLng);
                partida = Localizar(partidaLocal, linha.HorarioChegadaIda).AddHours(-1);
            }
            else
            {
                origem = new Coordenada(linha.DestinoLat, linha.DestinoLng);
                destino = new Coordenada(linha.OrigemLat, linha.OrigemLng);
                if (linha.VoltaTerminaNoUltimoAluno)
                {
                    destinoAluno = paradas.OrderByDescending(p => Distancia(origem, p.Local)).First();
                    destino = destinoAluno.Local;
                }
                partida = Localizar(partidaLocal, linha.HorarioSaidaVolta);
            }

            var paradasIntermediarias = destinoAluno is null ? paradas : paradas.Where(p => p.UsuarioId != destinoAluno.UsuarioId).ToArray();
            var resultado = await _roteirizador.CalcularAsync(origem, destino, paradasIntermediarias, partida,
                otimizarOrdem: true, cancellationToken: ct);
            if (sentidoIda)
            {
                var saidaSugerida = Localizar(partidaLocal, linha.HorarioChegadaIda)
                    .AddSeconds(-resultado.DuracaoSegundos).AddMinutes(-linha.FolgaMinutos);
                var paradasOrdenadas = resultado.Paradas.Join(paradasIntermediarias, r => r.UsuarioId, p => p.UsuarioId,
                    (r, p) => p).ToArray();
                resultado = await _roteirizador.CalcularAsync(origem, destino, paradasOrdenadas, saidaSugerida,
                    otimizarOrdem: false, cancellationToken: ct);
            }

            var novas = resultado.Paradas.ToList();
            if (destinoAluno is not null)
                novas.Add(new ParadaCalculada(destinoAluno.UsuarioId, novas.Count + 1,
                    partida.AddSeconds(resultado.DuracaoSegundos), 0));
            _db.ViagensParadas.RemoveRange(paradasAntigas);
            foreach (var parada in novas)
            {
                var ponto = candidatos.FirstOrDefault(p => p.UsuarioId == parada.UsuarioId);
                _db.ViagensParadas.Add(new ViagemParada
                {
                    ViagemId = viagem.Id,
                    UsuarioId = parada.UsuarioId,
                    Ordem = parada.Ordem,
                    EtaPrevisto = parada.EtaPrevisto,
                    EnderecoEmbarque = ponto?.EnderecoEmbarque,
                    LatitudeEmbarque = ponto?.LatitudeEmbarque,
                    LongitudeEmbarque = ponto?.LongitudeEmbarque
                });
            }
            viagem.HashConfirmados = hash;
            viagem.CalculadaEm = _time.GetUtcNow();
            viagem.RoteirizacaoFalhouEm = null;
            viagem.DistanciaMetros = resultado.DistanciaMetros;
            viagem.DuracaoSegundos = resultado.DuracaoSegundos;
            viagem.HorarioSaidaSugerido = resultado.SaidaSugerida;
            viagem.HoraInicio = TimeSpan.FromTicks(resultado.SaidaSugerida.ToOffset(TimeSpan.FromHours(-3)).TimeOfDay.Ticks);
            viagem.StatusRoteirizacao = "Calculada";
            await _db.SaveChangesAsync(ct);
            return new ResultadoRecalculoRota(true, false, null);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (!ct.IsCancellationRequested
            && exception is HttpRequestException or TaskCanceledException or InvalidOperationException or JsonException or ArgumentException)
        {
            var agora = _time.GetUtcNow();
            viagem.RoteirizacaoFalhouEm = agora;
            if (paradasAntigas.Count == 0)
            {
                var data = DateOnly.FromDateTime(viagem.Data);
                var ida = (viagem.Sentido ?? SentidoViagem.Ida) == SentidoViagem.Ida;
                var origemFallback = ida
                    ? new Coordenada(linha.OrigemLat, linha.OrigemLng)
                    : new Coordenada(linha.DestinoLat, linha.DestinoLng);
                var destinoFallback = ida
                    ? new Coordenada(linha.DestinoLat, linha.DestinoLng)
                    : new Coordenada(linha.OrigemLat, linha.OrigemLng);
                var horarioReferencia = ida
                    ? Localizar(data, linha.HorarioChegadaIda)
                    : Localizar(data, linha.HorarioSaidaVolta);
                var estimativa = await new FakeRoteirizador().CalcularAsync(origemFallback, destinoFallback,
                    paradas, horarioReferencia, otimizarOrdem: true, cancellationToken: ct);
                var saida = ida
                    ? horarioReferencia.AddSeconds(-estimativa.DuracaoSegundos).AddMinutes(-linha.FolgaMinutos)
                    : horarioReferencia;
                var fallbackFinal = await new FakeRoteirizador().CalcularAsync(origemFallback, destinoFallback,
                    estimativa.Paradas.Join(paradas, r => r.UsuarioId, p => p.UsuarioId, (_, p) => p).ToArray(),
                    saida, otimizarOrdem: false, cancellationToken: ct);
                foreach (var parada in fallbackFinal.Paradas)
                {
                    var ponto = candidatos.FirstOrDefault(p => p.UsuarioId == parada.UsuarioId);
                    _db.ViagensParadas.Add(new ViagemParada
                    {
                        ViagemId = viagem.Id, UsuarioId = parada.UsuarioId,
                        Ordem = parada.Ordem, EtaPrevisto = parada.EtaPrevisto,
                        EnderecoEmbarque = ponto?.EnderecoEmbarque,
                        LatitudeEmbarque = ponto?.LatitudeEmbarque,
                        LongitudeEmbarque = ponto?.LongitudeEmbarque
                    });
                }
                viagem.HashConfirmados = hash;
                viagem.CalculadaEm = agora;
                viagem.StatusRoteirizacao = "Fallback";
                viagem.DistanciaMetros = fallbackFinal.DistanciaMetros;
                viagem.DuracaoSegundos = fallbackFinal.DuracaoSegundos;
                viagem.HorarioSaidaSugerido = saida;
                viagem.HoraInicio = TimeSpan.FromTicks(saida.ToOffset(TimeSpan.FromHours(-3)).TimeOfDay.Ticks);
            }
            else
            {
                viagem.StatusRoteirizacao = "Calculada";
            }
            await _db.SaveChangesAsync(ct);
            _logger.LogWarning("Falha ao calcular rota da viagem {ViagemId}; endereço e identificação de passageiros omitidos.", viagem.Id);
            return new ResultadoRecalculoRota(true, false,
                paradasAntigas.Count > 0 ? "Não foi possível atualizar a rota; exibindo a última rota válida." : "Não foi possível calcular a rota agora. Tente novamente em alguns minutos.");
        }
    }

    public async Task<int> RecalcularPrazosEncerradosAsync(CancellationToken ct = default)
    {
        var agora = TimeZoneInfo.ConvertTime(_time.GetUtcNow(), FusoSaoPaulo);
        var dataInicial = DateTime.SpecifyKind(agora.Date.AddDays(-1), DateTimeKind.Unspecified);
        var dataFinal = DateTime.SpecifyKind(agora.Date.AddDays(14), DateTimeKind.Unspecified);
        var ids = await _db.Viagens.Where(v => v.LinhaId.HasValue && v.Data >= dataInicial && v.Data <= dataFinal
            && v.Status != "Cancelada" && v.Status != "Finalizada" && v.Status != "Concluida")
            .Select(v => v.Id).ToListAsync(ct);
        var processadas = 0;
        foreach (var id in ids)
        {
            var viagem = await _db.Viagens.Include(v => v.Linha).FirstOrDefaultAsync(v => v.Id == id, ct);
            if (viagem?.Linha is null || agora < LimiteLocal(viagem)) continue;
            await RecalcularSeNecessarioAsync(id, ct);
            processadas++;
        }
        return processadas;
    }

    public static string CriarHashConfirmados(IEnumerable<int> ids, DateTime data, SentidoViagem sentido)
    {
        var carga = $"{data:yyyy-MM-dd}|{(int)sentido}|{string.Join(',', ids.Distinct().Order())}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(carga)));
    }

    private static bool TemCoordenada(double latitude, double longitude) =>
        double.IsFinite(latitude) && double.IsFinite(longitude)
        && latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180
        && (latitude != 0 || longitude != 0);

    private DateTimeOffset LimiteLocal(Viagem viagem)
    {
        if (viagem.Sentido == SentidoViagem.Volta)
            return Localizar(DateOnly.FromDateTime(viagem.Data), viagem.Linha!.HorarioSaidaVolta).Add(-_prazo.AntecedenciaLimiteVolta);
        return Localizar(DateOnly.FromDateTime(viagem.Data).AddDays(-1), _prazo.HorarioLimiteIda);
    }

    private static DateTimeOffset Localizar(DateOnly data, TimeSpan hora)
    {
        var local = data.ToDateTime(TimeOnly.FromTimeSpan(hora));
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, FusoSaoPaulo), TimeSpan.Zero);
    }

    private static int Distancia(Coordenada a, Coordenada b)
    {
        const double r = 6_371_000;
        var lat1 = a.Lat * Math.PI / 180; var lat2 = b.Lat * Math.PI / 180;
        var dLat = lat2 - lat1; var dLng = (b.Lng - a.Lng) * Math.PI / 180;
        var h = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLng / 2), 2);
        return (int)(r * 2 * Math.Asin(Math.Sqrt(h)));
    }

    private static readonly TimeZoneInfo FusoSaoPaulo = ObterFuso();
    private static TimeZoneInfo ObterFuso()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); }
    }
}

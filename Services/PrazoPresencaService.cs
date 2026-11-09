using EasyVan.Models;

namespace EasyVan.Services;

public sealed class PrazoPresencaOptions
{
    public TimeSpan HorarioLimiteIda { get; set; } = new(21, 0, 0);
    public TimeSpan AntecedenciaLimiteVolta { get; set; } = TimeSpan.FromHours(2);
}

public sealed record ResultadoPrazoPresenca(
    bool PodeAlterar,
    DateTime? LimiteLocal,
    DateTimeOffset AgoraUtc,
    string Mensagem);

public sealed class PrazoPresencaService
{
    private readonly PrazoPresencaOptions _options;
    private readonly TimeProvider _timeProvider;

    public PrazoPresencaService(PrazoPresencaOptions options, TimeProvider timeProvider)
    {
        if (options.HorarioLimiteIda < TimeSpan.Zero || options.HorarioLimiteIda >= TimeSpan.FromDays(1))
            throw new ArgumentOutOfRangeException(nameof(options), "O limite da ida deve estar entre 00:00 e 23:59.");
        if (options.AntecedenciaLimiteVolta <= TimeSpan.Zero || options.AntecedenciaLimiteVolta > TimeSpan.FromDays(1))
            throw new ArgumentOutOfRangeException(nameof(options), "A antecedência da volta deve ser maior que zero e no máximo 24 horas.");

        _options = options;
        _timeProvider = timeProvider;
    }

    public ResultadoPrazoPresenca Avaliar(Viagem viagem)
    {
        var agoraUtc = _timeProvider.GetUtcNow();
        var agoraLocal = TimeZoneInfo.ConvertTime(agoraUtc, FusoSaoPaulo).DateTime;
        var data = DateTime.SpecifyKind(viagem.Data.Date, DateTimeKind.Unspecified);
        DateTime limite;

        if (viagem.Sentido == SentidoViagem.Ida)
        {
            limite = data.AddDays(-1).Add(_options.HorarioLimiteIda);
        }
        else if (viagem.Sentido == SentidoViagem.Volta)
        {
            if (!viagem.HoraInicio.HasValue)
                return Bloqueada(null, agoraUtc, "Esta viagem não tem horário de saída definido.");

            limite = data.Add(viagem.HoraInicio.Value).Subtract(_options.AntecedenciaLimiteVolta);
        }
        else
        {
            if (!viagem.HoraInicio.HasValue)
                return Bloqueada(null, agoraUtc, "Esta viagem antiga não tem sentido nem horário definidos. Consulte o administrador.");

            var limiteIda = data.AddDays(-1).Add(_options.HorarioLimiteIda);
            var limiteVolta = data.Add(viagem.HoraInicio.Value).Subtract(_options.AntecedenciaLimiteVolta);
            limite = limiteIda <= limiteVolta ? limiteIda : limiteVolta;
        }

        return agoraLocal <= limite
            ? new ResultadoPrazoPresenca(true, limite, agoraUtc, string.Empty)
            : Bloqueada(limite, agoraUtc, "O prazo para responder esta viagem encerrou.");
    }

    private static ResultadoPrazoPresenca Bloqueada(DateTime? limite, DateTimeOffset agoraUtc, string mensagem) =>
        new(false, limite, agoraUtc, mensagem);

    public DateTime AgoraLocal() => TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), FusoSaoPaulo).DateTime;

    private static readonly TimeZoneInfo FusoSaoPaulo = ObterFusoSaoPaulo();

    private static TimeZoneInfo ObterFusoSaoPaulo()
    {
        foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        return TimeZoneInfo.Utc;
    }
}

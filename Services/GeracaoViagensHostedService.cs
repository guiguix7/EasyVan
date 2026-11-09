using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace EasyVan.Services;

public sealed class GeracaoViagensOptions
{
    public int HorizonteDias { get; set; } = 14;
    public int IntervaloVerificacaoMinutos { get; set; } = 15;
}

public sealed class GeracaoViagensHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly GeracaoViagensOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<GeracaoViagensHostedService> _logger;

    public GeracaoViagensHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<GeracaoViagensOptions> options,
        TimeProvider timeProvider,
        ILogger<GeracaoViagensHostedService> logger)
    {
        if (options.Value.HorizonteDias is < 1 or > 32)
            throw new ArgumentOutOfRangeException(nameof(options), "O horizonte deve estar entre 1 e 32 dias úteis.");
        if (options.Value.IntervaloVerificacaoMinutos is < 1 or > 1440)
            throw new ArgumentOutOfRangeException(nameof(options), "O intervalo de verificação deve estar entre 1 e 1440 minutos.");

        _scopeFactory = scopeFactory;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var hojeLocal = HojeLocal(_timeProvider.GetUtcNow());
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<EasyVan.Data.ApplicationDbContext>();
                var organizacoes = await db.Organizacoes.AsNoTracking().Select(o => o.Id).ToListAsync(stoppingToken);
                var fim = UltimoDiaUtil(hojeLocal, _options.HorizonteDias);
                foreach (var organizacaoId in organizacoes)
                {
                    db.ChangeTracker.Clear();
                    db.DefinirOrganizacaoAtual(organizacaoId);
                    var geracao = scope.ServiceProvider.GetRequiredService<GeracaoViagemService>();
                    var geracaoLinhas = scope.ServiceProvider.GetRequiredService<GeracaoViagensLinhaService>();
                    var resultado = await geracao.GerarPeriodoAsync(hojeLocal, fim, stoppingToken);
                    var resultadoLinhas = await geracaoLinhas.GerarPeriodoAsync(hojeLocal, fim, stoppingToken);

                    if (resultado.TotalCriadas + resultadoLinhas.Criadas > 0)
                        _logger.LogInformation(
                            "Geração diária concluída para a organização {OrganizacaoId}. Viagens de agenda: {AgendaCriadas}; viagens de linhas: {LinhasCriadas}; existentes: {JaExistiam}.",
                            organizacaoId,
                            resultado.TotalCriadas,
                            resultadoLinhas.Criadas,
                            resultado.JaExistiam + resultadoLinhas.Existentes);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Falha na geração diária de viagens; haverá nova tentativa.");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromMinutes(_options.IntervaloVerificacaoMinutos),
                    _timeProvider,
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private static DateTime HojeLocal(DateTimeOffset instanteUtc)
    {
        var horarioLocal = TimeZoneInfo.ConvertTime(instanteUtc, FusoSaoPaulo).DateTime.Date;
        return DateTime.SpecifyKind(horarioLocal, DateTimeKind.Unspecified);
    }

    private static readonly TimeZoneInfo FusoSaoPaulo = ObterFusoSaoPaulo();

    private static DateTime UltimoDiaUtil(DateTime inicio, int diasUteis)
    {
        var dia = inicio.Date;
        var contados = 0;
        while (contados < diasUteis)
        {
            if (dia.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday) contados++;
            if (contados < diasUteis) dia = dia.AddDays(1);
        }
        return dia;
    }

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

using EasyVan.Data;
using Microsoft.EntityFrameworkCore;

namespace EasyVan.Services;

public sealed class RoteirizacaoHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly TimeProvider _time;
    private readonly ILogger<RoteirizacaoHostedService> _logger;

    public RoteirizacaoHostedService(IServiceScopeFactory scopes, TimeProvider time, ILogger<RoteirizacaoHostedService> logger)
    {
        _scopes = scopes; _time = time; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var organizacoes = await db.Organizacoes.AsNoTracking().Select(o => o.Id).ToListAsync(stoppingToken);
                foreach (var organizacaoId in organizacoes)
                {
                    db.ChangeTracker.Clear();
                    db.DefinirOrganizacaoAtual(organizacaoId);
                    var roteirizacao = scope.ServiceProvider.GetRequiredService<RoteirizacaoService>();
                    await roteirizacao.RecalcularPrazosEncerradosAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch
            {
                _logger.LogWarning("A rotina de roteirização não foi concluída; haverá nova tentativa.");
            }

            try { await Task.Delay(TimeSpan.FromMinutes(5), _time, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}

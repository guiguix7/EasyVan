using EasyVan.Data;
using EasyVan.Models;
using Microsoft.EntityFrameworkCore;

namespace EasyVan.Services;

public sealed record ViagemGeradaResumo(
    int ViagemId,
    int RotaId,
    SentidoViagem Sentido,
    DateTime Data,
    int? VanId,
    int? MotoristaId,
    int PresencasCriadas);

public sealed record ResultadoGeracaoViagens(
    IReadOnlyList<ViagemGeradaResumo> ViagensCriadas,
    int JaExistiam)
{
    public int TotalCriadas => ViagensCriadas.Count;
}

public sealed class GeracaoViagemService
{
    private readonly ApplicationDbContext _db;
    private readonly PresencaViagemService _presencas;
    private readonly TimeProvider _timeProvider;

    public GeracaoViagemService(
        ApplicationDbContext db,
        PresencaViagemService presencas,
        TimeProvider timeProvider)
    {
        _db = db;
        _presencas = presencas;
        _timeProvider = timeProvider;
    }

    public async Task<ResultadoGeracaoViagens> GerarPeriodoAsync(
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken = default)
    {
        var primeiroDia = DateTime.SpecifyKind(inicio.Date, DateTimeKind.Unspecified);
        var ultimoDia = DateTime.SpecifyKind(fim.Date, DateTimeKind.Unspecified);
        if (ultimoDia < primeiroDia)
            throw new ArgumentException("A data final não pode ser anterior à inicial.", nameof(fim));
        if ((ultimoDia - primeiroDia).TotalDays > 31)
            throw new ArgumentOutOfRangeException(nameof(fim), "O período de geração não pode exceder 32 dias.");

        var horarios = await _db.HorariosRota
            .AsNoTracking()
            .Include(h => h.Rota)
            .Include(h => h.VanPadrao)
            .Where(h => h.Ativo && h.Rota != null && h.Rota.Ativo)
            .OrderBy(h => h.Id)
            .ToListAsync(cancellationToken);

        var criadas = new List<ViagemGeradaResumo>();
        var jaExistiam = 0;

        for (var data = primeiroDia; data <= ultimoDia; data = data.AddDays(1))
        {
            var dia = (DiasSemana)(1 << (int)data.DayOfWeek);
            foreach (var horario in horarios.Where(h => (h.DiasSemana & dia) != 0))
            {
                var existe = await _db.Viagens.AnyAsync(v => v.HorarioRotaId == horario.Id && v.Data == data, cancellationToken);
                if (existe)
                {
                    jaExistiam++;
                    continue;
                }

                await using var transacao = await _db.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    existe = await _db.Viagens.AnyAsync(v => v.HorarioRotaId == horario.Id && v.Data == data, cancellationToken);
                    if (existe)
                    {
                        await transacao.RollbackAsync(cancellationToken);
                        jaExistiam++;
                        continue;
                    }

                    var viagem = new Viagem
                    {
                        RotaId = horario.RotaId,
                        Sentido = horario.Sentido,
                        HorarioRotaId = horario.Id,
                        Data = data,
                        HoraInicio = horario.HorarioSaida,
                        VanId = horario.VanPadraoId,
                        MotoristaId = horario.VanPadrao?.MotoristaId,
                        Status = "Agendada",
                        DataCriacao = _timeProvider.GetUtcNow().UtcDateTime
                    };

                    _db.Viagens.Add(viagem);
                    await _db.SaveChangesAsync(cancellationToken);
                    var resultadoPresencas = await _presencas.GerarAsync(viagem.Id, cancellationToken);
                    await transacao.CommitAsync(cancellationToken);

                    criadas.Add(new ViagemGeradaResumo(
                        viagem.Id,
                        viagem.RotaId,
                        horario.Sentido,
                        data,
                        viagem.VanId,
                        viagem.MotoristaId,
                        resultadoPresencas.Criadas));
                }
                catch (DbUpdateException)
                {
                    await transacao.RollbackAsync(cancellationToken);
                    _db.ChangeTracker.Clear();
                    existe = await _db.Viagens.AsNoTracking()
                        .AnyAsync(v => v.HorarioRotaId == horario.Id && v.Data == data, cancellationToken);
                    if (!existe)
                        throw;

                    jaExistiam++;
                }
            }
        }

        return new ResultadoGeracaoViagens(criadas, jaExistiam);
    }
}

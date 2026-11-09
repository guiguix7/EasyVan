using EasyVan.Data;
using EasyVan.Models;
using Microsoft.EntityFrameworkCore;

namespace EasyVan.Services;

public sealed record ResumoViagensLinha(int Criadas, int Existentes);

public sealed class GeracaoViagensLinhaService
{
    private readonly ApplicationDbContext _db;
    private readonly TimeProvider _time;

    public GeracaoViagensLinhaService(ApplicationDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    public async Task<ResumoViagensLinha> GerarPeriodoAsync(DateTime inicio, DateTime fim, CancellationToken ct = default)
    {
        if (fim.Date < inicio.Date || (fim.Date - inicio.Date).TotalDays > 31)
            throw new ArgumentOutOfRangeException(nameof(fim), "O período deve estar entre 1 e 32 dias de calendário.");
        var linhas = await _db.Linhas.AsNoTracking().Include(l => l.Vans).Include(l => l.Passageiros)
            .Where(l => l.Ativa).OrderBy(l => l.Id).ToListAsync(ct);
        var criadas = 0;
        var existentes = 0;
        var passageiros = await _db.Alunos.Where(a => a.UsuarioId.HasValue && a.Ativo)
            .Select(a => new { a.UsuarioId, a.Id }).ToListAsync(ct);
        var alunoPorUsuario = passageiros.ToDictionary(a => a.UsuarioId!.Value, a => a.Id);

        for (var dia = inicio.Date; dia <= fim.Date; dia = dia.AddDays(1))
        {
            var flag = (DiasSemana)(1 << (int)dia.DayOfWeek);
            foreach (var linha in linhas.Where(l => (l.DiasSemana & flag) != 0))
            foreach (var alocacaoVan in linha.Vans)
            foreach (var sentido in Sentidos(linha))
            {
                var existe = await _db.Viagens.AnyAsync(v => v.LinhaId == linha.Id && v.VanId == alocacaoVan.VanId
                    && v.Data == DateTime.SpecifyKind(dia, DateTimeKind.Unspecified) && v.Sentido == sentido, ct);
                if (existe) { existentes++; continue; }

                var viagem = new Viagem
                {
                    LinhaId = linha.Id,
                    RotaId = linha.RotaLegadaId,
                    VanId = alocacaoVan.VanId,
                    MotoristaId = alocacaoVan.MotoristaId,
                    Data = DateTime.SpecifyKind(dia, DateTimeKind.Unspecified),
                    Sentido = sentido,
                    HoraInicio = sentido == SentidoViagem.Volta ? linha.HorarioSaidaVolta : null,
                    Status = "Agendada",
                    StatusRoteirizacao = "Planejada",
                    DataCriacao = _time.GetUtcNow().UtcDateTime
                };
                _db.Viagens.Add(viagem);
                foreach (var passageiro in linha.Passageiros.Where(p => p.VanId == alocacaoVan.VanId))
                {
                    if (!alunoPorUsuario.TryGetValue(passageiro.UsuarioId, out var alunoId)) continue;
                    viagem.Presencas.Add(new PresencaViagem
                    {
                        AlunoId = alunoId,
                        UsuarioId = passageiro.UsuarioId,
                        Status = StatusPresenca.SemResposta,
                        Presente = false
                    });
                }

                try
                {
                    await _db.SaveChangesAsync(ct);
                    criadas++;
                }
                catch (DbUpdateException)
                {
                    _db.ChangeTracker.Clear();
                    existe = await _db.Viagens.AsNoTracking().AnyAsync(v => v.LinhaId == linha.Id
                        && v.VanId == alocacaoVan.VanId && v.Data == DateTime.SpecifyKind(dia, DateTimeKind.Unspecified)
                        && v.Sentido == sentido, ct);
                    if (!existe) throw;
                    existentes++;
                }
            }
        }

        return new ResumoViagensLinha(criadas, existentes);
    }

    private static IEnumerable<SentidoViagem> Sentidos(Linha linha)
    {
        if (linha.TemIda) yield return SentidoViagem.Ida;
        if (linha.TemVolta) yield return SentidoViagem.Volta;
    }
}

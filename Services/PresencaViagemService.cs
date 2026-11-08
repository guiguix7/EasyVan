using EasyVan.Data;
using EasyVan.Models;
using Microsoft.EntityFrameworkCore;

namespace EasyVan.Services
{
    public sealed record ResultadoGeracaoPresencas(
        int Criadas,
        int JaExistiam,
        bool UsouFallback,
        string? Aviso);

    public class PresencaViagemService
    {
        private readonly ApplicationDbContext _db;

        public PresencaViagemService(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Gera presenças para os alunos com vínculo ativo na rota e no sentido da viagem.
        /// Sem vínculo algum, mantém o comportamento legado (todos os alunos ativos) e avisa.
        /// </summary>
        public async Task<ResultadoGeracaoPresencas> GerarAsync(
            int viagemId,
            CancellationToken cancellationToken = default)
        {
            var viagem = await _db.Viagens
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == viagemId, cancellationToken);

            if (viagem is null)
                return new ResultadoGeracaoPresencas(0, 0, false, "Viagem não encontrada.");

            var existentes = await _db.PresencasViagem
                .Where(p => p.ViagemId == viagemId)
                .Select(p => p.AlunoId)
                .ToListAsync(cancellationToken);

            var alunos = new List<int>();
            if (viagem.Sentido.HasValue)
            {
                alunos = await _db.VinculosAluno
                    .Where(v => v.Ativo
                        && v.RotaId == viagem.RotaId
                        && v.Sentido == viagem.Sentido.Value)
                    .Select(v => v.AlunoId)
                    .Distinct()
                    .ToListAsync(cancellationToken);
            }

            var usouFallback = alunos.Count == 0;
            if (usouFallback)
            {
                alunos = await _db.Alunos
                    .Where(a => a.Ativo)
                    .Select(a => a.Id)
                    .ToListAsync(cancellationToken);
            }

            var novos = alunos
                .Except(existentes)
                .Distinct()
                .ToList();

            foreach (var alunoId in novos)
            {
                _db.PresencasViagem.Add(new PresencaViagem
                {
                    ViagemId = viagemId,
                    AlunoId = alunoId,
                    Presente = false,
                    Status = StatusPresenca.SemResposta
                });
            }

            if (novos.Count > 0)
                await _db.SaveChangesAsync(cancellationToken);

            return new ResultadoGeracaoPresencas(
                novos.Count,
                existentes.Count,
                usouFallback,
                usouFallback && alunos.Count > 0
                    ? "Nenhum vínculo ativo para esta rota e sentido; as presenças foram geradas para todos os alunos ativos."
                    : null);
        }
    }
}

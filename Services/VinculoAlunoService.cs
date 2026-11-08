using EasyVan.Data;
using EasyVan.Models;
using Microsoft.EntityFrameworkCore;

namespace EasyVan.Services
{
    public sealed record ResultadoVinculo(bool Sucesso, string? Erro, VinculoAluno? Vinculo)
    {
        public static ResultadoVinculo Ok(VinculoAluno vinculo) => new(true, null, vinculo);
        public static ResultadoVinculo Falha(string erro) => new(false, erro, null);
    }

    public class VinculoAlunoService
    {
        private readonly ApplicationDbContext _db;

        public VinculoAlunoService(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Valida e cria o vínculo, desativando o anterior do mesmo aluno e sentido.
        /// O chamador deve salvar e controlar a transação.
        /// </summary>
        public async Task<ResultadoVinculo> DefinirAsync(
            int alunoId,
            int rotaId,
            int paradaId,
            SentidoViagem sentido,
            DateTime? dataInicio = null,
            string? observacao = null,
            CancellationToken cancellationToken = default)
        {
            var aluno = await _db.Alunos.FirstOrDefaultAsync(a => a.Id == alunoId, cancellationToken);
            if (aluno is null)
                return ResultadoVinculo.Falha("Aluno não encontrado.");

            if (!aluno.Ativo)
                return ResultadoVinculo.Falha("O aluno está inativo.");

            var rota = await _db.Rotas.FirstOrDefaultAsync(r => r.Id == rotaId, cancellationToken);
            if (rota is null || !rota.Ativo)
                return ResultadoVinculo.Falha("Rota não encontrada ou inativa.");

            var parada = await _db.Paradas.FirstOrDefaultAsync(p => p.Id == paradaId, cancellationToken);
            if (parada is null || !parada.Ativo)
                return ResultadoVinculo.Falha("Parada não encontrada ou inativa.");

            if (parada.RotaId != rotaId)
                return ResultadoVinculo.Falha("A parada selecionada não pertence à rota.");

            if (parada.Sentido != sentido)
                return ResultadoVinculo.Falha("A parada selecionada não pertence a este sentido.");

            var inicio = dataInicio ?? DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified);

            var anteriores = await _db.VinculosAluno
                .Where(v => v.AlunoId == alunoId && v.Sentido == sentido && v.Ativo)
                .ToListAsync(cancellationToken);

            foreach (var anterior in anteriores)
            {
                anterior.Ativo = false;
                anterior.DataFim ??= inicio;
            }

            var vinculo = new VinculoAluno
            {
                AlunoId = alunoId,
                RotaId = rotaId,
                ParadaId = paradaId,
                Sentido = sentido,
                Ativo = true,
                DataInicio = inicio,
                Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim()
            };

            _db.VinculosAluno.Add(vinculo);
            return ResultadoVinculo.Ok(vinculo);
        }
    }
}

using EasyVan.Data;
using EasyVan.Models;
using Microsoft.EntityFrameworkCore;

namespace EasyVan.Services
{
    public sealed record ResultadoAgenda(bool Sucesso, string? Erro)
    {
        public static ResultadoAgenda Ok() => new(true, null);
        public static ResultadoAgenda Falha(string erro) => new(false, erro);
    }

    public class AgendaRotaService
    {
        private readonly ApplicationDbContext _db;

        public AgendaRotaService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ResultadoAgenda> ValidarHorarioAsync(
            HorarioRota horario,
            int? ignorarId = null,
            CancellationToken cancellationToken = default)
        {
            if (horario.RotaId <= 0)
                return ResultadoAgenda.Falha("Selecione a rota.");

            if (!Enum.IsDefined(horario.Sentido))
                return ResultadoAgenda.Falha("Selecione um sentido válido.");

            const DiasSemana diasValidos = DiasSemana.DiasUteis | DiasSemana.Domingo | DiasSemana.Sabado;
            if (horario.DiasSemana == DiasSemana.Nenhum || (horario.DiasSemana & ~diasValidos) != 0)
                return ResultadoAgenda.Falha("Selecione ao menos um dia da semana.");

            if (horario.HorarioSaida < TimeSpan.Zero || horario.HorarioSaida >= TimeSpan.FromDays(1))
                return ResultadoAgenda.Falha("Informe um horário de saída válido.");

            if (!await _db.Rotas.AnyAsync(r => r.Id == horario.RotaId, cancellationToken))
                return ResultadoAgenda.Falha("Rota não encontrada.");

            if (horario.VanPadraoId.HasValue
                && !await _db.Vans.AnyAsync(v => v.Id == horario.VanPadraoId.Value, cancellationToken))
                return ResultadoAgenda.Falha("Van padrão não encontrada.");

            var duplicado = await _db.HorariosRota.AnyAsync(h =>
                h.RotaId == horario.RotaId
                && h.Sentido == horario.Sentido
                && h.HorarioSaida == horario.HorarioSaida
                && h.DiasSemana == horario.DiasSemana
                && (!ignorarId.HasValue || h.Id != ignorarId.Value),
                cancellationToken);

            return duplicado
                ? ResultadoAgenda.Falha("Já existe um horário com o mesmo sentido, saída e dias da semana para esta rota.")
                : ResultadoAgenda.Ok();
        }

        public async Task<ResultadoAgenda> ValidarParadaAsync(
            Parada parada,
            int? ignorarId = null,
            CancellationToken cancellationToken = default)
        {
            if (parada.RotaId <= 0)
                return ResultadoAgenda.Falha("Selecione a rota.");

            if (!Enum.IsDefined(parada.Sentido))
                return ResultadoAgenda.Falha("Selecione um sentido válido.");

            if (parada.Ordem < 1)
                return ResultadoAgenda.Falha("A ordem da parada deve ser maior que zero.");

            if (string.IsNullOrWhiteSpace(parada.Endereco))
                return ResultadoAgenda.Falha("Informe o endereço da parada.");

            if (!await _db.Rotas.AnyAsync(r => r.Id == parada.RotaId, cancellationToken))
                return ResultadoAgenda.Falha("Rota não encontrada.");

            var duplicada = await _db.Paradas.AnyAsync(p =>
                p.RotaId == parada.RotaId
                && p.Sentido == parada.Sentido
                && p.Ordem == parada.Ordem
                && (!ignorarId.HasValue || p.Id != ignorarId.Value),
                cancellationToken);

            return duplicada
                ? ResultadoAgenda.Falha("Já existe uma parada com esta ordem para a rota e o sentido.")
                : ResultadoAgenda.Ok();
        }
    }
}

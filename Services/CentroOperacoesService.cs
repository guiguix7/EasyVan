using EasyVan.Data;
using EasyVan.Models;
using Microsoft.EntityFrameworkCore;

namespace EasyVan.Services
{
    /// <summary>
    /// Serviço do Centro de Operações Inteligente.
    /// Calcula indicadores operacionais a partir de dados reais do banco.
    /// </summary>
    public class CentroOperacoesService
    {
        private readonly ApplicationDbContext _db;
        private readonly ValidacaoOperacional _validacao;

        public CentroOperacoesService(ApplicationDbContext db, ValidacaoOperacional validacao)
        {
            _db = db;
            _validacao = validacao;
        }

        public async Task<CentroOperacoesViewModel> ObterPainelAsync()
        {
            var fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            var hojeLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, fuso).Date;
            var inicioDoDia = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(hojeLocal, DateTimeKind.Unspecified), fuso);
            var fimDoDia = inicioDoDia.AddDays(1);

            var modelo = new CentroOperacoesViewModel();

            // --- Frota ---
            modelo.TotalVans = await _db.Vans.CountAsync();
            modelo.VansOperacionais = await _db.Vans.CountAsync(v => v.Status == "Operacional");
            modelo.VansManutencao = await _db.Vans.CountAsync(v => v.Status == "Manutenção");
            modelo.VansOutros = modelo.TotalVans - modelo.VansOperacionais - modelo.VansManutencao;

            // --- Pessoas ---
            modelo.TotalMotoristas = await _db.Motoristas.CountAsync();
            modelo.MotoristasAtivos = await _db.Motoristas.CountAsync(m => m.Ativo);
            modelo.TotalAlunos = await _db.Alunos.CountAsync();
            modelo.AlunosAtivos = await _db.Alunos.CountAsync(a => a.Ativo);

            // --- Rotas ---
            modelo.TotalRotas = await _db.Rotas.CountAsync();
            modelo.RotasAtivas = await _db.Rotas.CountAsync(r => r.Ativo);

            // --- Viagens por status ---
            modelo.ViagensPlanejadas = await _db.Viagens.CountAsync(v => v.Status == "Planejada");
            modelo.ViagensConfirmadas = await _db.Viagens.CountAsync(v => v.Status == "Confirmada");
            modelo.ViagensAgendadas = await _db.Viagens.CountAsync(v => v.Status == "Agendada");
            modelo.ViagensEmAndamento = await _db.Viagens.CountAsync(v => v.Status == "EmAndamento");
            modelo.ViagensFinalizadas = await _db.Viagens.CountAsync(v => v.Status == "Finalizada");
            modelo.ViagensCanceladas = await _db.Viagens.CountAsync(v => v.Status == "Cancelada");

            // --- Viagens hoje ---
            modelo.ViagensHoje = await _db.Viagens.CountAsync(v => v.Data >= inicioDoDia && v.Data < fimDoDia);

            // --- Próximas viagens (agendadas/confirmadas/em andamento a partir de hoje) ---
            modelo.ProximasViagens = await _db.Viagens
                .Include(v => v.Van)
                .Include(v => v.Motorista)
                .Include(v => v.Rota)
                .Where(v => v.Data >= inicioDoDia
                    && (v.Status == "Planejada" || v.Status == "Confirmada"
                        || v.Status == "Agendada" || v.Status == "EmAndamento"))
                .OrderBy(v => v.Data)
                .ThenBy(v => v.Id)
                .Take(8)
                .ToListAsync();

            // --- Vans em manutenção ---
            modelo.VansEmManutencao = await _db.Vans
                .Where(v => v.Status == "Manutenção")
                .OrderBy(v => v.Plate)
                .Take(6)
                .ToListAsync();

            // --- Atividade recente (auditoria) ---
            modelo.AtividadeRecente = await _db.LogOperacao
                .Include(l => l.Usuario)
                .OrderByDescending(l => l.DataOperacao)
                .Take(10)
                .ToListAsync();

            // --- Alertas operacionais ---
            modelo.Alertas = _validacao.AnalisarAlertas().ToList();

            return modelo;
        }
    }
}
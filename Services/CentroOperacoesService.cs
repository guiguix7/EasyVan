using EasyVan.Data;
using EasyVan.Models;
using Microsoft.EntityFrameworkCore;

namespace EasyVan.Services
{
    /// <summary>
    /// Serviço do Centro de Operações.
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
            var hojeLocal = DateTime.SpecifyKind(ViagemAgenda.AgoraLocal().Date, DateTimeKind.Unspecified);
            var amanhaLocal = hojeLocal.AddDays(1);

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
            modelo.TotalRotas = await _db.Linhas.CountAsync();
            modelo.RotasAtivas = await _db.Linhas.CountAsync(r => r.Ativa);

            // --- Viagens por status ---
            modelo.ViagensPlanejadas = await _db.Viagens.CountAsync(v => v.Status == "Planejada");
            modelo.ViagensConfirmadas = await _db.Viagens.CountAsync(v => v.Status == "Confirmada");
            modelo.ViagensAgendadas = await _db.Viagens.CountAsync(v => v.Status == "Agendada");
            modelo.ViagensEmAndamento = await _db.Viagens.CountAsync(v => v.Status == "EmAndamento");
            modelo.ViagensFinalizadas = await _db.Viagens.CountAsync(v => v.Status == "Finalizada");
            modelo.ViagensCanceladas = await _db.Viagens.CountAsync(v => v.Status == "Cancelada");

            // --- Viagens hoje ---
            modelo.ViagensHoje = await _db.Viagens.CountAsync(v => v.Data >= hojeLocal && v.Data < amanhaLocal);

            // --- Próximas viagens (agendadas/confirmadas/em andamento a partir de hoje) ---
            modelo.ProximasViagens = await _db.Viagens
                .Include(v => v.Van)
                .Include(v => v.Motorista)
                .Include(v => v.Rota)
                .Where(v => v.Data >= hojeLocal
                    && (v.Status == "Planejada" || v.Status == "Confirmada"
                        || v.Status == "Agendada" || v.Status == "EmAndamento"))
                .OrderBy(v => v.Data)
                .ThenBy(v => v.Id)
                .Take(8)
                .ToListAsync();

            var viagensOcupacao = await _db.Viagens.AsNoTracking()
                .Include(v => v.Van)
                .Include(v => v.Rota)
                .Include(v => v.Linha)
                .Include(v => v.Presencas)
                .Where(v => v.Data >= hojeLocal && v.Data < hojeLocal.AddDays(8)
                    && v.Status != "Cancelada" && v.Status != "Finalizada" && v.Status != "Concluida")
                .ToListAsync();
            modelo.OcupacaoProximasViagens = viagensOcupacao
                .OrderBy(v => v.Data)
                .ThenBy(v => v.HoraInicio)
                .ThenBy(v => v.Id)
                .Take(12)
                .Select(v => new ViagemOcupacaoItemViewModel(
                v,
                v.Presencas.Count(p => p.Status == StatusPresenca.Confirmado),
                v.Presencas.Count(p => p.Status == StatusPresenca.Ausente),
                v.Presencas.Count(p => p.Status == StatusPresenca.SemResposta),
                v.Van?.Capacity)).ToList();

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
            modelo.Alertas = (await _validacao.AnalisarAlertasAsync()).ToList();

            return modelo;
        }
    }
}

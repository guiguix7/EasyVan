using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using EasyVan.Data;
using EasyVan.Models;
using System.Threading;
using System.Threading.Tasks;

namespace EasyVan.Services
{
    /// <summary>
    /// Validadores centralizados de regras operacionais do EasyVan.
    /// Todas as regras são avaliadas contra dados reais do banco, nunca contra dados fictícios.
    /// </summary>
    public class ValidacaoOperacional
    {
        private readonly ApplicationDbContext _db;
        private readonly PrazoPresencaService _prazoPresenca;

        public ValidacaoOperacional(ApplicationDbContext db, PrazoPresencaService? prazoPresenca = null)
        {
            _db = db;
            _prazoPresenca = prazoPresenca ?? new PrazoPresencaService(new PrazoPresencaOptions(), TimeProvider.System);
        }

        /// <summary>
        /// Resultado de uma validação operacional.
        /// </summary>
        public record ResultadoValidacao(
            bool Valido,
            string Tipo,
            string Titulo,
            string Descricao,
            string Gravidade, // "Impeditivo" ou "Aviso"
            int? EntidadeId,
            string? EntidadeTipo,
            string AcaoRecomendada
        );

        /// <summary>
        /// Analisa o estado atual do banco e retorna todos os alertas operacionais encontrados.
        /// </summary>
        public IEnumerable<ResultadoValidacao> AnalisarAlertas() =>
            AnalisarAlertasAsync().GetAwaiter().GetResult();

        public async Task<IReadOnlyList<ResultadoValidacao>> AnalisarAlertasAsync(CancellationToken cancellationToken = default)
        {
            var alertas = new List<ResultadoValidacao>();

            var viagens = await _db.Viagens.AsNoTracking().Include(v => v.Van).Include(v => v.Motorista)
                .ToListAsync(cancellationToken);
            var vans = await _db.Vans.AsNoTracking().ToListAsync(cancellationToken);
            var alunos = await _db.Alunos.AsNoTracking().ToListAsync(cancellationToken);
            var presencas = await _db.PresencasViagem.AsNoTracking().ToListAsync(cancellationToken);
            var linhasPassageiros = await _db.LinhasPassageiros.AsNoTracking().Include(p => p.Linha)
                .ToListAsync(cancellationToken);
            var vinculos = await _db.VinculosAluno.AsNoTracking().Include(v => v.Rota).Include(v => v.Parada)
                .ToListAsync(cancellationToken);

            // 1. Conflito de horário entre viagens do mesmo motorista
            alertas.AddRange(DetectarConflitoHorarioMotorista(viagens));

            // 2. Conflito de horário entre viagens da mesma van
            alertas.AddRange(DetectarConflitoHorarioVan(viagens));

            // A1. Confirmados acima da capacidade.
            alertas.AddRange(DetectarCapacidadeExcedida(viagens, vans, presencas));

            // A2. Viagem sem van ou motorista.
            alertas.AddRange(DetectarViagemSemAlocacao(viagens));

            // 6. Viagem com dados obrigatórios ausentes ou inconsistentes
            alertas.AddRange(DetectarDadosInconsistentes(viagens));

            alertas.AddRange(DetectarPrazoEncerradoSemResposta(viagens, presencas));
            alertas.AddRange(DetectarAlunosSemVinculo(alunos, linhasPassageiros, vinculos));
            alertas.AddRange(DetectarViagemSemConfirmados(viagens, presencas));
            alertas.AddRange(DetectarVanForaDeOperacao(viagens));

            return alertas;
        }

        private IEnumerable<ResultadoValidacao> DetectarPrazoEncerradoSemResposta(
            IReadOnlyCollection<Viagem> viagens, IReadOnlyCollection<PresencaViagem> presencas)
        {
            foreach (var viagem in viagens.Where(v => v.Status is not ("Cancelada" or "Finalizada" or "Concluida")))
            {
                var pendentes = presencas.Count(p => p.ViagemId == viagem.Id && p.Status == StatusPresenca.SemResposta);
                if (pendentes == 0 || _prazoPresenca.Avaliar(viagem).PodeAlterar) continue;

                yield return new ResultadoValidacao(
                    true, "A3", "Prazo encerrado com alunos sem resposta",
                    $"{pendentes} aluno(s) ainda não responderam à viagem de {viagem.Data:dd/MM/yyyy}.",
                    "Média", viagem.Id, "Viagem", "Confirme a operação com os alunos que ainda não responderam.");
            }
        }

        private static IEnumerable<ResultadoValidacao> DetectarAlunosSemVinculo(
            IReadOnlyCollection<Aluno> alunos,
            IReadOnlyCollection<LinhaPassageiro> passageirosLinha,
            IReadOnlyCollection<VinculoAluno> vinculos)
        {
            var usuariosComPontoOperacional = passageirosLinha
                .Where(p => p.Linha?.Ativa == true && !string.IsNullOrWhiteSpace(p.EnderecoEmbarque)
                    && p.LatitudeEmbarque.HasValue && p.LongitudeEmbarque.HasValue
                    && double.IsFinite(p.LatitudeEmbarque.Value) && double.IsFinite(p.LongitudeEmbarque.Value))
                .Select(p => p.UsuarioId)
                .ToHashSet();
            var alunosComParadaLegada = vinculos
                .Where(v => v.Ativo && v.Rota?.Ativo == true && v.Parada?.Ativo == true
                    && v.Parada.RotaId == v.RotaId && v.Parada.Sentido == v.Sentido)
                .Select(v => v.AlunoId)
                .ToHashSet();

            foreach (var aluno in alunos.Where(a => a.Ativo))
            {
                var temRotaEParada = (aluno.UsuarioId.HasValue && usuariosComPontoOperacional.Contains(aluno.UsuarioId.Value))
                    || alunosComParadaLegada.Contains(aluno.Id);
                if (temRotaEParada) continue;

                yield return new ResultadoValidacao(
                    true, "A4", "Aluno ativo sem linha ou ponto de embarque",
                    $"O cadastro de aluno #{aluno.Id} não tem vínculo ativo com uma linha e um ponto de embarque.",
                    "Média", aluno.Id, "Aluno", "Vincule o aluno a uma linha e cadastre o ponto operacional de embarque.");
            }
        }

        private static IEnumerable<ResultadoValidacao> DetectarViagemSemAlocacao(IEnumerable<Viagem> viagens)
        {
            foreach (var viagem in viagens.Where(v => v.Status is not ("Cancelada" or "Finalizada" or "Concluida")))
            {
                var semVan = !viagem.VanId.HasValue || viagem.Van is null;
                var semMotorista = !viagem.MotoristaId.HasValue || viagem.Motorista is null;
                if (!semVan && !semMotorista) continue;

                var ausentes = new List<string>();
                if (semVan) ausentes.Add("van");
                if (semMotorista) ausentes.Add("motorista");
                yield return new ResultadoValidacao(
                    true, "A2", "Viagem sem van ou motorista",
                    $"A viagem #{viagem.Id} em {viagem.Data:dd/MM/yyyy} está sem {string.Join(" e ", ausentes)}.",
                    "Alta", viagem.Id, "Viagem", "Atribua a van e o motorista antes da operação.");
            }
        }

        private static IEnumerable<ResultadoValidacao> DetectarVanForaDeOperacao(IEnumerable<Viagem> viagens)
        {
            foreach (var viagem in viagens.Where(v => v.Status is not ("Cancelada" or "Finalizada" or "Concluida")
                         && v.VanId.HasValue && v.Van is not null
                         && !string.Equals(v.Van.Status, "Operacional", StringComparison.OrdinalIgnoreCase)))
            {
                yield return new ResultadoValidacao(
                    true, "A6", "Van indisponível atribuída à viagem",
                    $"A van {viagem.Van!.Plate} está com status {viagem.Van.Status} na viagem de {viagem.Data:dd/MM/yyyy}.",
                    "Alta", viagem.Id, "Viagem", "Atribua uma van operacional a esta viagem.");
            }
        }

        private static IEnumerable<ResultadoValidacao> DetectarViagemSemConfirmados(
            IEnumerable<Viagem> viagens, IReadOnlyCollection<PresencaViagem> presencas)
        {
            foreach (var viagem in viagens.Where(v => v.Status is not ("Cancelada" or "Finalizada" or "Concluida")))
            {
                var temConfirmado = presencas.Any(p => p.ViagemId == viagem.Id
                    && p.Status == StatusPresenca.Confirmado);
                if (temConfirmado) continue;

                yield return new ResultadoValidacao(
                    true, "A5", "Viagem sem passageiros confirmados",
                    "Nenhum passageiro confirmou presença; a rota não será calculada até haver confirmações.",
                    "Baixa", viagem.Id, "Viagem", "Acompanhe as respostas ou confirme a operação com os passageiros.");
            }
        }

        /// <summary>
        /// Valida se uma nova viagem pode ser criada sem conflitos operacionais impeditivos.
        /// </summary>
        public IEnumerable<ResultadoValidacao> ValidarNovaViagem(
            int vanId,
            int motoristaId,
            int rotaId,
            DateTime data,
            TimeSpan? horaInicio = null,
            TimeSpan? horaFim = null,
            int? viagemIdParaIgnorar = null)
        {
            var erros = new List<ResultadoValidacao>();

            var van = _db.Vans.FirstOrDefault(v => v.Id == vanId);
            var motorista = _db.Motoristas.FirstOrDefault(m => m.Id == motoristaId);
            var rota = _db.Rotas.FirstOrDefault(r => r.Id == rotaId);

            if (van == null)
                erros.Add(new ResultadoValidacao(
                    false, "VanInexistente", "Van não encontrada",
                    "A van selecionada não existe no cadastro.", "Impeditivo",
                    vanId, "Van", "Selecione uma van válida."));

            if (motorista == null)
                erros.Add(new ResultadoValidacao(
                    false, "MotoristaInexistente", "Motorista não encontrado",
                    "O motorista selecionado não existe no cadastro.", "Impeditivo",
                    motoristaId, "Motorista", "Selecione um motorista válido."));

            if (rota == null)
                erros.Add(new ResultadoValidacao(
                    false, "RotaInexistente", "Rota não encontrada",
                    "A rota selecionada não existe no cadastro.", "Impeditivo",
                    rotaId, "Rota", "Selecione uma rota válida."));

            if (van == null || motorista == null || rota == null)
                return erros;

            if (!van.MotoristaId.HasValue || van.MotoristaId != motoristaId)
            {
                // Avanço: não bloquear, apenas avisar que o motorista não está vinculado à van
                erros.Add(new ResultadoValidacao(
                    true, "MotoristaNaoVinculado", "Motorista não vinculado à van",
                    $"O motorista {motorista.Nome} não está vinculado à van {van.Plate}.", "Aviso",
                    vanId, "Van", "Vincule o motorista à van no cadastro da van."));
            }

            // Verificar conflito de horário com viagens existentes
            var viagensExistentes = _db.Viagens
                .Where(v => v.Id != (viagemIdParaIgnorar ?? 0))
                .ToList();

            foreach (var existente in viagensExistentes)
            {
                if (existente.MotoristaId == motoristaId && HorariosSobrepostos(data, horaInicio, horaFim, existente))
                {
                    erros.Add(new ResultadoValidacao(
                        false, "ConflitoHorarioMotorista", "Conflito de horário do motorista",
                        $"O motorista {motorista.Nome} já tem uma viagem em {existente.Data:dd/MM/yyyy} com horário sobreposto.", "Impeditivo",
                        existente.Id, "Viagem", "Ajuste o horário ou escolha outro motorista."));
                }

                if (existente.VanId == vanId && HorariosSobrepostos(data, horaInicio, horaFim, existente))
                {
                    erros.Add(new ResultadoValidacao(
                        false, "ConflitoHorarioVan", "Conflito de horário da van",
                        $"A van {van.Plate} já tem uma viagem em {existente.Data:dd/MM/yyyy} com horário sobreposto.", "Impeditivo",
                        existente.Id, "Viagem", "Ajuste o horário ou escolha outra van."));
                }
            }

            return erros;
        }

        /// <summary>
        /// Valida se a quantidade de passageiros vinculados excede a capacidade da van.
        /// </summary>
        public IEnumerable<ResultadoValidacao> ValidarCapacidade(int viagemId)
        {
            var erros = new List<ResultadoValidacao>();

            var viagem = _db.Viagens
                .Include(v => v.Van)
                .FirstOrDefault(v => v.Id == viagemId);

            if (viagem == null || viagem.Van == null)
                return erros;

            var totalPassageiros = _db.PresencasViagem
                .Count(p => p.ViagemId == viagemId && p.Status == StatusPresenca.Confirmado);

            if (totalPassageiros > viagem.Van.Capacity)
            {
                erros.Add(new ResultadoValidacao(
                    false, "CapacidadeExcedida", "Capacidade excedida",
                    $"A viagem tem {totalPassageiros} passageiro(s) vinculado(s), mas a van {viagem.Van.Plate} comporta {viagem.Van.Capacity}.",
                    "Impeditivo", viagemId, "Viagem", "Reduza o número de passageiros ou use uma van com maior capacidade."));
            }

            return erros;
        }

        private static bool HorariosSobrepostos(
            DateTime dataNova,
            TimeSpan? inicioNova,
            TimeSpan? fimNova,
            Viagem existente)
        {
            if (existente.Data.Date != dataNova.Date)
                return false;

            var inicioA = inicioNova ?? TimeSpan.Zero;
            var fimA = fimNova ?? (inicioNova ?? TimeSpan.Zero).Add(TimeSpan.FromHours(2));
            var inicioB = existente.HoraInicio ?? TimeSpan.Zero;
            var fimB = existente.HoraFim ?? (inicioB).Add(TimeSpan.FromHours(2));

            return inicioA < fimB && inicioB < fimA;
        }

        private IEnumerable<ResultadoValidacao> DetectarConflitoHorarioMotorista(List<Viagem> viagens)
        {
            var alertas = new List<ResultadoValidacao>();

            var gruposMotorista = viagens
                .Where(v => v.MotoristaId.HasValue)
                .GroupBy(v => v.MotoristaId!.Value);

            foreach (var grupo in gruposMotorista)
            {
                var viagensDoMotorista = grupo.ToList();
                for (var i = 0; i < viagensDoMotorista.Count; i++)
                {
                    for (var j = i + 1; j < viagensDoMotorista.Count; j++)
                    {
                        var a = viagensDoMotorista[i];
                        var b = viagensDoMotorista[j];
                        if (HorariosSobrepostos(a.Data, a.HoraInicio, a.HoraFim, b))
                        {
                            alertas.Add(new ResultadoValidacao(
                                true, "ConflitoHorarioMotorista", "Conflito de horário do motorista",
                                $"O motorista {a.Motorista?.Nome ?? "não informado"} tem viagens sobrepostas em {a.Data:dd/MM/yyyy}.",
                                "Impeditivo", a.Id, "Viagem", "Ajuste os horários ou atribua outro motorista."));
                        }
                    }
                }
            }

            return alertas;
        }

        private IEnumerable<ResultadoValidacao> DetectarConflitoHorarioVan(List<Viagem> viagens)
        {
            var alertas = new List<ResultadoValidacao>();

            var gruposVan = viagens
                .Where(v => v.VanId.HasValue)
                .GroupBy(v => v.VanId!.Value);

            foreach (var grupo in gruposVan)
            {
                var viagensDaVan = grupo.ToList();
                for (var i = 0; i < viagensDaVan.Count; i++)
                {
                    for (var j = i + 1; j < viagensDaVan.Count; j++)
                    {
                        var a = viagensDaVan[i];
                        var b = viagensDaVan[j];
                        if (HorariosSobrepostos(a.Data, a.HoraInicio, a.HoraFim, b))
                        {
                            alertas.Add(new ResultadoValidacao(
                                true, "ConflitoHorarioVan", "Conflito de horário da van",
                                $"A van {a.Van?.Plate ?? "não informada"} tem viagens sobrepostas em {a.Data:dd/MM/yyyy}.",
                                "Impeditivo", a.Id, "Viagem", "Ajuste os horários ou use outra van."));
                        }
                    }
                }
            }

            return alertas;
        }

        private IEnumerable<ResultadoValidacao> DetectarCapacidadeExcedida(
            List<Viagem> viagens, List<Van> vans, List<PresencaViagem> presencas)
        {
            var alertas = new List<ResultadoValidacao>();
            var vansPorId = vans.ToDictionary(v => v.Id);

            foreach (var viagem in viagens)
            {
                if (!viagem.VanId.HasValue || !vansPorId.TryGetValue(viagem.VanId.Value, out var van)) continue;

                var totalPassageiros = presencas.Count(p =>
                    p.ViagemId == viagem.Id && p.Status == StatusPresenca.Confirmado);
                if (viagem.Status is not ("Cancelada" or "Finalizada" or "Concluida") && totalPassageiros > van.Capacity)
                {
                    alertas.Add(new ResultadoValidacao(
                        true, "A1", "Capacidade excedida",
                        $"A viagem #{viagem.Id} tem {totalPassageiros} passageiro(s), mas a van {van.Plate} comporta {van.Capacity}.",
                        "Alta", viagem.Id, "Viagem", "Reduza passageiros ou troque a van."));
                }
            }

            return alertas;
        }

        private IEnumerable<ResultadoValidacao> DetectarViagemSemMotorista(List<Viagem> viagens)
        {
            var alertas = new List<ResultadoValidacao>();

            foreach (var viagem in viagens)
            {
                if (!viagem.MotoristaId.HasValue)
                {
                    alertas.Add(new ResultadoValidacao(
                        true, "ViagemSemMotorista", "Viagem sem motorista",
                        $"A viagem #{viagem.Id} em {viagem.Data:dd/MM/yyyy} não possui motorista associado.",
                        "Impeditivo", viagem.Id, "Viagem", "Atribua um motorista à viagem."));
                }
            }

            return alertas;
        }

        private IEnumerable<ResultadoValidacao> DetectarViagemSemVan(List<Viagem> viagens)
        {
            var alertas = new List<ResultadoValidacao>();

            foreach (var viagem in viagens)
            {
                if (!viagem.VanId.HasValue)
                {
                    alertas.Add(new ResultadoValidacao(
                        true, "ViagemSemVan", "Viagem sem veículo",
                        $"A viagem #{viagem.Id} em {viagem.Data:dd/MM/yyyy} não possui van associada.",
                        "Impeditivo", viagem.Id, "Viagem", "Atribua uma van à viagem."));
                }
            }

            return alertas;
        }

        private IEnumerable<ResultadoValidacao> DetectarDadosInconsistentes(List<Viagem> viagens)
        {
            var alertas = new List<ResultadoValidacao>();

            foreach (var viagem in viagens)
            {
                var problemas = new List<string>();

                if (string.IsNullOrWhiteSpace(viagem.Status))
                    problemas.Add("status não informado");

                if (viagem.Data == default)
                    problemas.Add("data não informada");

                if (viagem.HoraFim.HasValue && viagem.HoraInicio.HasValue &&
                    viagem.HoraFim.Value <= viagem.HoraInicio.Value)
                    problemas.Add("horário de fim anterior ou igual ao início");

                if (problemas.Count > 0)
                {
                    alertas.Add(new ResultadoValidacao(
                        true, "DadosInconsistentes", "Dados inconsistentes",
                        $"A viagem #{viagem.Id} tem: {string.Join(", ", problemas)}.",
                        "Aviso", viagem.Id, "Viagem", "Corrija os dados da viagem."));
                }
            }

            return alertas;
        }

        /// <summary>
        /// Valida se a transição de status de viagem é permitida.
        /// </summary>
        public ResultadoValidacao ValidarTransicaoStatus(string statusAtual, string statusNovo)
        {
            var transicoesValidas = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Planejada", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Confirmada", "Cancelada" } },
                { "Confirmada", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "EmAndamento", "Cancelada" } },
                { "EmAndamento", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Finalizada" } },
                { "Finalizada", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { } },
                { "Cancelada", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { } },
                { "Agendada", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Confirmada", "EmAndamento", "Finalizada", "Cancelada" } } // compatibilidade com dados existentes
            };

            if (!transicoesValidas.ContainsKey(statusAtual))
            {
                return new ResultadoValidacao(
                    false, "TransicaoInvalida", "Status atual desconhecido",
                    $"O status '{statusAtual}' não é reconhecido pelo sistema.", "Impeditivo",
                    null, "Viagem", "Corrija o status da viagem antes de alterar.");
            }

            if (!transicoesValidas[statusAtual].Contains(statusNovo))
            {
                return new ResultadoValidacao(
                    false, "TransicaoInvalida", "Transição não permitida",
                    $"Não é possível alterar de '{statusAtual}' para '{statusNovo}'.", "Impeditivo",
                    null, "Viagem", "Escolha uma transição válida para o ciclo de vida da viagem.");
            }

            return new ResultadoValidacao(
                true, "TransicaoValida", "Transição permitida",
                $"A alteração de '{statusAtual}' para '{statusNovo}' é válida.", "Aviso",
                null, "Viagem", string.Empty);
        }
    }
}

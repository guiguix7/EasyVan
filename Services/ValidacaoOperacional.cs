using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using EasyVan.Data;
using EasyVan.Models;

namespace EasyVan.Services
{
    /// <summary>
    /// Validadores centralizados de regras operacionais do EasyVan.
    /// Todas as regras são avaliadas contra dados reais do banco, nunca contra dados fictícios.
    /// </summary>
    public class ValidacaoOperacional
    {
        private readonly ApplicationDbContext _db;

        public ValidacaoOperacional(ApplicationDbContext db)
        {
            _db = db;
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
        public IEnumerable<ResultadoValidacao> AnalisarAlertas()
        {
            var alertas = new List<ResultadoValidacao>();

            var viagens = _db.Viagens.ToList();
            var vans = _db.Vans.ToList();
            var motoristas = _db.Motoristas.ToList();
            var alunos = _db.Alunos.ToList();
            var presencas = _db.PresencasViagem.ToList();

            // 1. Conflito de horário entre viagens do mesmo motorista
            alertas.AddRange(DetectarConflitoHorarioMotorista(viagens));

            // 2. Conflito de horário entre viagens da mesma van
            alertas.AddRange(DetectarConflitoHorarioVan(viagens));

            // 3. Capacidade excedida (passageiros vinculados > capacidade da van)
            alertas.AddRange(DetectarCapacidadeExcedida(viagens, vans, presencas));

            // 4. Viagem sem motorista associado
            alertas.AddRange(DetectarViagemSemMotorista(viagens));

            // 5. Viagem sem van associada
            alertas.AddRange(DetectarViagemSemVan(viagens));

            // 6. Viagem com dados obrigatórios ausentes ou inconsistentes
            alertas.AddRange(DetectarDadosInconsistentes(viagens));

            // A5. Viagem sem nenhuma presença confirmada.
            alertas.AddRange(DetectarViagemSemConfirmados(viagens, presencas));

            return alertas;
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
                if (totalPassageiros > van.Capacity)
                {
                    alertas.Add(new ResultadoValidacao(
                        true, "CapacidadeExcedida", "Capacidade excedida",
                        $"A viagem #{viagem.Id} tem {totalPassageiros} passageiro(s), mas a van {van.Plate} comporta {van.Capacity}.",
                        "Impeditivo", viagem.Id, "Viagem", "Reduza passageiros ou troque a van."));
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

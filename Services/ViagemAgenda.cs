using System.Globalization;

namespace EasyVan.Services
{
    /// <summary>
    /// Regras de calendário das viagens. Datas/horários previstos são horário de parede
    /// em America/Sao_Paulo; os instantes reais são persistidos em UTC.
    /// </summary>
    public static class ViagemAgenda
    {
        private static readonly TimeZoneInfo FusoSaoPaulo = ObterFusoSaoPaulo();

        public static DateTime AgoraLocal()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, FusoSaoPaulo);
        }

        public static DateTime ParaLocal(DateTime utc)
        {
            var instante = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(instante, FusoSaoPaulo);
        }

        public static DateTime? ParaLocal(DateTime? utc)
        {
            return utc.HasValue ? ParaLocal(utc.Value) : null;
        }

        public static TimeSpan? HoraLocal(DateTime? utc)
        {
            return utc.HasValue ? ParaLocal(utc.Value).TimeOfDay : null;
        }

        public static string FormatarData(DateTime data)
        {
            return data.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR"));
        }

        public static string FormatarDataHora(DateTime? utc)
        {
            return utc.HasValue
                ? ParaLocal(utc.Value).ToString("dd/MM/yyyy 'às' HH:mm", CultureInfo.GetCultureInfo("pt-BR"))
                : "Não registrado";
        }

        public static bool TentarMontar(
            DateTime data,
            string? horaInicio,
            DateTime? dataChegada,
            string? horaFim,
            out DateTime partida,
            out DateTime chegada)
        {
            partida = default;
            chegada = default;

            if (!ValidacaoBrasil.HorarioValido(horaInicio, out var inicio)
                || !ValidacaoBrasil.HorarioValido(horaFim, out var fim))
            {
                return false;
            }

            partida = DateTime.SpecifyKind(data.Date.Add(inicio), DateTimeKind.Unspecified);
            chegada = DateTime.SpecifyKind((dataChegada ?? data).Date.Add(fim), DateTimeKind.Unspecified);
            return chegada > partida;
        }

        public static IReadOnlyList<string> ValidarPlanejamento(
            DateTime data,
            string? horaInicio,
            DateTime? dataChegada,
            string? horaFim,
            bool exigirFuturo = true,
            DateTime? agoraLocal = null)
        {
            var erros = new List<string>();

            if (data == default)
            {
                erros.Add("Informe a data prevista de partida.");
                return erros;
            }

            if ((dataChegada ?? data) == default)
            {
                erros.Add("Informe a data prevista de chegada.");
                return erros;
            }

            if (string.IsNullOrWhiteSpace(horaInicio))
            {
                erros.Add("Informe o horário previsto de partida.");
            }

            if (string.IsNullOrWhiteSpace(horaFim))
            {
                erros.Add("Informe o horário previsto de chegada.");
            }

            if (erros.Count > 0)
            {
                return erros;
            }

            if (!ValidacaoBrasil.HorarioValido(horaInicio, out var inicio)
                || !ValidacaoBrasil.HorarioValido(horaFim, out var fim))
            {
                erros.Add("Use horários válidos no formato HH:mm.");
                return erros;
            }

            var partida = DateTime.SpecifyKind(data.Date.Add(inicio), DateTimeKind.Unspecified);
            var chegada = DateTime.SpecifyKind((dataChegada ?? data).Date.Add(fim), DateTimeKind.Unspecified);

            if (chegada <= partida)
            {
                erros.Add("A chegada prevista deve ser posterior à partida prevista.");
            }

            if (exigirFuturo)
            {
                var agora = agoraLocal ?? AgoraLocal();
                var hoje = agora.Date;
                var partidaNoMinuto = new TimeSpan(partida.Hour, partida.Minute, 0);

                if (data.Date < hoje)
                {
                    erros.Add("A data prevista de partida não pode ser anterior a hoje.");
                }
                else if (data.Date == hoje && partidaNoMinuto < agora.TimeOfDay)
                {
                    erros.Add("O horário previsto de partida não pode estar no passado.");
                }
            }

            return erros;
        }

        private static TimeZoneInfo ObterFusoSaoPaulo()
        {
            foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            return TimeZoneInfo.Utc;
        }
    }
}

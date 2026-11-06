namespace EasyVan.Models
{
    public class CalendarioViagensViewModel
    {
        public int Mes { get; set; }
        public int Ano { get; set; }
        public string NomeMes { get; set; } = string.Empty;
        public string MesAnterior { get; set; } = string.Empty;
        public string MesSeguinte { get; set; } = string.Empty;
        public IReadOnlyList<CalendarioDiaViewModel> Dias { get; set; } = Array.Empty<CalendarioDiaViewModel>();
    }

    public class CalendarioDiaViewModel
    {
        public DateTime Data { get; set; }
        public bool DoMes { get; set; }
        public IReadOnlyList<Viagem> Viagens { get; set; } = Array.Empty<Viagem>();
    }
}

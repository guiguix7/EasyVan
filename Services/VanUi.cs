using EasyVan.Models;

namespace EasyVan.Services
{
    /// <summary>
    /// Identificação visual única das vans. Nunca usa o Id interno como rótulo:
    /// a referência é a placa e, quando cadastrado, o modelo.
    /// </summary>
    public static class VanUi
    {
        public static string Identificacao(Van? van)
        {
            if (van == null)
            {
                return "Van não informada";
            }

            return string.IsNullOrWhiteSpace(van.Modelo)
                ? van.Plate
                : $"{van.Plate} · {van.Modelo.Trim()}";
        }

        public static string Motorista(Van? van)
        {
            return van?.Motorista?.Nome ?? (string.IsNullOrWhiteSpace(van?.Driver) ? "Não definido" : van.Driver);
        }

        public static string Rota(Van? van)
        {
            return van?.Rota?.Nome ?? (string.IsNullOrWhiteSpace(van?.Route) ? "Não definida" : van.Route);
        }
    }
}

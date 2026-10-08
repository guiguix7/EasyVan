// Parâmetros de busca e filtros da listagem de vans (recebidos pela query string de /Vans).
using System.ComponentModel.DataAnnotations;

namespace EasyVan.Models
{
    public class VanFiltro
    {
        public const int TamanhoMaximoBusca = 100;
        public const int CapacidadeMaximaFiltro = 1000;

        // Texto livre: pesquisado em placa, motorista, rota e descrição.
        // O limite de tamanho é validado no controller, depois do Trim.
        public string? Busca { get; set; }

        // Deve ser um dos status existentes (validado no controller).
        public string? Status { get; set; }

        [Range(0, CapacidadeMaximaFiltro, ErrorMessage = "A capacidade mínima deve ser um número inteiro entre 0 e 1000.")]
        public int? CapacidadeMin { get; set; }

        [Range(0, CapacidadeMaximaFiltro, ErrorMessage = "A capacidade máxima deve ser um número inteiro entre 0 e 1000.")]
        public int? CapacidadeMax { get; set; }

        public bool TemFiltroAtivo =>
            !string.IsNullOrWhiteSpace(Busca)
            || !string.IsNullOrWhiteSpace(Status)
            || CapacidadeMin.HasValue
            || CapacidadeMax.HasValue;
    }
}

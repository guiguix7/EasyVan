// ViewModel da listagem de vans: resultado filtrado + dados necessários para montar o formulário de filtros.
namespace EasyVan.Models
{
    public class VanIndexViewModel
    {
        public VanFiltro Filtro { get; set; } = new VanFiltro();

        public IReadOnlyList<Van> Vans { get; set; } = Array.Empty<Van>();

        // Quantidade total de vans, antes de aplicar os filtros.
        public int TotalGeral { get; set; }

        public IReadOnlyList<string> StatusDisponiveis { get; set; } = Array.Empty<string>();
    }
}

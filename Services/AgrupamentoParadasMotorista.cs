using EasyVan.Models;

namespace EasyVan.Services;

public static class AgrupamentoParadasMotorista
{
    public static IReadOnlyList<ParadaMotoristaViewModel> Agrupar(IEnumerable<ViagemParada> paradas)
    {
        return paradas
            .Where(p => p.LatitudeEmbarque.HasValue && p.LongitudeEmbarque.HasValue)
            .GroupBy(p => new { p.LatitudeEmbarque, p.LongitudeEmbarque })
            .Select(grupo => new ParadaMotoristaViewModel
            {
                Ordem = grupo.Min(p => p.Ordem),
                Endereco = grupo.Select(p => p.EnderecoEmbarque)
                    .FirstOrDefault(endereco => !string.IsNullOrWhiteSpace(endereco)) ?? "Ponto de embarque",
                EtaPrevisto = grupo.Min(p => p.EtaPrevisto),
                Passageiros = grupo.OrderBy(p => p.Ordem)
                    .Select(p => p.Usuario?.Nome ?? "Passageiro")
                    .Distinct(StringComparer.CurrentCultureIgnoreCase)
                    .ToArray()
            })
            .OrderBy(p => p.Ordem)
            .ToArray();
    }
}

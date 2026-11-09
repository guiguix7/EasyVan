using EasyVan.Models;

namespace EasyVan.Services;

public static class AgendaUi
{
    private static readonly (DiasSemana Dia, string Nome)[] Dias =
    {
        (DiasSemana.Domingo, "Domingo"),
        (DiasSemana.Segunda, "Segunda-feira"),
        (DiasSemana.Terca, "Terça-feira"),
        (DiasSemana.Quarta, "Quarta-feira"),
        (DiasSemana.Quinta, "Quinta-feira"),
        (DiasSemana.Sexta, "Sexta-feira"),
        (DiasSemana.Sabado, "Sábado")
    };

    public static string FormatarDias(DiasSemana dias)
    {
        var selecionados = Dias.Where(d => dias.HasFlag(d.Dia)).Select(d => d.Nome).ToList();
        return selecionados.Count == 5
            && selecionados.Contains("Segunda-feira")
            && selecionados.Contains("Sexta-feira")
            ? "Segunda a sexta"
            : string.Join(", ", selecionados);
    }
}

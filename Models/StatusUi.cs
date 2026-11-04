namespace EasyVan.Models;

/// <summary>
/// Centraliza o rótulo e a variante visual de estados exibidos na interface.
/// Status nunca é comunicado apenas por cor: o texto acompanha o marcador.
/// </summary>
public static class StatusUi
{
    public static string Label(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return "Não informado";
        }

        return valor.Trim() switch
        {
            var v when Igual(v, "EmAndamento") => "Em andamento",
            var v when Igual(v, "Operacional") => "Operacional",
            var v when Igual(v, "Manutencao") || Igual(v, "Manutenção") => "Manutenção",
            var v when Igual(v, "Agendada") => "Agendada",
            var v when Igual(v, "Finalizada") => "Finalizada",
            var v when Igual(v, "Cancelada") => "Cancelada",
            var v when Igual(v, "Ativo") => "Ativo",
            var v when Igual(v, "Inativo") => "Inativo",
            var v when Igual(v, "Ativa") => "Ativa",
            var v when Igual(v, "Inativa") => "Inativa",
            _ => valor.Trim()
        };
    }

    public static string CssClass(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return "ev-badge--neutral";
        }

        return valor.Trim() switch
        {
            var v when Igual(v, "Operacional") || Igual(v, "Ativo") || Igual(v, "Ativa") || Igual(v, "Finalizada") || Igual(v, "Presente") => "ev-badge--success",
            var v when Igual(v, "Manutencao") || Igual(v, "Manutenção") || Igual(v, "EmAndamento") => "ev-badge--warning",
            var v when Igual(v, "Agendada") => "ev-badge--info",
            var v when Igual(v, "Cancelada") || Igual(v, "Inativo") || Igual(v, "Inativa") || Igual(v, "Ausente") => "ev-badge--neutral",
            var v when Igual(v, "Administrador") => "ev-badge--brand",
            var v when Igual(v, "Motorista") => "ev-badge--info",
            _ => "ev-badge--neutral"
        };
    }

    private static bool Igual(string valor, string esperado) =>
        string.Equals(valor, esperado, StringComparison.OrdinalIgnoreCase);
}

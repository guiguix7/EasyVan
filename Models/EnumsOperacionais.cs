namespace EasyVan.Models
{
    [Flags]
    public enum DiasSemana
    {
        Nenhum = 0,
        Domingo = 1,
        Segunda = 2,
        Terca = 4,
        Quarta = 8,
        Quinta = 16,
        Sexta = 32,
        Sabado = 64,
        DiasUteis = Segunda | Terca | Quarta | Quinta | Sexta
    }

    public enum SentidoViagem
    {
        Ida = 1,
        Volta = 2
    }

    public enum StatusPresenca
    {
        SemResposta = 0,
        Confirmado = 1,
        Ausente = 2
    }
}

using System.Data;

public class ChamadoViewModel
{
    public int Id { get; set; }
    public string? Aluno { get; set; }
    public string? Local { get; set; }
    public string? Horario { get; set; }
    public string? Motorista { get; set; }
    public string Status { get; set; } = "Aberto";
    public DateTime DataAbertura { get; set; } = DateTime.Now;
    public DateTime? DataFechamento { get; set; }
}
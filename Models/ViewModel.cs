// ViewModel para representar os dados do chamado (Model)
using System.Data;
using EasyVan.Controllers;

public class ChamadoViewModel
{
    public int Id { get; set; }

    public required UsuariosController UsuariosController { get; set; }
    public string? Aluno { get; set; }
    public string? Local { get; set; }
    public string? Horario { get; set; }
    public string? Motorista { get; set; }
    public string Status { get; set; } = "Aberto";
    public DateTime DataAbertura { get; set; } = DateTime.Now;
    public DateTime? DataFechamento { get; set; }
}
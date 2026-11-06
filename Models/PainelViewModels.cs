namespace EasyVan.Models
{
    public class PainelAlunoViewModel
    {
        public Aluno? Aluno { get; set; }
        public IReadOnlyList<PresencaViagem> Presencas { get; set; } = Array.Empty<PresencaViagem>();
        public DateTime Hoje { get; set; } = DateTime.Today;

        public bool SemVinculo => Aluno == null;
    }

    public class LinhaAlunoViagem
    {
        public int PresencaId { get; set; }
        public int AlunoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Endereco { get; set; }
        public string? Telefone { get; set; }
        public bool Presente { get; set; }
        public bool? StatusConfirmacao { get; set; }
    }

    public class ViagemDoMotorista
    {
        public Viagem Viagem { get; set; } = null!;
        public IReadOnlyList<LinhaAlunoViagem> Alunos { get; set; } = Array.Empty<LinhaAlunoViagem>();
    }

    public class PainelMotoristaViewModel
    {
        public Motorista? Motorista { get; set; }
        public IReadOnlyList<ViagemDoMotorista> Viagens { get; set; } = Array.Empty<ViagemDoMotorista>();
        public DateTime Hoje { get; set; } = DateTime.Today;

        public bool SemVinculo => Motorista == null;
    }
}

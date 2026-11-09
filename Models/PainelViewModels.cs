namespace EasyVan.Models
{
    public class PainelAlunoViewModel
    {
        public Aluno? Aluno { get; set; }
        public IReadOnlyList<PresencaViagem> Presencas { get; set; } = Array.Empty<PresencaViagem>();
        public IReadOnlyDictionary<int, PrazoPresencaViewModel> PrazosPresenca { get; set; }
            = new Dictionary<int, PrazoPresencaViewModel>();
        public DateTime Hoje { get; set; } = DateTime.Today;

        public bool SemVinculo => Aluno == null;
    }

    public sealed class PrazoPresencaViewModel
    {
        public bool PodeAlterar { get; init; }
        public DateTime? LimiteLocal { get; init; }
        public string Mensagem { get; init; } = string.Empty;
    }

    public class LinhaAlunoViagem
    {
        public int PresencaId { get; set; }
        public int AlunoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Endereco { get; set; }
        public string? Telefone { get; set; }
        public string? ParadaEndereco { get; set; }
        public int? OrdemParada { get; set; }
        public bool Presente { get; set; }
        public StatusPresenca Status { get; set; }
    }

    public class ViagemDoMotorista
    {
        public Viagem Viagem { get; set; } = null!;
        public IReadOnlyList<LinhaAlunoViagem> Alunos { get; set; } = Array.Empty<LinhaAlunoViagem>();
        public MapaRotaViewModel? Mapa { get; set; }
        public IReadOnlyList<ViagemParada> ParadasCalculadas { get; set; } = Array.Empty<ViagemParada>();
        public IReadOnlyList<TrechoGoogleMapsViewModel> Trechos { get; set; } = Array.Empty<TrechoGoogleMapsViewModel>();
        public string? AvisoRoteirizacao { get; set; }
    }

    public class PainelMotoristaViewModel
    {
        public Motorista? Motorista { get; set; }
        public IReadOnlyList<ViagemDoMotorista> Viagens { get; set; } = Array.Empty<ViagemDoMotorista>();
        public DateTime Hoje { get; set; } = DateTime.Today;

        public bool SemVinculo => Motorista == null;
    }
}

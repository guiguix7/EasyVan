using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gerenciamento_de_Van.Models;
using EasyVan.Data;
using EasyVan.Models;
using EasyVan.Services;
using Microsoft.EntityFrameworkCore;

namespace Gerenciamento_de_Van.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly CentroOperacoesService _centroOperacoes;
    private readonly PrazoPresencaService _prazoPresenca;
    private readonly RoteirizacaoService? _roteirizacao;

    public HomeController(ApplicationDbContext db, CentroOperacoesService centroOperacoes, PrazoPresencaService prazoPresenca,
        RoteirizacaoService? roteirizacao = null)
    {
        _db = db;
        _centroOperacoes = centroOperacoes;
        _prazoPresenca = prazoPresenca;
        _roteirizacao = roteirizacao;
    }

    // GET / — Login sem header. Se já estiver logado, redireciona pelo papel.
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var papel = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
            return papel switch
            {
                "Administrador" => RedirectToAction(nameof(Administrador)),
                "Motorista" => RedirectToAction(nameof(Motorista)),
                _ => RedirectToAction(nameof(Aluno))
            };
        }

        return View(new LoginViewModel());
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // Tela de "Acesso restrito" quando um não-Admin tenta página administrativa.
    public IActionResult AcessoRestrito()
    {
        return View();
    }

    // ----- Aluno -----
    [Authorize(Roles = "Aluno,Administrador")]
    public async Task<IActionResult> Aluno()
    {
        var aluno = await ObterAlunoAtualAsync();
        if (aluno == null)
            return View(new PainelAlunoViewModel());

        var inicioDoDia = DateTime.SpecifyKind(_prazoPresenca.AgoraLocal().Date, DateTimeKind.Unspecified);
        var presencas = await _db.PresencasViagem
            .Include(p => p.Viagem)!.ThenInclude(v => v!.Rota)
            .Include(p => p.Viagem)!.ThenInclude(v => v!.Van)
            .Include(p => p.Viagem)!.ThenInclude(v => v!.Motorista)
            .Where(p => p.AlunoId == aluno.Id && p.Viagem != null && p.Viagem.Status != "Cancelada")
            .ToListAsync();

        var futuras = presencas
            .Where(p => p.Viagem!.Data >= inicioDoDia)
            .OrderBy(p => p.Viagem!.Data)
            .ThenBy(p => p.Viagem!.Id);

        var passadas = presencas
            .Where(p => p.Viagem!.Data < inicioDoDia)
            .OrderByDescending(p => p.Viagem!.Data)
            .ThenByDescending(p => p.Viagem!.Id)
            .Take(8);

        var listaPresencas = futuras.Concat(passadas).ToList();
        var prazos = listaPresencas
            .Where(p => p.Viagem != null)
            .ToDictionary(p => p.ViagemId, p =>
            {
                var resultado = _prazoPresenca.Avaliar(p.Viagem!);
                return new PrazoPresencaViewModel
                {
                    PodeAlterar = resultado.PodeAlterar,
                    LimiteLocal = resultado.LimiteLocal,
                    Mensagem = resultado.Mensagem
                };
            });

        return View(new PainelAlunoViewModel
        {
            Aluno = aluno,
            Presencas = listaPresencas,
            PrazosPresenca = prazos,
            Hoje = inicioDoDia
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Aluno,Administrador")]
    public async Task<IActionResult> ConfirmarPresenca(int viagemId, bool vou)
    {
        var aluno = await ObterAlunoAtualAsync();
        if (aluno == null)
        {
            TempData["Erro"] = "Sua conta ainda não está vinculada a um cadastro de aluno.";
            return RedirectToAction(nameof(Aluno));
        }

        var presenca = await _db.PresencasViagem
            .Include(p => p.Viagem)
            .FirstOrDefaultAsync(p => p.ViagemId == viagemId && p.AlunoId == aluno.Id);

        if (presenca?.Viagem == null)
        {
            TempData["Erro"] = "Viagem não encontrada para este aluno.";
            return RedirectToAction(nameof(Aluno));
        }

        if (presenca.Viagem.Status is "Finalizada" or "Cancelada")
        {
            TempData["Erro"] = "Esta viagem já foi encerrada e não aceita confirmação.";
            return RedirectToAction(nameof(Aluno));
        }

        var prazo = _prazoPresenca.Avaliar(presenca.Viagem);
        if (!prazo.PodeAlterar)
        {
            TempData["Erro"] = prazo.Mensagem;
            return RedirectToAction(nameof(Aluno));
        }

        presenca.Status = vou ? StatusPresenca.Confirmado : StatusPresenca.Ausente;
        presenca.RespondidoEm = prazo.AgoraUtc.UtcDateTime;
        presenca.StatusConfirmacao = vou;
        presenca.ConfirmadoEm = prazo.AgoraUtc.UtcDateTime;
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = vou
            ? "Presença confirmada. Bom embarque!"
            : "Resposta registrada: você não vai nesta viagem.";
        return RedirectToAction(nameof(Aluno));
    }

    [Authorize(Roles = "Aluno,Administrador")]
    public async Task<IActionResult> Detalhes()
    {
        var aluno = await ObterAlunoAtualAsync();
        Van? van = null;
        string? motoristaNome = null;

        if (aluno != null)
        {
            var inicioDoDia = DateTime.SpecifyKind(_prazoPresenca.AgoraLocal().Date, DateTimeKind.Unspecified);
            var proximaViagem = await _db.Viagens
                .Include(v => v.Van)!.ThenInclude(van => van!.Rota)
                .Include(v => v.Motorista)
                .Where(v => v.Status != "Cancelada"
                    && v.Data >= inicioDoDia
                    && v.Presencas.Any(p => p.AlunoId == aluno.Id))
                .OrderBy(v => v.Data)
                .ThenBy(v => v.Id)
                .FirstOrDefaultAsync();

            van = proximaViagem?.Van;
            motoristaNome = proximaViagem?.Motorista?.Nome;
        }

        if (van == null)
        {
            van = await _db.Vans
                .Include(v => v.Motorista)
                .Include(v => v.Rota)
                .FirstOrDefaultAsync(v => v.Status == "Operacional")
                ?? await _db.Vans
                    .Include(v => v.Motorista)
                    .Include(v => v.Rota)
                    .FirstOrDefaultAsync();
            motoristaNome = van?.Motorista?.Nome;
        }

        if (van == null) return NotFound();

        return View("DetalhesAluno", new
        {
            Van = van,
            Rota = van.Rota,
            MotoristaNome = string.IsNullOrWhiteSpace(motoristaNome) ? van.Driver : motoristaNome
        });
    }

    // ----- Motorista -----
    [Authorize(Roles = "Motorista,Administrador")]
    public async Task<IActionResult> Motorista()
    {
        var motorista = await ObterMotoristaAtualAsync();
        if (motorista == null)
            return View(new PainelMotoristaViewModel());

        var viagens = await _db.Viagens
            .Include(v => v.Van)
            .Include(v => v.Rota)
            .Include(v => v.Linha)!.ThenInclude(l => l!.Passageiros).ThenInclude(p => p.Usuario)
            .Include(v => v.Presencas)!.ThenInclude(p => p.Aluno)
            .Where(v => v.MotoristaId == motorista.Id && v.Status != "Cancelada")
            .ToListAsync();

        var inicioDoDia = DateTime.SpecifyKind(_prazoPresenca.AgoraLocal().Date, DateTimeKind.Unspecified);
        var ordenadas = viagens
            .Where(v => v.Data >= inicioDoDia)
            .OrderBy(v => v.Data)
            .ThenBy(v => v.Id)
            .Concat(viagens
                .Where(v => v.Data < inicioDoDia)
                .OrderByDescending(v => v.Data)
                .ThenByDescending(v => v.Id)
                .Take(8))
            .ToList();

        var rotaPorViagem = new Dictionary<int, (IReadOnlyList<ViagemParada> Paradas, string? Aviso)>();
        foreach (var viagem in ordenadas.Where(v => v.LinhaId.HasValue))
        {
            string? aviso = null;
            if (_roteirizacao is not null)
                aviso = (await _roteirizacao.RecalcularSeNecessarioAsync(viagem.Id)).Aviso;
            var paradasCalculadas = await _db.ViagensParadas.Include(p => p.Usuario)
                .Where(p => p.ViagemId == viagem.Id).OrderBy(p => p.Ordem).ToListAsync();
            rotaPorViagem[viagem.Id] = (paradasCalculadas, aviso);
        }

        var rotasIds = ordenadas.Select(x => x.RotaId).Distinct().ToList();
        var vinculosPorRota = await _db.VinculosAluno
            .Where(v => v.Ativo && rotasIds.Contains(v.RotaId))
            .Include(v => v.Parada)
            .ToListAsync();

        var painel = ordenadas.Select(v => new ViagemDoMotorista
        {
            Viagem = v,
            Alunos = v.Presencas
                .Where(p => p.Aluno != null)
                .OrderBy(p => p.Aluno!.Nome)
                .Select(p =>
                {
                    var usuarioId = p.UsuarioId ?? p.Aluno!.UsuarioId;
                    var pontoLinha = usuarioId.HasValue
                        ? v.Linha?.Passageiros.FirstOrDefault(passageiro => passageiro.UsuarioId == usuarioId.Value)
                        : null;
                    var vinculo = vinculosPorRota
                        .Where(x => x.RotaId == v.RotaId && x.AlunoId == p.AlunoId
                            && x.Sentido == (v.Sentido ?? SentidoViagem.Ida) && x.Ativo && x.Parada?.Ativo == true)
                        .OrderBy(x => x.Parada!.Ordem)
                        .FirstOrDefault();
                    return new LinhaAlunoViagem
                    {
                        PresencaId = p.Id,
                        AlunoId = p.AlunoId,
                        Nome = p.Aluno!.Nome,
                        Endereco = pontoLinha?.EnderecoEmbarque ?? vinculo?.Parada?.Endereco,
                        ParadaEndereco = pontoLinha?.EnderecoEmbarque ?? vinculo?.Parada?.Endereco,
                        OrdemParada = vinculo is null ? null : vinculo.Parada?.Ordem,
                        Telefone = p.Aluno.Telefone,
                        Presente = p.Presente,
                        Status = p.Status
                    };
                })
                .ToList(),
            Mapa = v.LinhaId.HasValue || v.Rota == null ? null : new MapaRotaViewModel
            {
                Origem = v.Sentido == SentidoViagem.Volta ? v.Rota.Destino : v.Rota.Origem,
                Destino = v.Sentido == SentidoViagem.Volta ? v.Rota.Origem : v.Rota.Destino,
                Titulo = "Rota desta viagem",
                Paradas = vinculosPorRota
                    .Where(x => x.RotaId == v.RotaId && x.Sentido == (v.Sentido ?? SentidoViagem.Ida)
                        && x.Parada?.Ativo == true
                        && v.Presencas.Any(p => p.AlunoId == x.AlunoId && p.Status == StatusPresenca.Confirmado))
                    .OrderBy(x => x.Parada!.Ordem)
                    .Select(x => x.Parada!.Endereco)
                    .Distinct()
                    .ToList()
            },
            ParadasCalculadas = rotaPorViagem.GetValueOrDefault(v.Id).Paradas ?? Array.Empty<ViagemParada>(),
            ParadasAgrupadas = AgrupamentoParadasMotorista.Agrupar(
                rotaPorViagem.GetValueOrDefault(v.Id).Paradas ?? Array.Empty<ViagemParada>()),
            Trechos = v.Linha is null ? Array.Empty<TrechoGoogleMapsViewModel>()
                : GoogleMapsLinkService.CriarTrechos(v.Linha, v.Sentido ?? SentidoViagem.Ida,
                    rotaPorViagem.GetValueOrDefault(v.Id).Paradas ?? Array.Empty<ViagemParada>()),
            AvisoRoteirizacao = rotaPorViagem.GetValueOrDefault(v.Id).Aviso
        }).ToList();

        return View(new PainelMotoristaViewModel
        {
            Motorista = motorista,
            Viagens = painel,
            Hoje = _prazoPresenca.AgoraLocal().Date
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Motorista,Administrador")]
    public async Task<IActionResult> MarcarPresenca(int viagemId, int alunoId, bool presente)
    {
        var motorista = await ObterMotoristaAtualAsync();
        if (motorista == null && !User.IsInRole("Administrador"))
        {
            TempData["Erro"] = "Sua conta ainda não está vinculada a um cadastro de motorista.";
            return RedirectToAction(nameof(Motorista));
        }

        var viagem = await _db.Viagens.FirstOrDefaultAsync(v => v.Id == viagemId);
        if (viagem == null)
        {
            TempData["Erro"] = "Viagem não encontrada.";
            return RedirectToAction(nameof(Motorista));
        }

        if (!User.IsInRole("Administrador") && viagem.MotoristaId != motorista!.Id)
        {
            TempData["Erro"] = "Você só pode alterar presenças das suas próprias viagens.";
            return RedirectToAction(nameof(Motorista));
        }

        var presenca = await _db.PresencasViagem
            .FirstOrDefaultAsync(p => p.ViagemId == viagemId && p.AlunoId == alunoId);

        if (presenca == null)
        {
            TempData["Erro"] = "Presença não encontrada para esta viagem.";
            return RedirectToAction(nameof(Motorista));
        }

        presenca.Presente = presente;
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = presente ? "Presença registrada." : "Ausência registrada.";
        return RedirectToAction(nameof(Motorista));
    }

    // ----- Administrador -----
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Administrador()
    {
        var painel = await _centroOperacoes.ObterPainelAsync();
        return View(painel);
    }

    private int? ObterUsuarioId()
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    }

    private async Task<Aluno?> ObterAlunoAtualAsync()
    {
        var usuarioId = ObterUsuarioId();
        return usuarioId.HasValue
            ? await _db.Alunos.FirstOrDefaultAsync(a => a.UsuarioId == usuarioId.Value)
            : null;
    }

    private async Task<Motorista?> ObterMotoristaAtualAsync()
    {
        var usuarioId = ObterUsuarioId();
        return usuarioId.HasValue
            ? await _db.Motoristas.FirstOrDefaultAsync(m => m.UsuarioId == usuarioId.Value)
            : null;
    }

}

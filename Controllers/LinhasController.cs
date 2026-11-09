using System.Globalization;
using EasyVan.Data;
using EasyVan.Models;
using EasyVan.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Gerenciamento_de_Van.Controllers;

[Authorize(Roles = "Administrador")]
public sealed class LinhasController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly LinhaService _linhas;
    private readonly IGeocodificador? _geocodificador;

    public LinhasController(ApplicationDbContext db, LinhaService linhas, IGeocodificador? geocodificador = null)
    {
        _db = db; _linhas = linhas; _geocodificador = geocodificador;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        return View(await _linhas.ListarAsync(ct));
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var model = new LinhaFormViewModel();
        await PrepararOpcoesAsync(model, ct);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LinhaFormViewModel model, CancellationToken ct)
    {
        AplicarDias(model);
        await GeocodificarEnderecosSeNecessarioAsync(model, ct);
        if (!ModelState.IsValid)
        {
            await PrepararOpcoesAsync(model, ct);
            return View(model);
        }
        var resultado = await _linhas.SalvarAsync(model, ct);
        if (!resultado.Sucesso)
        {
            ModelState.AddModelError(string.Empty, resultado.Erro ?? "Não foi possível salvar a linha.");
            ViewData["AvisoCapacidade"] = resultado.Erro;
            await PrepararOpcoesAsync(model, ct);
            return View(model);
        }
        TempData["Sucesso"] = $"Linha {resultado.Linha!.Nome} cadastrada. As viagens serão geradas pela agenda.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var model = await _linhas.ObterFormularioAsync(id, ct);
        if (model is null) return NotFound();
        model.DiasSelecionados = Enum.GetValues<DiasSemana>().Where(d => d != DiasSemana.Nenhum &&
            d != DiasSemana.DiasUteis && (model.DiasSemana & d) != 0).Select(d => (int)d).ToList();
        await PrepararOpcoesAsync(model, ct);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LinhaFormViewModel model, CancellationToken ct)
    {
        if (id != model.Id) return NotFound();
        AplicarDias(model);
        await GeocodificarEnderecosSeNecessarioAsync(model, ct);
        if (!ModelState.IsValid)
        {
            await PrepararOpcoesAsync(model, ct);
            return View(model);
        }
        var resultado = await _linhas.SalvarAsync(model, ct);
        if (!resultado.Sucesso)
        {
            ModelState.AddModelError(string.Empty, resultado.Erro ?? "Não foi possível salvar a linha.");
            await PrepararOpcoesAsync(model, ct);
            return View(model);
        }
        TempData["Sucesso"] = "Linha atualizada. Viagens futuras ainda não concluídas serão recalculadas.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desativar(int id, CancellationToken ct)
    {
        if (!await _linhas.DesativarAsync(id, ct)) return NotFound();
        TempData["Sucesso"] = "Linha desativada e viagens futuras canceladas.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PrepararOpcoesAsync(LinhaFormViewModel model, CancellationToken ct)
    {
        var contasAlunos = await _db.Usuarios.AsNoTracking()
            .Where(u => u.Ativo && u.RoleManager == "Aluno")
            .OrderBy(u => u.Nome).ToListAsync(ct);
        var perfisAlunos = await _db.Alunos.AsNoTracking().Where(a => a.Ativo && a.UsuarioId.HasValue)
            .Select(a => new { UsuarioId = a.UsuarioId!.Value, a.Escola }).ToListAsync(ct);
        var escolaPorUsuario = perfisAlunos.ToDictionary(a => a.UsuarioId, a => a.Escola);
        ViewData["Alunos"] = contasAlunos
            .Where(u => escolaPorUsuario.ContainsKey(u.Id))
            .Select(u => new AlunoLinhaOpcaoViewModel(u.Id, u.Nome, escolaPorUsuario[u.Id], u.Endereco,
                u.Lat.HasValue && u.Lng.HasValue))
            .ToList();
        ViewData["Vans"] = await _db.Vans.AsNoTracking().Where(v => v.Status == "Operacional").OrderBy(v => v.Plate).ToListAsync(ct);
        ViewData["Motoristas"] = await _db.Motoristas.AsNoTracking().Where(m => m.Ativo).OrderBy(m => m.Nome).ToListAsync(ct);
    }

    private async Task GeocodificarEnderecosSeNecessarioAsync(LinhaFormViewModel model, CancellationToken ct)
    {
        await GeocodificarAsync(model.Destino, model.DestinoLat, model.DestinoLng,
            nameof(model.Destino), nameof(model.DestinoLat), nameof(model.DestinoLng),
            (endereco, ponto) => { model.Destino = endereco; model.DestinoLat = ponto.Lat; model.DestinoLng = ponto.Lng; }, ct);
        await GeocodificarAsync(model.Origem, model.OrigemLat, model.OrigemLng,
            nameof(model.Origem), nameof(model.OrigemLat), nameof(model.OrigemLng),
            (endereco, ponto) => { model.Origem = endereco; model.OrigemLat = ponto.Lat; model.OrigemLng = ponto.Lng; }, ct);
    }

    private async Task GeocodificarAsync(string endereco, double lat, double lng, string campoEndereco,
        string campoLat, string campoLng, Action<string, Coordenada> aplicar, CancellationToken ct)
    {
        if (double.IsFinite(lat) && double.IsFinite(lng) && (lat != 0 || lng != 0)) return;

        ModelState.Remove(campoLat);
        ModelState.Remove(campoLng);
        if (string.IsNullOrWhiteSpace(endereco))
        {
            ModelState.AddModelError(campoEndereco, "Informe um endereço completo para localizar este ponto.");
            return;
        }
        if (_geocodificador is null)
        {
            ModelState.AddModelError(campoEndereco, "Escolha o endereço no Google Maps ou configure a chave de geocodificação do servidor.");
            return;
        }

        try
        {
            var resultado = await _geocodificador.GeocodificarAsync(endereco, ct);
            aplicar(resultado.Endereco, resultado.Coordenada);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            ModelState.AddModelError(campoEndereco, "Não foi possível localizar este endereço. Confira o texto ou tente novamente.");
        }
    }

    private static void AplicarDias(LinhaFormViewModel model)
    {
        model.DiasSemana = model.DiasSelecionados.Aggregate(DiasSemana.Nenhum,
            (dias, valor) => Enum.IsDefined(typeof(DiasSemana), valor) && valor != (int)DiasSemana.DiasUteis
                ? dias | (DiasSemana)valor : dias);
    }
}

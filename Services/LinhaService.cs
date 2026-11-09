using EasyVan.Data;
using EasyVan.Models;
using Microsoft.EntityFrameworkCore;

namespace EasyVan.Services;

public sealed record ResultadoSalvarLinha(bool Sucesso, Linha? Linha, string? Erro, int Passageiros, int Capacidade);

public sealed class LinhaService
{
    private readonly ApplicationDbContext _db;

    public LinhaService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<LinhaIndexItemViewModel>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var linhas = await _db.Linhas.AsNoTracking()
            .Include(l => l.Passageiros)
            .Include(l => l.Vans).ThenInclude(v => v.Van)
            .OrderBy(l => l.Nome)
            .ToListAsync(cancellationToken);
        return linhas.Select(l => new LinhaIndexItemViewModel(
            l, l.Passageiros.Count, l.Vans.Sum(v => v.Van?.Capacity ?? 0))).ToList();
    }

    public async Task<LinhaFormViewModel?> ObterFormularioAsync(int id, CancellationToken cancellationToken = default)
    {
        var linha = await _db.Linhas.AsNoTracking()
            .Include(l => l.Passageiros)
            .Include(l => l.Vans)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (linha is null) return null;
        return new LinhaFormViewModel
        {
            Id = linha.Id, Nome = linha.Nome, Destino = linha.Destino,
            DestinoLat = linha.DestinoLat, DestinoLng = linha.DestinoLng,
            Origem = linha.Origem, OrigemLat = linha.OrigemLat, OrigemLng = linha.OrigemLng,
            DiasSemana = linha.DiasSemana, HorarioChegadaIda = linha.HorarioChegadaIda,
            HorarioSaidaVolta = linha.HorarioSaidaVolta, TemIda = linha.TemIda, TemVolta = linha.TemVolta,
            VoltaTerminaNoUltimoAluno = linha.VoltaTerminaNoUltimoAluno, FolgaMinutos = linha.FolgaMinutos,
            PassageirosIds = linha.Passageiros.Select(p => p.UsuarioId).ToList(),
            VansIds = linha.Vans.Select(v => v.VanId).ToList(),
            MotoristasPorVan = linha.Vans.ToDictionary(v => v.VanId, v => (int?)v.MotoristaId),
            VanPorPassageiro = linha.Passageiros.ToDictionary(p => p.UsuarioId, p => (int?)p.VanId),
            PontosEmbarque = linha.Passageiros.ToDictionary(p => p.UsuarioId, p => new PontoEmbarqueFormViewModel
            {
                Endereco = p.EnderecoEmbarque,
                Latitude = p.LatitudeEmbarque,
                Longitude = p.LongitudeEmbarque
            }),
            Ativa = linha.Ativa
        };
    }

    public async Task<ResultadoSalvarLinha> SalvarAsync(LinhaFormViewModel formulario, CancellationToken cancellationToken = default)
    {
        var vanIds = formulario.VansIds.Distinct().ToArray();
        var passageiroIds = formulario.PassageirosIds.Distinct().ToArray();
        var vans = await _db.Vans.Where(v => vanIds.Contains(v.Id) && v.Status == "Operacional")
            .ToDictionaryAsync(v => v.Id, cancellationToken);
        var motoristasIds = formulario.MotoristasPorVan.Values.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToArray();
        var motoristas = await _db.Motoristas.Where(m => m.Ativo && motoristasIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, cancellationToken);
        var usuarios = await _db.Usuarios.Where(u => passageiroIds.Contains(u.Id) && u.Ativo)
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        if (vanIds.Length == 0 || vans.Count != vanIds.Length)
            return Erro("Selecione ao menos uma van operacional da sua organização.", passageiroIds.Length);
        if (vanIds.Any(id => !formulario.MotoristasPorVan.TryGetValue(id, out var motoristaId)
                || !motoristaId.HasValue || !motoristas.ContainsKey(motoristaId.Value)))
            return Erro("Selecione um motorista ativo da sua organização para cada van.", passageiroIds.Length, vans.Values.Sum(v => v.Capacity));
        if (passageiroIds.Length != formulario.PassageirosIds.Count || usuarios.Count != passageiroIds.Length)
            return Erro("Há alunos duplicados, inativos ou que não pertencem à sua organização.", passageiroIds.Length, vans.Values.Sum(v => v.Capacity));
        if (usuarios.Values.Any(u => Usuarios.NormalizeRole(u.RoleManager) != "Aluno"))
            return Erro("Todos os passageiros selecionados precisam ter perfil de aluno.", passageiroIds.Length, vans.Values.Sum(v => v.Capacity));
        foreach (var usuarioId in passageiroIds)
        {
            if (!formulario.PontosEmbarque.TryGetValue(usuarioId, out var ponto)
                || string.IsNullOrWhiteSpace(ponto.Endereco))
                return Erro("Informe o endereço do ponto de embarque operacional de cada aluno selecionado.", passageiroIds.Length, vans.Values.Sum(v => v.Capacity));
            if (ponto.Latitude.HasValue != ponto.Longitude.HasValue
                || (ponto.Latitude.HasValue && (!double.IsFinite(ponto.Latitude.Value) || !double.IsFinite(ponto.Longitude!.Value)
                    || ponto.Latitude is < -90 or > 90 || ponto.Longitude is < -180 or > 180)))
                return Erro("O ponto de embarque localizado está inválido. Selecione o endereço no Google Maps novamente.", passageiroIds.Length, vans.Values.Sum(v => v.Capacity));
        }
        var usuariosAluno = await _db.Alunos.Where(a => a.Ativo && a.UsuarioId.HasValue && passageiroIds.Contains(a.UsuarioId.Value))
            .Select(a => a.UsuarioId!.Value).ToListAsync(cancellationToken);
        if (usuariosAluno.Distinct().Count() != passageiroIds.Length)
            return Erro("Cada passageiro precisa ter um cadastro operacional de aluno associado à conta.", passageiroIds.Length, vans.Values.Sum(v => v.Capacity));

        var capacidade = vans.Values.Sum(v => v.Capacity);
        if (passageiroIds.Length > capacidade)
            return Erro($"{passageiroIds.Length} alunos, capacidade total {capacidade} — adicione outra van.", passageiroIds.Length, capacidade);

        var alocacao = new Dictionary<int, int>();
        foreach (var usuarioId in passageiroIds)
        {
            int? vanId = vanIds.Length == 1 ? vanIds[0]
                : formulario.VanPorPassageiro.GetValueOrDefault(usuarioId);
            if (!vanId.HasValue || !vanIds.Contains(vanId.Value))
                return Erro("Atribua cada aluno a uma das vans selecionadas.", passageiroIds.Length, capacidade);
            alocacao[usuarioId] = vanId.Value;
        }

        foreach (var grupo in alocacao.GroupBy(x => x.Value))
            if (grupo.Count() > vans[grupo.Key].Capacity)
                return Erro($"A van {vans[grupo.Key].Plate} não comporta os alunos atribuídos. Redistribua-os.", passageiroIds.Length, capacidade);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var linha = formulario.Id > 0
            ? await _db.Linhas.Include(l => l.Vans).Include(l => l.Passageiros)
                .FirstOrDefaultAsync(l => l.Id == formulario.Id, cancellationToken)
            : null;
        if (formulario.Id > 0 && linha is null)
            return Erro("Linha não encontrada.", passageiroIds.Length, capacidade);

        var rota = linha is null
            ? new Rota { Nome = formulario.Nome.Trim(), Origem = formulario.Origem.Trim(), Destino = formulario.Destino.Trim(), Ativo = true }
            : await _db.Rotas.FirstAsync(r => r.Id == linha.RotaLegadaId, cancellationToken);
        if (linha is null)
        {
            _db.Rotas.Add(rota);
            await _db.SaveChangesAsync(cancellationToken);
            linha = new Linha { RotaLegadaId = rota.Id };
            _db.Linhas.Add(linha);
        }

        linha.Nome = formulario.Nome.Trim();
        linha.Destino = formulario.Destino.Trim();
        linha.DestinoLat = formulario.DestinoLat;
        linha.DestinoLng = formulario.DestinoLng;
        linha.Origem = formulario.Origem.Trim();
        linha.OrigemLat = formulario.OrigemLat;
        linha.OrigemLng = formulario.OrigemLng;
        linha.DiasSemana = formulario.DiasSemana;
        linha.HorarioChegadaIda = formulario.HorarioChegadaIda;
        linha.HorarioSaidaVolta = formulario.HorarioSaidaVolta;
        linha.TemIda = formulario.TemIda;
        linha.TemVolta = formulario.TemVolta;
        linha.VoltaTerminaNoUltimoAluno = formulario.VoltaTerminaNoUltimoAluno;
        linha.FolgaMinutos = formulario.FolgaMinutos;
        linha.Ativa = formulario.Ativa;
        rota.Nome = linha.Nome;
        rota.Origem = linha.Origem;
        rota.Destino = linha.Destino;
        rota.Ativo = linha.Ativa;

        _db.LinhasVans.RemoveRange(linha.Vans);
        _db.LinhasPassageiros.RemoveRange(linha.Passageiros);
        linha.Vans.Clear();
        linha.Passageiros.Clear();
        foreach (var vanId in vanIds)
            linha.Vans.Add(new LinhaVan { VanId = vanId, MotoristaId = formulario.MotoristasPorVan[vanId]!.Value });
        foreach (var (usuarioId, vanId) in alocacao)
        {
            var ponto = formulario.PontosEmbarque[usuarioId];
            linha.Passageiros.Add(new LinhaPassageiro
            {
                UsuarioId = usuarioId,
                VanId = vanId,
                EnderecoEmbarque = ponto.Endereco!.Trim(),
                LatitudeEmbarque = ponto.Latitude,
                LongitudeEmbarque = ponto.Longitude
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        var hojeLocal = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(-3).Date, DateTimeKind.Unspecified);
        var futuras = await _db.Viagens.Include(v => v.Presencas)
            .Where(v => v.LinhaId == linha.Id && v.Data >= hojeLocal
            && v.Status != "Finalizada" && v.Status != "Concluida" && v.Status != "Cancelada").ToListAsync(cancellationToken);
        var alunosPorUsuario = await _db.Alunos.Where(a => a.Ativo && a.UsuarioId.HasValue
                && passageiroIds.Contains(a.UsuarioId.Value))
            .ToDictionaryAsync(a => a.UsuarioId!.Value, a => a.Id, cancellationToken);
        foreach (var viagem in futuras)
        {
            var alocacaoVan = linha.Vans.FirstOrDefault(v => v.VanId == viagem.VanId);
            var diaSemana = (DiasSemana)(1 << (int)viagem.Data.DayOfWeek);
            var sentidoAtivo = viagem.Sentido == SentidoViagem.Volta ? linha.TemVolta : linha.TemIda;
            if (alocacaoVan is null || !sentidoAtivo || (linha.DiasSemana & diaSemana) == 0)
            {
                viagem.Status = "Cancelada";
                viagem.StatusRoteirizacao = "Cancelada";
                continue;
            }

            viagem.HashConfirmados = null;
            viagem.CalculadaEm = null;
            viagem.StatusRoteirizacao = "Planejada";
            viagem.DistanciaMetros = null;
            viagem.DuracaoSegundos = null;
            viagem.HorarioSaidaSugerido = null;
            viagem.MotoristaId = alocacaoVan.MotoristaId;

            var passageirosDaVan = linha.Passageiros.Where(p => p.VanId == viagem.VanId).ToDictionary(p => p.UsuarioId);
            var presencasRemovidas = viagem.Presencas.Where(p => !p.UsuarioId.HasValue
                || !passageirosDaVan.ContainsKey(p.UsuarioId.Value)).ToArray();
            _db.PresencasViagem.RemoveRange(presencasRemovidas);
            var existentes = viagem.Presencas.Except(presencasRemovidas).Where(p => p.UsuarioId.HasValue)
                .Select(p => p.UsuarioId!.Value).ToHashSet();
            foreach (var passageiro in passageirosDaVan.Values.Where(p => !existentes.Contains(p.UsuarioId)))
            {
                if (alunosPorUsuario.TryGetValue(passageiro.UsuarioId, out var alunoId))
                    viagem.Presencas.Add(new PresencaViagem
                    {
                        AlunoId = alunoId,
                        UsuarioId = passageiro.UsuarioId,
                        Status = StatusPresenca.SemResposta
                    });
            }
        }
        var viagensFuturasIds = futuras.Select(v => v.Id).ToArray();
        var paradasAntigas = await _db.ViagensParadas.Where(p => viagensFuturasIds.Contains(p.ViagemId)).ToListAsync(cancellationToken);
        _db.ViagensParadas.RemoveRange(paradasAntigas);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new ResultadoSalvarLinha(true, linha, null, passageiroIds.Length, capacidade);
    }

    public async Task<bool> DesativarAsync(int id, CancellationToken cancellationToken = default)
    {
        var linha = await _db.Linhas.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (linha is null) return false;
        linha.Ativa = false;
        var hoje = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(-3).Date, DateTimeKind.Unspecified);
        var futuras = await _db.Viagens.Where(v => v.LinhaId == id && v.Data >= hoje
            && v.Status != "Finalizada" && v.Status != "Concluida").ToListAsync(cancellationToken);
        foreach (var viagem in futuras)
        {
            viagem.Status = "Cancelada";
            viagem.StatusRoteirizacao = "Cancelada";
        }
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ResultadoSalvarLinha Erro(string mensagem, int passageiros, int capacidade = 0) =>
        new(false, null, mensagem, passageiros, capacidade);
}

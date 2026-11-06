using Microsoft.AspNetCore.Http;

namespace EasyVan.Services;

public interface IFotoPerfilService
{
    Task<string> SalvarAsync(IFormFile foto, CancellationToken cancellationToken = default);
    void Remover(string? nomeArquivo);
    string? ObterCaminho(string? nomeArquivo);
    string? ObterContentType(string? nomeArquivo);
}

public sealed class FotoPerfilService : IFotoPerfilService
{
    public const long TamanhoMaximo = 2 * 1024 * 1024;
    private readonly string _raiz;

    public FotoPerfilService(IWebHostEnvironment ambiente)
    {
        _raiz = Path.Combine(ambiente.ContentRootPath, "App_Data", "perfis");
        Directory.CreateDirectory(_raiz);
    }

    public async Task<string> SalvarAsync(IFormFile foto, CancellationToken cancellationToken = default)
    {
        if (foto is null || foto.Length == 0)
            throw new InvalidOperationException("Selecione uma imagem para enviar.");

        if (foto.Length > TamanhoMaximo)
            throw new InvalidOperationException("A foto deve ter no máximo 2 MB.");

        string? extensao;
        await using (var leitura = foto.OpenReadStream())
        {
            var cabecalho = new byte[12];
            var lidos = await leitura.ReadAsync(cabecalho.AsMemory(), cancellationToken);
            extensao = DetectarExtensao(cabecalho.AsSpan(0, lidos));
        }

        if (extensao is null)
            throw new InvalidOperationException("Envie uma imagem JPEG, PNG ou WebP.");

        Directory.CreateDirectory(_raiz);
        var nome = $"{Guid.NewGuid():N}{extensao}";
        var caminho = Path.Combine(_raiz, nome);

        await using var destino = new FileStream(
            caminho, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await using var origem = foto.OpenReadStream();
        await origem.CopyToAsync(destino, cancellationToken);

        return nome;
    }

    public void Remover(string? nomeArquivo)
    {
        var caminho = ObterCaminho(nomeArquivo);
        if (caminho is null) return;

        try
        {
            File.Delete(caminho);
        }
        catch (IOException)
        {
            // A foto antiga pode estar em uso; ela permanece órfã, mas o perfil não quebra.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public string? ObterCaminho(string? nomeArquivo)
    {
        if (string.IsNullOrWhiteSpace(nomeArquivo)) return null;

        var arquivo = Path.GetFileName(nomeArquivo);
        if (string.IsNullOrWhiteSpace(arquivo)) return null;

        var caminho = Path.GetFullPath(Path.Combine(_raiz, arquivo));
        var prefixoRaiz = Path.GetFullPath(_raiz) + Path.DirectorySeparatorChar;
        if (!caminho.StartsWith(prefixoRaiz, StringComparison.OrdinalIgnoreCase))
            return null;

        return File.Exists(caminho) ? caminho : null;
    }

    public string? ObterContentType(string? nomeArquivo)
    {
        if (string.IsNullOrWhiteSpace(nomeArquivo)) return null;

        return Path.GetExtension(Path.GetFileName(nomeArquivo)).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => null
        };
    }

    private static string? DetectarExtensao(ReadOnlySpan<byte> cabecalho)
    {
        if (cabecalho.Length >= 3
            && cabecalho[0] == 0xFF && cabecalho[1] == 0xD8 && cabecalho[2] == 0xFF)
        {
            return ".jpg";
        }

        if (cabecalho.Length >= 8
            && cabecalho[0] == 0x89 && cabecalho[1] == 0x50 && cabecalho[2] == 0x4E && cabecalho[3] == 0x47
            && cabecalho[4] == 0x0D && cabecalho[5] == 0x0A && cabecalho[6] == 0x1A && cabecalho[7] == 0x0A)
        {
            return ".png";
        }

        if (cabecalho.Length >= 12
            && cabecalho[0] == (byte)'R' && cabecalho[1] == (byte)'I'
            && cabecalho[2] == (byte)'F' && cabecalho[3] == (byte)'F'
            && cabecalho[8] == (byte)'W' && cabecalho[9] == (byte)'E'
            && cabecalho[10] == (byte)'B' && cabecalho[11] == (byte)'P')
        {
            return ".webp";
        }

        return null;
    }
}

using System.Security.Cryptography;
using EasyVan.Services;
using Xunit;

namespace EasyVan.Tests;

public class PasswordServiceTests
{
    private readonly PasswordService _service = new();

    [Fact]
    public void Hash_GeraValorDiferenteDaSenhaEValidaSenhaCorreta()
    {
        const string senha = "Frase-senha segura 2026!";
        var hash = _service.Hash(senha);

        Assert.NotEqual(senha, hash);
        Assert.True(_service.Verificar(senha, hash));
        Assert.False(_service.Verificar("senha incorreta", hash));
    }

    [Fact]
    public void Hash_UsaSaltAleatorio()
    {
        const string senha = "Frase-senha segura 2026!";

        Assert.NotEqual(_service.Hash(senha), _service.Hash(senha));
    }

    [Fact]
    public void Verificar_AceitaHashLegadoEIndicaAtualizacao()
    {
        const string senha = "senha antiga";
        var salt = RandomNumberGenerator.GetBytes(16);
        var derived = Rfc2898DeriveBytes.Pbkdf2(
            senha, salt, 100_000, HashAlgorithmName.SHA256, 32);
        var legacy = new byte[49];
        legacy[0] = 0x01;
        Buffer.BlockCopy(salt, 0, legacy, 1, 16);
        Buffer.BlockCopy(derived, 0, legacy, 17, 32);
        var encoded = Convert.ToBase64String(legacy);

        Assert.True(_service.Verificar(senha, encoded));
        Assert.True(_service.PrecisaAtualizar(encoded));
    }

    [Fact]
    public void HashNovo_NaoPrecisaAtualizacao()
    {
        var hash = _service.Hash("Frase-senha segura 2026!");

        Assert.False(_service.PrecisaAtualizar(hash));
    }
}
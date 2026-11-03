using System.Security.Cryptography;

namespace EasyVan.Services
{
    /// <summary>
    /// Serviço de hash de senhas usando PBKDF2 (HMACSHA256).
    /// Evita armazenar ou comparar senhas em texto simples, conforme exigido pelo AGENTS.md.
    /// </summary>
    public interface IPasswordService
    {
        string Hash(string senha);
        bool Verificar(string senha, string hash);
    }

    public class PasswordService : IPasswordService
    {
        private const int TamanhoSal = 16;
        private const int TamanhoChave = 32;
        private const int Iteracoes = 100_000;

        public string Hash(string senha)
        {
            var sal = RandomNumberGenerator.GetBytes(TamanhoSal);
            var chave = Rfc2898DeriveBytes.Pbkdf2(
                senha,
                sal,
                Iteracoes,
                HashAlgorithmName.SHA256,
                TamanhoChave);

            var bytes = new byte[1 + TamanhoSal + TamanhoChave];
            bytes[0] = 0x01; // versão do formato
            Buffer.BlockCopy(sal, 0, bytes, 1, TamanhoSal);
            Buffer.BlockCopy(chave, 0, bytes, 1 + TamanhoSal, TamanhoChave);

            return Convert.ToBase64String(bytes);
        }

        public bool Verificar(string senha, string hash)
        {
            if (string.IsNullOrEmpty(hash))
            {
                return false;
            }

            try
            {
                var bytes = Convert.FromBase64String(hash);
                if (bytes.Length != 1 + TamanhoSal + TamanhoChave || bytes[0] != 0x01)
                {
                    return false;
                }

                var sal = new byte[TamanhoSal];
                var chaveArmazenada = new byte[TamanhoChave];
                Buffer.BlockCopy(bytes, 1, sal, 0, TamanhoSal);
                Buffer.BlockCopy(bytes, 1 + TamanhoSal, chaveArmazenada, 0, TamanhoChave);

                var chave = Rfc2898DeriveBytes.Pbkdf2(
                    senha,
                    sal,
                    Iteracoes,
                    HashAlgorithmName.SHA256,
                    TamanhoChave);

                return CryptographicOperations.FixedTimeEquals(chave, chaveArmazenada);
            }
            catch
            {
                return false;
            }
        }
    }
}

using System.Buffers.Binary;
using System.Security.Cryptography;

namespace EasyVan.Services
{
    /// <summary>
    /// Hash de senha PBKDF2 versionado. O formato legado (v1/100.000 iterações)
    /// continua verificável e é atualizado automaticamente após login válido.
    /// </summary>
    public interface IPasswordService
    {
        string Hash(string senha);
        bool Verificar(string senha, string hash);
        bool PrecisaAtualizar(string hash);
    }

    public class PasswordService : IPasswordService
    {
        private const int TamanhoSal = 16;
        private const int TamanhoChave = 32;
        private const int IteracoesLegadas = 100_000;
        private const int IteracoesAtuais = 600_000;
        private const byte VersaoLegada = 0x01;
        private const byte VersaoAtual = 0x02;

        public string Hash(string senha)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(senha);
            var sal = RandomNumberGenerator.GetBytes(TamanhoSal);
            var chave = Rfc2898DeriveBytes.Pbkdf2(
                senha, sal, IteracoesAtuais, HashAlgorithmName.SHA256, TamanhoChave);

            // v2: versão (1) + iterações (4, big-endian) + salt (16) + hash (32).
            var bytes = new byte[1 + 4 + TamanhoSal + TamanhoChave];
            bytes[0] = VersaoAtual;
            BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(1, 4), IteracoesAtuais);
            Buffer.BlockCopy(sal, 0, bytes, 5, TamanhoSal);
            Buffer.BlockCopy(chave, 0, bytes, 5 + TamanhoSal, TamanhoChave);
            return Convert.ToBase64String(bytes);
        }

        public bool Verificar(string senha, string hash)
        {
            if (string.IsNullOrEmpty(senha) || string.IsNullOrEmpty(hash)) return false;

            try
            {
                var bytes = Convert.FromBase64String(hash);
                int iteracoes;
                int offsetSal;

                if (bytes.Length == 1 + TamanhoSal + TamanhoChave && bytes[0] == VersaoLegada)
                {
                    iteracoes = IteracoesLegadas;
                    offsetSal = 1;
                }
                else if (bytes.Length == 1 + 4 + TamanhoSal + TamanhoChave && bytes[0] == VersaoAtual)
                {
                    iteracoes = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(1, 4));
                    if (iteracoes < IteracoesLegadas || iteracoes > 2_000_000) return false;
                    offsetSal = 5;
                }
                else return false;

                var sal = bytes.AsSpan(offsetSal, TamanhoSal).ToArray();
                var chaveArmazenada = bytes.AsSpan(offsetSal + TamanhoSal, TamanhoChave).ToArray();
                var chave = Rfc2898DeriveBytes.Pbkdf2(
                    senha, sal, iteracoes, HashAlgorithmName.SHA256, TamanhoChave);
                return CryptographicOperations.FixedTimeEquals(chave, chaveArmazenada);
            }
            catch (FormatException) { return false; }
            catch (ArgumentException) { return false; }
        }

        public bool PrecisaAtualizar(string hash)
        {
            try
            {
                var bytes = Convert.FromBase64String(hash);
                if (bytes.Length != 1 + 4 + TamanhoSal + TamanhoChave || bytes[0] != VersaoAtual)
                    return true;

                return BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(1, 4)) < IteracoesAtuais;
            }
            catch { return true; }
        }
    }
}
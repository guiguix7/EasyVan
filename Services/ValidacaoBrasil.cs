using System.Globalization;
using System.Text;

namespace EasyVan.Services
{
    /// <summary>
    /// Validações e normalizações de dados brasileiros usadas no servidor.
    /// O JavaScript apenas melhora a experiência; a regra de negócio fica aqui.
    /// </summary>
    public static class ValidacaoBrasil
    {
        private static readonly HashSet<string> DddsValidos = new(StringComparer.Ordinal)
        {
            "11", "12", "13", "14", "15", "16", "17", "18", "19",
            "21", "22", "24", "27", "28",
            "31", "32", "33", "34", "35", "37", "38",
            "41", "42", "43", "44", "45", "46", "47", "48", "49",
            "51", "53", "54", "55",
            "61", "62", "63", "64", "65", "66", "67", "68", "69",
            "71", "73", "74", "75", "77", "79",
            "81", "82", "83", "84", "85", "86", "87", "88", "89",
            "91", "92", "93", "94", "95", "96", "97", "98", "99"
        };

        public static string SomenteDigitos(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return string.Empty;
            }

            var digitos = new StringBuilder(valor.Length);
            foreach (var caractere in valor)
            {
                if (char.IsDigit(caractere))
                {
                    digitos.Append(caractere);
                }
            }

            return digitos.ToString();
        }

        public static bool TelefoneValido(string? telefone)
        {
            if (string.IsNullOrWhiteSpace(telefone))
            {
                return true;
            }

            var digitos = SomenteDigitos(telefone);
            if (digitos.Length is not (10 or 11))
            {
                return false;
            }

            if (!DddsValidos.Contains(digitos[..2]))
            {
                return false;
            }

            // Celular brasileiro: 11 dígitos e nono dígito 9.
            if (digitos.Length == 11)
            {
                return digitos[2] == '9';
            }

            // Fixo: 10 dígitos e primeiro dígito do assinante entre 2 e 5.
            return digitos[2] is >= '2' and <= '5';
        }

        public static string NormalizarTelefone(string? telefone)
        {
            var digitos = SomenteDigitos(telefone);
            return digitos.Length is 10 or 11 ? digitos : (telefone ?? string.Empty).Trim();
        }

        public static string FormatarTelefone(string? telefone)
        {
            var digitos = SomenteDigitos(telefone);
            if (digitos.Length == 11)
            {
                return $"({digitos[..2]}) {digitos[2..7]}-{digitos[7..]}";
            }

            if (digitos.Length == 10)
            {
                return $"({digitos[..2]}) {digitos[2..6]}-{digitos[6..]}";
            }

            return (telefone ?? string.Empty).Trim();
        }

        /// <summary>
        /// Valida os dois dígitos verificadores da CNH (algoritmo oficial).
        /// Retorna true para vazio, pois a CNH é opcional em alguns cadastros legados.
        /// </summary>
        public static bool CnhValida(string? cnh)
        {
            if (string.IsNullOrWhiteSpace(cnh))
            {
                return true;
            }

            var digitos = SomenteDigitos(cnh);
            if (digitos.Length != 11 || digitos.Distinct().Count() == 1)
            {
                return false;
            }

            var baseCnh = digitos[..9];
            var primeiro = CalcularDigitoCnh(baseCnh, 9);
            var segundo = CalcularDigitoCnh(baseCnh, 1);

            return digitos[9] == (char)('0' + primeiro)
                && digitos[10] == (char)('0' + segundo);
        }

        private static int CalcularDigitoCnh(string baseCnh, int pesoInicial)
        {
            var soma = 0;
            var peso = pesoInicial;
            foreach (var caractere in baseCnh)
            {
                soma += (caractere - '0') * peso;
                peso--;
            }

            var resto = soma % 11;
            var digito = resto == 0 ? 0 : 11 - resto;
            return digito >= 10 ? 0 : digito;
        }

        public static string NormalizarCnh(string? cnh) => SomenteDigitos(cnh);

        public static bool NomeValido(string? nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                return false;
            }

            var limpo = nome.Trim();
            return limpo.Length is >= 3 and <= 120
                && limpo.Any(char.IsLetter)
                && !limpo.Any(char.IsControl);
        }

        public static string LimparTexto(string? texto, int tamanhoMaximo = 200)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return string.Empty;
            }

            var limpo = new string(texto.Trim().Where(c => !char.IsControl(c) || c is '\r' or '\n').ToArray());
            return limpo.Length <= tamanhoMaximo ? limpo : limpo[..tamanhoMaximo];
        }

        public static bool HorarioValido(string? horario, out TimeSpan valor)
        {
            valor = default;
            return !string.IsNullOrWhiteSpace(horario)
                && TimeSpan.TryParseExact(horario.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out valor)
                && valor >= TimeSpan.Zero
                && valor < TimeSpan.FromDays(1);
        }
    }
}

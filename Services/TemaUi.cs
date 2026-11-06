using Microsoft.AspNetCore.Http;

namespace EasyVan.Services
{
    public static class TemaUi
    {
        public const string Cookie = "EasyVan.Tema";
        public const string Claro = "claro";
        public const string Escuro = "escuro";

        public static string Normalizar(string? tema)
        {
            return string.Equals(tema, Escuro, StringComparison.OrdinalIgnoreCase) ? Escuro : Claro;
        }

        public static void Gravar(HttpResponse resposta, string? tema, bool seguro)
        {
            resposta.Cookies.Append(Cookie, Normalizar(tema), new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = seguro,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
        }
    }
}

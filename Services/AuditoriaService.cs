using EasyVan.Data;
using EasyVan.Models;

namespace EasyVan.Services
{
    /// <summary>
    /// Implementação do serviço de auditoria.
    /// Registra operações em LogOperacao de forma consistente com as operações correspondentes.
    /// </summary>
    public class AuditoriaService : IAuditoriaService
    {
        private readonly ApplicationDbContext _db;

        public AuditoriaService(ApplicationDbContext db)
        {
            _db = db;
        }

        public void Registrar(
            int? usuarioId,
            string? usuarioNome,
            string tipoOperacao,
            string entidade,
            int? entidadeId,
            string? descricao,
            string? informacoesComplementares = null)
        {
            var log = new LogOperacao
            {
                UsuarioId = usuarioId,
                UsuarioNome = usuarioNome,
                TipoOperacao = tipoOperacao,
                Entidade = entidade,
                EntidadeId = entidadeId,
                Descricao = descricao,
                InformacoesComplementares = informacoesComplementares,
                DataOperacao = DateTime.UtcNow
            };

            _db.LogOperacao.Add(log);
        }
    }
}
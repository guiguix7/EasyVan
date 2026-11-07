using EasyVan.Models;

namespace EasyVan.Services
{
    /// <summary>
    /// Serviço de auditoria para registrar operações relevantes no sistema.
    /// </summary>
    public interface IAuditoriaService
    {
        void Registrar(
            int? usuarioId,
            string? usuarioNome,
            string tipoOperacao,
            string entidade,
            int? entidadeId,
            string? descricao,
            string? informacoesComplementares = null);
    }
}
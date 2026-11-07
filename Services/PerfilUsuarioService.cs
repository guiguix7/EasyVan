using EasyVan.Data;
using EasyVan.Models;
using Microsoft.EntityFrameworkCore;

namespace EasyVan.Services
{
    public sealed record DadosPerfilUsuario(string? Telefone, string? Endereco, string? Cnh, bool Ativo);

    public sealed record ResultadoPerfilUsuario(bool Sucesso, string? Erro)
    {
        public static ResultadoPerfilUsuario Ok() => new(true, null);
        public static ResultadoPerfilUsuario Falha(string erro) => new(false, erro);
    }

    /// <summary>
    /// Mantém a conta de acesso e o perfil operacional (Aluno ou Motorista) como a mesma pessoa.
    /// O chamador controla a transação e o SaveChanges, para que conta e perfil sejam gravados juntos.
    /// </summary>
    public class PerfilUsuarioService
    {
        private readonly ApplicationDbContext _db;

        public PerfilUsuarioService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ResultadoPerfilUsuario> GarantirPerfilAsync(
            Usuarios usuario,
            DadosPerfilUsuario dados,
            CancellationToken cancellationToken = default)
        {
            var papel = Usuarios.NormalizeRole(usuario.RoleManager);
            if (papel is not ("Aluno" or "Motorista"))
                return ResultadoPerfilUsuario.Ok();

            var email = Usuarios.NormalizeEmail(usuario.Email);
            if (string.IsNullOrWhiteSpace(email))
                return ResultadoPerfilUsuario.Falha("Informe o e-mail da pessoa.");

            var alunos = await _db.Alunos
                .Where(a => a.UsuarioId == usuario.Id || a.Email.ToLower() == email)
                .ToListAsync(cancellationToken);

            var motoristas = await _db.Motoristas
                .Where(m => m.UsuarioId == usuario.Id || m.Email.ToLower() == email)
                .ToListAsync(cancellationToken);

            if (papel == "Aluno")
            {
                if (motoristas.Count > 0)
                    return ResultadoPerfilUsuario.Falha("Este e-mail já está associado a um perfil de motorista. Revise o vínculo antes de continuar.");

                if (alunos.Count > 1)
                    return ResultadoPerfilUsuario.Falha("Há mais de um aluno com este e-mail. Revise os cadastros duplicados antes de continuar.");

                var aluno = alunos.SingleOrDefault();
                if (aluno is not null && aluno.UsuarioId.HasValue && aluno.UsuarioId != usuario.Id)
                    return ResultadoPerfilUsuario.Falha("Este aluno já está vinculado a outra conta de acesso.");

                if (aluno is null)
                {
                    aluno = new Aluno();
                    _db.Alunos.Add(aluno);
                }

                if (!aluno.UsuarioId.HasValue)
                {
                    if (usuario.Id == 0) aluno.Usuario = usuario;
                    else aluno.UsuarioId = usuario.Id;
                }

                aluno.Nome = usuario.Nome.Trim();
                aluno.Email = email;
                aluno.Telefone = dados.Telefone;
                aluno.Endereco = dados.Endereco;
                aluno.Ativo = dados.Ativo;
            }
            else
            {
                if (alunos.Count > 0)
                    return ResultadoPerfilUsuario.Falha("Este e-mail já está associado a um perfil de aluno. Revise o vínculo antes de continuar.");

                if (motoristas.Count > 1)
                    return ResultadoPerfilUsuario.Falha("Há mais de um motorista com este e-mail. Revise os cadastros duplicados antes de continuar.");

                var motorista = motoristas.SingleOrDefault();
                if (motorista is not null && motorista.UsuarioId.HasValue && motorista.UsuarioId != usuario.Id)
                    return ResultadoPerfilUsuario.Falha("Este motorista já está vinculado a outra conta de acesso.");

                if (motorista is null)
                {
                    motorista = new Motorista();
                    _db.Motoristas.Add(motorista);
                }

                if (!motorista.UsuarioId.HasValue)
                {
                    if (usuario.Id == 0) motorista.Usuario = usuario;
                    else motorista.UsuarioId = usuario.Id;
                }

                motorista.Nome = usuario.Nome.Trim();
                motorista.Email = email;
                motorista.Telefone = dados.Telefone;
                motorista.CNH = dados.Cnh;
                motorista.Ativo = dados.Ativo;
            }

            return ResultadoPerfilUsuario.Ok();
        }
    }
}

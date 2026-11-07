using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using EasyVan.Models;
using EasyVan.Services;

namespace EasyVan.Data
{
    /// <summary>
    /// Popula dados iniciais de referência quando as tabelas estão vazias.
    /// Cada entidade é verificada independentemente para não duplicar dados já existentes.
    /// </summary>
    public static class DbInitializer
    {
        public static void Initialize(
            ApplicationDbContext context,
            IPasswordService? senha = null,
            bool seedDemoUsers = true,
            string? adminEmail = null,
            string? adminPassword = null)
        {
            var hasher = senha ?? new PasswordService();

            // Vans (referência da spec de filtros e busca).
            if (seedDemoUsers && !context.Vans.Any())
            {
                context.Vans.AddRange(
                    new Van
                    {
                        Plate = "ABC-1234",
                        Driver = "João Silva",
                        Capacity = 12,
                        Route = "Centro - Universidade",
                        Schedule = "07:00 / 17:30",
                        Status = "Operacional",
                        Description = "Van para transporte de alunos da universidade."
                    },
                    new Van
                    {
                        Plate = "DEF-5678",
                        Driver = "Maria Oliveira",
                        Capacity = 10,
                        Route = "Zona Leste - Centro",
                        Schedule = "06:30 / 18:00",
                        Status = "Manutenção",
                        Description = "Van em manutenção preventiva. Retorno previsto em 3 dias."
                    },
                    new Van
                    {
                        Plate = "TUF-6769",
                        Driver = "Aurudo da Silva",
                        Capacity = 15,
                        Route = "Centro - Distrito Industrial",
                        Schedule = "08:00 / 19:00",
                        Status = "Operacional",
                        Description = "Van de maior capacidade para trajetos longos."
                    }
                );
            }

            // Contas padrão de demonstração somente em Development.
            if (seedDemoUsers && !context.Usuarios.Any())
            {
                var demoHash = hasher.Hash("123456");
                context.Usuarios.AddRange(
                    new Usuarios
                    {
                        Nome = "Administrador Demo",
                        Email = "admin@easyvan.com",
                        RoleManager = "Administrador",
                        PasswordHasher = demoHash
                    },
                    new Usuarios
                    {
                        Nome = "Motorista Demo",
                        Email = "motorista@easyvan.com",
                        RoleManager = "Motorista",
                        PasswordHasher = demoHash
                    },
                    new Usuarios
                    {
                        Nome = "Aluno Demo",
                        Email = "aluno@easyvan.com",
                        RoleManager = "Aluno",
                        PasswordHasher = demoHash
                    }
                );
            }

            // Conta de administrador solicitada (somente Development).
            // Verifica por e-mail específico para não depender do banco estar vazio.
            // Carrega os e-mails para memória porque Usuarios.NormalizeEmail não é traduzível para SQL.
            var emailsExistentes = context.Usuarios.Select(u => u.Email).ToList();
            if (seedDemoUsers && !emailsExistentes.Any(e => Usuarios.NormalizeEmail(e) == Usuarios.NormalizeEmail("email@email.com")))
            {
                var demoHash = hasher.Hash("123456");
                context.Usuarios.Add(new Usuarios
                {
                    Nome = "Administrador",
                    Email = Usuarios.NormalizeEmail("email@email.com"),
                    RoleManager = "Administrador",
                    PasswordHasher = demoHash
                });
                context.SaveChanges();
            }

            // Em produção, cria o primeiro administrador apenas com credenciais fornecidas
            // por variáveis de ambiente privadas (nunca usa senha padrão).
            if (!seedDemoUsers && !context.Usuarios.Any())
            {
                if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
                    throw new InvalidOperationException("Configure EASYVAN_ADMIN_EMAIL e EASYVAN_ADMIN_PASSWORD nas variáveis privadas da Railway para criar o primeiro administrador.");

                if (adminPassword.Length < 6 || adminPassword.Length > 20)
                    throw new InvalidOperationException("EASYVAN_ADMIN_PASSWORD deve ter entre 6 e 20 caracteres.");

                context.Usuarios.Add(new Usuarios
                {
                    Nome = "Administrador",
                    Email = Usuarios.NormalizeEmail(adminEmail),
                    RoleManager = "Administrador",
                    PasswordHasher = hasher.Hash(adminPassword)
                });
            }

            // Motoristas.
            if (seedDemoUsers && !context.Motoristas.Any())
            {
                context.Motoristas.AddRange(
                    new Motorista
                    {
                        Nome = "João Silva",
                        Email = "joao.silva@easyvan.com",
                        Telefone = "(11) 99999-0001",
                        CNH = "12345678901",
                        Ativo = true
                    },
                    new Motorista
                    {
                        Nome = "Maria Oliveira",
                        Email = "maria.oliveira@easyvan.com",
                        Telefone = "(11) 99999-0002",
                        CNH = "23456789012",
                        Ativo = true
                    },
                    new Motorista
                    {
                        Nome = "Aurudo da Silva",
                        Email = "aurudo.silva@easyvan.com",
                        Telefone = "(11) 99999-0003",
                        CNH = "34567890123",
                        Ativo = true
                    }
                );
            }

            // Alunos.
            if (seedDemoUsers && !context.Alunos.Any())
            {
                context.Alunos.AddRange(
                    new Aluno
                    {
                        Nome = "Ana Pereira",
                        Email = "ana.pereira@email.com",
                        Telefone = "(11) 98888-1111",
                        Endereco = "Rua das Flores, 100 - Centro",
                        Ativo = true
                    },
                    new Aluno
                    {
                        Nome = "Carlos Souza",
                        Email = "carlos.souza@email.com",
                        Telefone = "(11) 98888-2222",
                        Endereco = "Av. Paulista, 500 - Bela Vista",
                        Ativo = true
                    },
                    new Aluno
                    {
                        Nome = "Fernanda Lima",
                        Email = "fernanda.lima@email.com",
                        Telefone = "(11) 98888-3333",
                        Endereco = "Rua Augusta, 300 - Consolação",
                        Ativo = true
                    }
                );
            }

            // Rotas.
            if (seedDemoUsers && !context.Rotas.Any())
            {
                context.Rotas.AddRange(
                    new Rota
                    {
                        Nome = "Centro - Universidade",
                        Descricao = "Rota da zona central para a universidade.",
                        Origem = "Centro",
                        Destino = "Universidade",
                        Ativo = true
                    },
                    new Rota
                    {
                        Nome = "Zona Leste - Centro",
                        Descricao = "Rota da zona leste para o centro.",
                        Origem = "Zona Leste",
                        Destino = "Centro",
                        Ativo = true
                    },
                    new Rota
                    {
                        Nome = "Centro - Distrito Industrial",
                        Descricao = "Rota do centro para o distrito industrial.",
                        Origem = "Centro",
                        Destino = "Distrito Industrial",
                        Ativo = true
                    }
                );
            }

            context.SaveChanges();

            VincularPerfis(context, hasher, seedDemoUsers);
            context.SaveChanges();

            ReconciliarPerfis(context);
            VincularVans(context);
            context.SaveChanges();
        }

        /// <summary>
        /// Cria o perfil operacional que falta para contas de Aluno/Motorista já existentes.
        /// O vínculo usa exclusivamente o e-mail normalizado; nomes iguais não são usados como prova.
        /// </summary>
        private static void ReconciliarPerfis(ApplicationDbContext context)
        {
            var alunos = context.Alunos.ToList();
            var motoristas = context.Motoristas.ToList();
            var usuarios = context.Usuarios.ToList();

            foreach (var usuario in usuarios)
            {
                var papel = Usuarios.NormalizeRole(usuario.RoleManager);
                if (papel is not ("Aluno" or "Motorista"))
                    continue;

                var email = Usuarios.NormalizeEmail(usuario.Email);
                var alunosMesmoEmail = alunos
                    .Where(a => Usuarios.NormalizeEmail(a.Email) == email)
                    .ToList();
                var motoristasMesmoEmail = motoristas
                    .Where(m => Usuarios.NormalizeEmail(m.Email) == email)
                    .ToList();

                if (papel == "Aluno")
                {
                    var aluno = alunos.FirstOrDefault(a => a.UsuarioId == usuario.Id);
                    if (aluno != null)
                    {
                        aluno.Nome = usuario.Nome.Trim();
                        aluno.Email = email;
                        continue;
                    }

                    if (motoristasMesmoEmail.Count > 0)
                    {
                        Registrar(context, usuario, "PerfilNaoReconciliado",
                            "Conta de aluno possui e-mail já usado por um motorista. Revisão administrativa necessária.");
                        continue;
                    }

                    if (alunosMesmoEmail.Count > 1)
                    {
                        Registrar(context, usuario, "PerfilNaoReconciliado",
                            "Há mais de um aluno com o mesmo e-mail. Revisão administrativa necessária.");
                        continue;
                    }

                    var existente = alunosMesmoEmail.SingleOrDefault();
                    if (existente != null)
                    {
                        if (existente.UsuarioId.HasValue)
                        {
                            Registrar(context, usuario, "PerfilNaoReconciliado",
                                "O aluno com este e-mail já está vinculado a outra conta.");
                            continue;
                        }

                        existente.UsuarioId = usuario.Id;
                        existente.Nome = usuario.Nome.Trim();
                        existente.Email = email;
                        Registrar(context, usuario, "PerfilVinculado", "Aluno existente vinculado automaticamente pelo e-mail.");
                        continue;
                    }

                    context.Alunos.Add(new Aluno
                    {
                        Nome = usuario.Nome.Trim(),
                        Email = email,
                        Ativo = true,
                        UsuarioId = usuario.Id
                    });
                    alunos.Add(context.Alunos.Local.Last());
                    Registrar(context, usuario, "PerfilCriado", "Perfil de aluno criado automaticamente para a conta existente.");
                }
                else
                {
                    var motorista = motoristas.FirstOrDefault(m => m.UsuarioId == usuario.Id);
                    if (motorista != null)
                    {
                        motorista.Nome = usuario.Nome.Trim();
                        motorista.Email = email;
                        continue;
                    }

                    if (alunosMesmoEmail.Count > 0)
                    {
                        Registrar(context, usuario, "PerfilNaoReconciliado",
                            "Conta de motorista possui e-mail já usado por um aluno. Revisão administrativa necessária.");
                        continue;
                    }

                    if (motoristasMesmoEmail.Count > 1)
                    {
                        Registrar(context, usuario, "PerfilNaoReconciliado",
                            "Há mais de um motorista com o mesmo e-mail. Revisão administrativa necessária.");
                        continue;
                    }

                    var existente = motoristasMesmoEmail.SingleOrDefault();
                    if (existente != null)
                    {
                        if (existente.UsuarioId.HasValue)
                        {
                            Registrar(context, usuario, "PerfilNaoReconciliado",
                                "O motorista com este e-mail já está vinculado a outra conta.");
                            continue;
                        }

                        existente.UsuarioId = usuario.Id;
                        existente.Nome = usuario.Nome.Trim();
                        existente.Email = email;
                        Registrar(context, usuario, "PerfilVinculado", "Motorista existente vinculado automaticamente pelo e-mail.");
                        continue;
                    }

                    context.Motoristas.Add(new Motorista
                    {
                        Nome = usuario.Nome.Trim(),
                        Email = email,
                        Ativo = true,
                        UsuarioId = usuario.Id
                    });
                    motoristas.Add(context.Motoristas.Local.Last());
                    Registrar(context, usuario, "PerfilCriado", "Perfil de motorista criado automaticamente para a conta existente.");
                }
            }
        }

        /// <summary>
        /// Completa os vínculos das vans antigas quando o nome do motorista e o nome da rota
        /// identificam exatamente um registro. Casos ambíguos permanecem sem vínculo para revisão.
        /// </summary>
        private static void VincularVans(ApplicationDbContext context)
        {
            var motoristas = context.Motoristas.ToList();
            var rotas = context.Rotas.ToList();

            foreach (var van in context.Vans.ToList())
            {
                if (van.MotoristaId == null && !string.IsNullOrWhiteSpace(van.Driver))
                {
                    var candidatos = motoristas
                        .Where(m => NormalizarComparacao(m.Nome) == NormalizarComparacao(van.Driver))
                        .ToList();

                    if (candidatos.Count == 1)
                    {
                        van.MotoristaId = candidatos[0].Id;
                        van.Driver = candidatos[0].Nome;
                    }
                    else if (candidatos.Count > 1)
                    {
                        Registrar(context, null, "VanNaoVinculada",
                            $"A van {van.Plate} tem mais de um motorista com o nome informado. Revisão administrativa necessária.",
                            van.Id, "Vans");
                    }
                }

                if (van.RotaId == null && !string.IsNullOrWhiteSpace(van.Route))
                {
                    var candidatas = rotas
                        .Where(r => NormalizarComparacao(r.Nome) == NormalizarComparacao(van.Route))
                        .ToList();

                    if (candidatas.Count == 1)
                    {
                        van.RotaId = candidatas[0].Id;
                        van.Route = candidatas[0].Nome;
                    }
                    else if (candidatas.Count > 1)
                    {
                        Registrar(context, null, "VanNaoVinculada",
                            $"A van {van.Plate} tem mais de uma rota com o nome informado. Revisão administrativa necessária.",
                            van.Id, "Vans");
                    }
                }
            }
        }

        private static void Registrar(ApplicationDbContext context, Usuarios? usuario, string tipo, string descricao, int? entidadeId = null, string entidade = "Usuarios")
        {
            context.LogOperacao.Add(new LogOperacao
            {
                UsuarioId = usuario?.Id,
                UsuarioNome = "Sistema",
                TipoOperacao = tipo,
                Entidade = entidade,
                EntidadeId = entidadeId ?? usuario?.Id,
                Descricao = descricao,
                DataOperacao = DateTime.UtcNow
            });
        }

        private static string NormalizarComparacao(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            var decomposto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var construtor = new StringBuilder(decomposto.Length);
            foreach (var caractere in decomposto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
                    construtor.Append(caractere);
            }

            return construtor.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Garante que cada motorista/aluno tenha uma conta de acesso.
        /// Em desenvolvimento, cria contas de demonstração com a senha padrão; em produção,
        /// apenas vincula contas existentes com o mesmo e-mail (o administrador cria as demais pelo CRUD).
        /// </summary>
        private static void VincularPerfis(ApplicationDbContext context, IPasswordService hasher, bool seedDemoUsers)
        {
            var usuarios = context.Usuarios.ToList();
            var contasPorEmail = usuarios
                .GroupBy(u => Usuarios.NormalizeEmail(u.Email))
                .ToDictionary(g => g.Key, g => g.First());

            var motoristas = context.Motoristas.ToList();
            var alunos = context.Alunos.ToList();

            // Contas recém-criadas ainda têm Id = 0 até o próximo SaveChanges,
            // então o vínculo é controlado também por referência.
            var idsVinculados = motoristas
                .Where(m => m.UsuarioId.HasValue)
                .Select(m => m.UsuarioId!.Value)
                .Concat(alunos.Where(a => a.UsuarioId.HasValue).Select(a => a.UsuarioId!.Value))
                .ToHashSet();
            var contasVinculadas = new HashSet<Usuarios>();

            bool JaVinculada(Usuarios conta) =>
                contasVinculadas.Contains(conta) || (conta.Id != 0 && idsVinculados.Contains(conta.Id));

            string? demoHash = null;

            Usuarios? ObterOuCriarConta(string nome, string email, string papel)
            {
                var normalizado = Usuarios.NormalizeEmail(email);
                if (contasPorEmail.TryGetValue(normalizado, out var existente))
                    return existente;
                if (!seedDemoUsers)
                    return null;

                demoHash ??= hasher.Hash("123456");
                var nova = new Usuarios
                {
                    Nome = nome,
                    Email = normalizado,
                    RoleManager = papel,
                    PasswordHasher = demoHash,
                    Tema = "claro"
                };
                context.Usuarios.Add(nova);
                contasPorEmail[normalizado] = nova;
                return nova;
            }

            // As contas demo usam um e-mail genérico; associe-as aos primeiros perfis do seed.
            if (seedDemoUsers)
            {
                var contaDemoMotorista = contasPorEmail.GetValueOrDefault("motorista@easyvan.com");
                var joao = motoristas.FirstOrDefault(m =>
                    m.UsuarioId == null
                    && string.Equals(Usuarios.NormalizeEmail(m.Email), "joao.silva@easyvan.com", StringComparison.Ordinal)
                    && contaDemoMotorista != null
                    && Usuarios.NormalizeRole(contaDemoMotorista.RoleManager) == "Motorista"
                    && !JaVinculada(contaDemoMotorista));

                if (joao != null && contaDemoMotorista != null)
                {
                    joao.Usuario = contaDemoMotorista;
                    contasVinculadas.Add(contaDemoMotorista);
                }

                var contaDemoAluno = contasPorEmail.GetValueOrDefault("aluno@easyvan.com");
                var ana = alunos.FirstOrDefault(a =>
                    a.UsuarioId == null
                    && string.Equals(Usuarios.NormalizeEmail(a.Email), "ana.pereira@email.com", StringComparison.Ordinal)
                    && contaDemoAluno != null
                    && Usuarios.NormalizeRole(contaDemoAluno.RoleManager) == "Aluno"
                    && !JaVinculada(contaDemoAluno));

                if (ana != null && contaDemoAluno != null)
                {
                    ana.Usuario = contaDemoAluno;
                    contasVinculadas.Add(contaDemoAluno);
                }
            }

            foreach (var motorista in motoristas.Where(m => m.UsuarioId == null))
            {
                var conta = ObterOuCriarConta(motorista.Nome, motorista.Email, "Motorista");
                if (conta == null
                    || Usuarios.NormalizeRole(conta.RoleManager) != "Motorista"
                    || JaVinculada(conta))
                    continue;

                motorista.Usuario = conta;
                contasVinculadas.Add(conta);
            }

            foreach (var aluno in alunos.Where(a => a.UsuarioId == null))
            {
                var conta = ObterOuCriarConta(aluno.Nome, aluno.Email, "Aluno");
                if (conta == null
                    || Usuarios.NormalizeRole(conta.RoleManager) != "Aluno"
                    || JaVinculada(conta))
                    continue;

                aluno.Usuario = conta;
                contasVinculadas.Add(conta);
            }
        }
    }
}

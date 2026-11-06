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
                        Id = 1,
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
                        Id = 2,
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
                        Id = 3,
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

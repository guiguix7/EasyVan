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
        public static void Initialize(ApplicationDbContext context, IPasswordService? senha = null)
        {
            var hasher = senha ?? new PasswordService();
            var hash = hasher.Hash("123456");

            // Vans (referência da spec de filtros e busca).
            if (!context.Vans.Any())
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

            // Usuários de demonstração (senha: 123456).
            if (!context.Usuarios.Any())
            {
                context.Usuarios.AddRange(
                    new Usuarios
                    {
                        Nome = "Administrador Demo",
                        Email = "admin@easyvan.com",
                        RoleManager = "Administrador",
                        PasswordHasher = hash
                    },
                    new Usuarios
                    {
                        Nome = "Motorista Demo",
                        Email = "motorista@easyvan.com",
                        RoleManager = "Motorista",
                        PasswordHasher = hash
                    },
                    new Usuarios
                    {
                        Nome = "Aluno Demo",
                        Email = "aluno@easyvan.com",
                        RoleManager = "Aluno",
                        PasswordHasher = hash
                    }
                );
            }

            // Motoristas.
            if (!context.Motoristas.Any())
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
            if (!context.Alunos.Any())
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
            if (!context.Rotas.Any())
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
        }
    }
}

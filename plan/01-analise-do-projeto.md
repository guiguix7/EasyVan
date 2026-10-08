# 01 — Análise do projeto EasyVan

> Documento gerado com auxílio de IA como etapa inicial da feature **Filtros e Busca**.
> Nenhum arquivo do projeto foi alterado durante esta análise.

## 1. Visão geral

EasyVan é um sistema web **ASP.NET Core MVC (.NET 10)** para gerenciar o transporte por vans
(rotas, horários, passageiros e motoristas). Não é uma SPA: não há front-end separado.
O "front-end" são **Razor Views** com Bootstrap 5 e jQuery; o "back-end" são **Controllers C#**.

## 2. Tecnologias (confirmadas no código)

| Camada | Tecnologia | Onde |
|---|---|---|
| Framework | ASP.NET Core MVC, `net10.0`, `Nullable` e `ImplicitUsings` habilitados | `Gerenciamento de Van.csproj` |
| ORM / banco | EF Core **8.0.0** + SQLite (`easyvan.db`), criado com `EnsureCreated()` (sem migrations) | `program.cs`, `Data/ApplicationDbContext.cs` |
| Views | Razor (`.cshtml`), Bootstrap 5, jQuery, jQuery Validation | `Views/`, `wwwroot/lib` |
| CSS | `wwwroot/css/site.css` (+ utilitários Bootstrap) | `wwwroot/css/` |

## 3. Estrutura de pastas

```
Controllers/   HomeController, UserControler.cs (UsuariosController), VansController
Models/        Usuarios, Van, LoginViewModel, UserLogin, ViewModel (ChamadoViewModel), ErrorViewModel
Data/          ApplicationDbContext (DbSet<Usuarios>, DbSet<Van>)
Views/         Home, Usuarios, Vans, Shared (_Layout)
wwwroot/       css, js, lib, images
API/, Backend/ PLACEHOLDERS (arquivos vazios / só comentário) — não participam da aplicação .NET
```

Observações relevantes:

- **Não existem** as pastas `/plan` e `/spec` (foram criadas nesta atividade) e **não existe projeto de testes**.
- **Não existe camada de Services/Repositories.** Os controllers falam direto com o `DbContext` (Usuários) ou com uma lista estática (Vans).
- Namespaces são mistos: `EasyVan.Models`, `EasyVan.Data`, `EasyVan.Controllers` (Usuários) e `Gerenciamento_de_Van.Controllers` (Home, Vans). A feature segue o que o `VansController` já usa.
- Nomes de propriedades da entidade `Van` estão em inglês (`Plate`, `Driver`...), enquanto textos de interface e mensagens de validação estão em português.

## 4. Funcionalidades já implementadas

- Login (`Home/Index` → `UsuariosController.Login`), redireciona por perfil (Aluno/Motorista/Administrador).
- Cadastro de usuário (`Home/Pages/cadastro`) e CRUD de Usuários com EF Core/SQLite.
- Listagem e detalhes de **Vans** (`Vans/Index`, `Vans/Details`), com dados **em memória**.
- Páginas institucionais (`Detalhes`, `Privacy`) e painéis por perfil (apenas texto).

## 5. Principal funcionalidade do sistema

Gerenciar a **frota de vans** e o transporte: placa, motorista, capacidade, rota, horários e status.
A tela `Vans/Index` é a listagem central dos dados operacionais.

## 6. Entidades relacionadas à feature

| Entidade | Situação | Campos |
|---|---|---|
| `Van` | Modelo + `DbSet<Van>` + tabela `Vans` (vazia). **Controller usa lista estática em memória**, não o banco. | `Id, Plate, Driver, Capacity, Route, Schedule, Status, Description` |
| `Usuarios` | Persistida em SQLite, tem listagem (`Usuarios/Index`) | `Id, Nome, Email, RoleManager, PasswordHasher` |

Não há relacionamento (FK) entre `Van` e `Usuarios`; `Van.Driver` é texto livre.

## 7. Onde implementar Filtros e Busca

**Entidade escolhida: `Van`. Tela: `Vans/Index`.** Justificativa:

1. Vans são os "dados principais" do EasyVan (o sistema existe para gerenciar vans).
2. É a listagem com mais atributos filtráveis (status, capacidade, rota, motorista, placa).
3. `Usuarios/Index` também é uma lista, mas é administrativa, tem poucos campos filtráveis e sensíveis (e-mail, senha em texto simples), então fica como melhoria futura.

A fonte de dados de `Vans` continua sendo a lista estática do `VansController`. A migração para EF Core está prevista no README, mas **está fora do escopo** desta feature; a filtragem será escrita de modo a poder migrar depois com poucas mudanças.

## 8. Arquivos existentes que serão modificados

| Arquivo | Alteração |
|---|---|
| `Controllers/VansController.cs` | `Index` passa a receber filtros (query string), validar, filtrar e devolver um ViewModel |
| `Views/Vans/Index.cshtml` | Formulário de busca/filtros, contador de resultados, mensagem de "sem resultados", novo `@model` |
| `README.md` | Registrar a funcionalidade e a existência de `/plan` e `/spec` |

Arquivos novos: `Models/VanFiltro.cs`, `Models/VanIndexViewModel.cs`, `plan/*`, `spec/*`.

**Sem alteração:** banco de dados / `ApplicationDbContext` / `program.cs` / `_Layout` / `Van.cs`.

## 9. Riscos e lacunas identificados

- **Sem `dotnet` no ambiente de geração**: build e execução precisam ser feitos localmente pelo aluno (instruções em `03-plano-de-testes.md`).
- `Vans/Details` volta para `Index` sem manter filtros (limitação aceita; ver melhorias futuras).
- Senhas em texto simples e ausência de autenticação nas rotas já existem no projeto, mas **não fazem parte** desta feature.

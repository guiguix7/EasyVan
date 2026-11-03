# AGENTS.md — EasyVan

Instruções para agentes de IA que trabalham neste repositório. Leia antes de alterar qualquer arquivo.

## Visão geral

EasyVan é um sistema web em **ASP.NET Core MVC (.NET 10)** para gerenciar o transporte de vans: rotas, horários, passageiros e motoristas. Persistência em **SQLite via EF Core** (`easyvan.db` na raiz). Front-end com Razor Views, Bootstrap, jQuery e `wwwroot/css/site.css`.

A direção do produto é virar um **SaaS multi-cliente** (cada operador de transporte com seu ambiente). Veja o relatório de produto da equipe para o contexto completo.

Tipos de usuário: `Aluno` (padrão), `Admin` (gerenciador) e `Driver` (motorista).

## Comandos

```powershell
dotnet restore
dotnet build
dotnet run          # http://localhost:5256
dotnet test         # quando existir projeto de testes
dotnet ef migrations add <Nome>
dotnet ef database update
```

Rode `dotnet build` depois de qualquer mudança em C# ou Razor. Não entregue código que não compila.

## Estrutura

```
Controllers/        Controladores MVC
Views/              Views Razor
Models/             Modelos e ViewModels
Data/               ApplicationDbContext (EF Core)
wwwroot/            CSS, JS e bibliotecas
spec/               Especificações (ex.: filtros-e-busca.md)
plan/               Planejamentos de features
API/                Placeholder para API
Backend/JS/         Placeholder Node.js (não usar; ver "Decisões")
DataBase/           Placeholder de banco
```

## Convenções de código

- C# moderno: `nullable` habilitado, `async/await` em todo acesso ao banco, nomes de membros públicos em PascalCase.
- Nomes de domínio em português (`Usuario`, `Van`, `Rota`), como já existe no projeto. Mantenha o padrão ao criar novas entidades.
- Controllers finos: regra de negócio vai para classes de serviço quando passar de poucas linhas.
- Acesso ao banco só via `ApplicationDbContext`. Sem SQL cru, a não ser que seja indispensável e parametrizado.
- Formulários POST usam `[ValidateAntiForgeryToken]` e validação no servidor (`ModelState.IsValid`), nunca só no navegador.
- Views Razor: sem lógica pesada, textos em português, rótulos associados aos campos e mensagens de erro junto ao campo.
- Estilo visual: Bootstrap + `site.css`. Mobile first, alvos de toque de ao menos 44 px, contraste AA, status nunca só por cor.
- Sem comentários óbvios; comente apenas o porquê de decisões não triviais.

## Regras de segurança (inegociáveis)

1. **Nunca** gravar ou comparar senha em texto simples em código novo. Use ASP.NET Core Identity ou hash adequado (PBKDF2/BCrypt).
2. Não commitar segredos, chaves, connection strings de produção nem o arquivo `easyvan.db`.
3. Toda ação administrativa precisa de checagem de papel (`[Authorize(Roles = "Admin")]` ou política equivalente).
4. Coletar o mínimo de dados pessoais (LGPD). Não registrar senhas, tokens ou dados pessoais em logs.
5. Validar e sanitizar toda entrada; nunca confiar em campos ocultos ou no cliente.

## Prioridades atuais (dívida técnica)

Em ordem; não pule para features novas sem avisar a equipe:

1. Substituir o armazenamento de senhas em texto simples por hash (Identity).
2. Migrar `Van` da lista em memória para EF Core.
3. Trocar `EnsureCreated()` por migrações EF Core.
4. Implementar autenticação por sessão e níveis de acesso (Admin, Driver, Aluno).
5. Só então: rotas, horários, viagens, reservas e multi-tenancy (`TenantId` + filtro global).

## Decisões de arquitetura

- **Uma única stack (.NET).** Não adicionar código em `Backend/JS/`. Se um endpoint de API for necessário, ele vai em `API/` com ASP.NET Core.
- SQLite em desenvolvimento; PostgreSQL planejado para produção.
- Busca e filtros de vans seguem `spec/filtros-e-busca.md` (insensível a maiúsculas e acentos, filtros combináveis). Não quebre esse comportamento.

## Como trabalhar

- Faça mudanças pequenas e focadas. Uma tarefa, um conjunto coeso de arquivos.
- Antes de alterar algo, leia o código vizinho e siga o padrão existente.
- Se faltar informação (regra de negócio, campo, papel), pergunte em vez de inventar.
- Ao terminar, diga em português o que mudou, quais arquivos foram tocados e como testar.
- Não rode `git push`, não apague o banco e não faça operações destrutivas sem pedido explícito.
- Não crie dependências novas sem justificar o motivo.

## Testes

- Novos serviços e regras de filtro devem ter testes de unidade (xUnit).
- Para testes com banco, use SQLite em memória.
- Cobertura mínima esperada: o caminho feliz e ao menos um caso de erro por regra.

## Documentação e entrega acadêmica

- Mantenha o `README.md` atualizado quando mudar funcionalidades, comandos ou estrutura.
- O professor exige o **registro dos prompts de IA** usados no desenvolvimento. Mantenha `docs/prompts-ia.md` (data, objetivo, resumo do resultado) e converta para `.docx` na entrega. O comando `/registrar-prompts` ajuda nisso.
- Código gerado por IA precisa ser lido e entendido por quem for commitar.

## Idioma

Responda e escreva documentação, mensagens de commit e textos de interface em **português do Brasil**. Identificadores de código seguem o padrão de nomes já usado no projeto.
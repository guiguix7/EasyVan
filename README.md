# EasyVan

## Equipe
- Guilherme Pereira Andraz
- Davi Gato Grijó da Silva
- Victor Hugo Volpi Pereira
- Caio Candiani Regio
- Pedro Ailton dos Santos Cavalieri
- Enzo Miguel Bertoluci Ovido

## Sobre o projeto
EasyVan é um sistema web ASP.NET Core MVC para gerenciar o transporte de vans. O objetivo é centralizar rotas, horários, passageiros e motoristas em uma interface web simples e responsiva.

## Status atual
- Aplicação ASP.NET Core MVC funcionando
- Login e cadastro integrados com banco SQLite via EF Core
- `ApplicationDbContext` implementado em `Data/ApplicationDbContext.cs`
- `UsuariosController` atualizado para persistir dados no banco
- `Vans` e `Usuarios` modelos preparados para persistência
- Banco é criado automaticamente em tempo de execução com `EnsureCreated()` (desenvolvimento)

## Funcionalidades implementadas
- Login visual com campos de email e senha (view `Home/Index`)
- Cadastro de usuário via formulário (`Home/Pages/cadastro`) persistido em SQLite
- Validação de campos obrigatórios e formato de email
- CRUD de usuários via `UsuariosController` usando EF Core
- Model `Van` com CRUD básico em memória (pode ser migrado para EF Core)
- **Busca e filtros na listagem de vans** (`/Vans`): busca por placa, motorista, rota e descrição (sem diferenciar maiúsculas/acentos) e filtros por status e capacidade mínima/máxima, combináveis entre si (ver `spec/filtros-e-busca.md`)

## Funcionalidades planejadas
- Migrar todas as listas em memória para EF Core (Vans)
- Adicionar autenticação com ASP.NET Identity (hash de senhas)
- Implementar níveis de acesso (Admin / User / Driver)
- Criar interfaces administrativas para gerenciar vans e rotas

## Tecnologias
- ASP.NET Core MVC (.NET 10)
- Razor Views
- Bootstrap
- jQuery
- CSS customizado em `wwwroot/css/site.css`

## Estrutura do projeto
```
Controllers/        Controladores MVC
Views/              Views Razor
Models/             Modelos e ViewModels
wwwroot/            CSS, JS e bibliotecas
API/                Arquivos placeholder para API
Backend/JS/         Estrutura placeholder de servidor Node.js
plan/               Planejamento da feature Filtros e Busca (gerado com IA)
spec/               Especificação da feature Filtros e Busca
DataBase/           Estrutura placeholder de banco de dados
```

## Como executar (desenvolvimento)
1. Abra o terminal na pasta do projeto.
2. (Opcional) Instale pacotes front-end se houver (não obrigatório):

```powershell
npm install
```

3. Restaure pacotes .NET e compile:

```powershell
dotnet restore
dotnet build
```

4. Execute a aplicação:

```powershell
dotnet run
```

5. Acesse `http://localhost:5256`.

Notas:
- O projeto agora usa SQLite via EF Core. O arquivo do banco será criado em `easyvan.db` na raiz do projeto.
- Em desenvolvimento o banco é criado automaticamente com `EnsureCreated()`; para produção, use migrações EF Core.

## Observações
- Senhas atualmente são armazenadas em texto simples (não criptografadas). Para produção, implemente hashing (ASP.NET Identity ou BCrypt).
- Se preferir controlar o esquema do banco, remova `EnsureCreated()` e use migrações EF Core:

```powershell
dotnet ef migrations add Initial
dotnet ef database update
```

## Tipos de usuário
- `Aluno` (padrão)
- `Admin` (gerenciador)
- `Driver` (motorista)

## Próximos passos que recomendo
- Criar view e controller para cadastrar `Van` persistente via EF Core.
- Implementar hashing de senhas e autenticação com Identity.
- Gerar arquivo `.docx` com prompts de IA usados durante o desenvolvimento (conforme solicitado pelo professor).
 
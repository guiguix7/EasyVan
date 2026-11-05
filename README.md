# EasyVan

Sistema web para gestão de transporte por vans. O EasyVan centraliza vans, motoristas, alunos, rotas, viagens, presenças e contas de acesso em uma aplicação ASP.NET Core MVC.

## Estado atual

O projeto está funcional para desenvolvimento local e possui:

- login por cookie e autorização por papel;
- perfis de Administrador, Motorista e Aluno;
- CRUD de usuários, vans, motoristas, alunos e rotas;
- criação, início, finalização e acompanhamento de viagens;
- registro de presença dos alunos nas viagens;
- busca e filtros combináveis de vans por texto, status e capacidade;
- persistência em SQLite com Entity Framework Core e migrações aplicadas na inicialização;
- hash de senhas com PBKDF2 por meio de `IPasswordService`;
- interface Razor responsiva com Bootstrap, jQuery e CSS próprio;
- imagem Docker pronta para publicação em ambiente que forneça a porta 8080.

O produto ainda não implementa GPS em tempo real, reservas, pagamentos, notificações, multi-tenancy ou integração externa.

## Tecnologia

- .NET 10 e ASP.NET Core MVC;
- Razor Views;
- Entity Framework Core 8;
- SQLite;
- Bootstrap e jQuery;
- xUnit para testes automatizados;
- Docker com imagens oficiais do .NET 10.

## Pré-requisitos

- .NET SDK 10;
- Git, caso o projeto seja obtido por repositório;
- Docker opcional para execução em contêiner.

Não é necessário executar `npm install`: o projeto não possui `package.json` e não usa Node.js como parte da aplicação principal.

## Executar localmente

Na raiz do projeto:

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run
```

Acesse `http://localhost:5256`.

Na primeira inicialização, a aplicação aplica as migrações existentes no arquivo `easyvan.db` e cria os dados de referência que ainda não existirem. O banco local é um artefato de desenvolvimento e não deve ser versionado.

### Contas de demonstração

As contas abaixo são criadas somente quando `ASPNETCORE_ENVIRONMENT=Development` e o banco ainda não possui usuários:

| Perfil | E-mail | Senha |
|---|---|---|
| Administrador | `admin@easyvan.com` | `123456` |
| Motorista | `motorista@easyvan.com` | `123456` |
| Aluno | `aluno@easyvan.com` | `123456` |

Essas credenciais são exclusivas para demonstração local. Não as utilize em produção.

## Configuração

A conexão padrão é definida em `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=easyvan.db"
  }
}
```

Em produção no Railway, anexe um Volume ao serviço com Mount Path exatamente `/data`. O banco e as chaves de autenticação ficam nesse volume. A aplicação falha ao iniciar no Railway se o volume não estiver montado corretamente.\n\nEm produção, prefira variáveis de ambiente:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ConnectionStrings__DefaultConnection = "Data Source=/data/easyvan.db"
$env:EASYVAN_ADMIN_EMAIL = "admin@exemplo.com"
$env:EASYVAN_ADMIN_PASSWORD = "use-uma-senha-forte-com-12-ou-mais-caracteres"
```

Quando o banco de produção estiver vazio, `EASYVAN_ADMIN_EMAIL` e `EASYVAN_ADMIN_PASSWORD` permitem criar o primeiro administrador. A aplicação não cria senha padrão em produção.

Em produção, as chaves do cookie do ASP.NET Core Data Protection são persistidas em `/data/keys`. Esse diretório deve estar em um volume persistente quando o ambiente reiniciar contêineres.

## Docker

Construir e executar:

```powershell
docker build -t easyvan .
docker run --rm -p 8080:8080 `
  -e ASPNETCORE_ENVIRONMENT=Production `
  -e EASYVAN_ADMIN_EMAIL=admin@exemplo.com `
  -e EASYVAN_ADMIN_PASSWORD="defina-uma-senha-forte" `
  -v easyvan-data:/data `
  easyvan
```

Acesse `http://localhost:8080`. O Dockerfile configura `ASPNETCORE_URLS=http://0.0.0.0:8080`.

## Estrutura

```text
Controllers/     Fluxos MVC e regras de entrada
Data/             DbContext, inicialização e persistência
Models/           Entidades e ViewModels
Services/         Serviços de domínio, incluindo hash de senhas
Views/            Views Razor da aplicação
Migrations/       Migrações do Entity Framework Core
tests/            Projeto de testes xUnit
wwwroot/          CSS, JavaScript, imagens e bibliotecas estáticas
spec/             Especificações funcionais
plan/             Planos de implementação e testes
```

## Testes e validação

```powershell
dotnet restore
dotnet build
dotnet test
```

O conjunto atual inclui testes da normalização de papéis em `tests/EasyVan.Tests`. A compilação também valida as Views Razor.

## Segurança

- Senhas novas são armazenadas com hash PBKDF2, nunca em texto simples.
- A autenticação usa cookie HttpOnly, SameSite e expiração deslizante.
- Ações administrativas devem respeitar o papel do usuário.
- Segredos de produção devem ser fornecidos por variáveis de ambiente ou pelo provedor de hospedagem.
- Não versione `easyvan.db`, tokens, senhas ou chaves de Data Protection.

Consulte [SECURITY.md](SECURITY.md) para os controles, limitações conhecidas e responsabilidades de implantação.

## Documentação do projeto

- [Direção de produto](PRODUCT.md)
- [Sistema visual](DESIGN.md)
- [Acessibilidade](ACCESSIBILITY.md)
- [Contribuição](CONTRIBUTING.md)
- [Privacidade](PRIVACY.md)
- [Segurança](SECURITY.md)
- [Especificação de filtros](spec/filtros-e-busca.md)
- [Plano de testes](plan/03-plano-de-testes.md)

## Equipe

- Guilherme Pereira Andraz
- Davi Gato Grijó da Silva
- Victor Hugo Volpi Pereira
- Caio Candiani Regio
- Pedro Ailton dos Santos Cavalieri
- Enzo Miguel Bertoluci Ovido

## Licença

Não há uma licença open source definida neste momento. Consulte [LICENSE](LICENSE) antes de redistribuir ou reutilizar o código.

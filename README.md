# EasyVan

Sistema web para gestão de transporte por vans. O EasyVan centraliza vans, motoristas, alunos, rotas, viagens, presenças e contas de acesso em uma aplicação ASP.NET Core MVC.

## Estado atual

O projeto está funcional para desenvolvimento local e possui:

- login por cookie e autorização por papel;
- perfis de Administrador, Motorista e Aluno;
- CRUD de usuários, vans, motoristas, alunos e rotas;
- criação, início, finalização e acompanhamento de viagens;
- **ciclo de vida formal das viagens**: Planejada → Confirmada → Em andamento → Finalizada, com Cancelada;
- **validação de transições de estado** no backend, bloqueando mudanças inválidas;
- **validadores operacionais centralizados**: conflito de horário (motorista e van), capacidade excedida, viagem sem motorista, viagem sem van, dados inconsistentes;
- registro de presença dos alunos nas viagens;
- **confirmação de presença dos alunos**: "Vou"/"Não vou" nas viagens agendadas;
- **marcação de presença pelo motorista**: Presente/Ausente durante viagens em andamento;
- busca e filtros combináveis de vans por texto, status e capacidade;
- **Centro de Operações Inteligente**: dashboard do gestor com indicadores reais, alertas operacionais, ações prioritárias e atividade recente;
- **histórico de auditoria**: registro de operações relevantes (criação, atualização, mudança de estado, cancelamento, presença);
- persistência em SQLite com Entity Framework Core e migrações aplicadas na inicialização;
- hash de senhas com PBKDF2 por meio de `IPasswordService`;
- perfil de usuário com edição de nome, e-mail e foto de perfil;
- tema claro/escuro com preferência persistida;
- criação de contas restrita ao Administrador, com perfil operacional de Aluno/Motorista criado ou vinculado na mesma transação;
- interface Razor responsiva com Bootstrap, jQuery e CSS próprio;
- integração opcional com Google Maps (chave por variável de ambiente, com fallback);
- imagem Docker pronta para publicação em ambiente que forneça a porta 8080.

O produto ainda não implementa GPS em tempo real, reservas, pagamentos, notificações ou multi-tenancy.

## Centro de Operações Inteligente

O painel administrativo (`/Administrador`) é um centro de acompanhamento operacional com:

- **Visão geral**: vans totais, operacionais, em manutenção; motoristas, alunos e rotas cadastrados; viagens hoje;
- **Situação das viagens**: distribuição por estado (Planejada, Confirmada, Agendada, Em andamento, Finalizada, Cancelada);
- **Alertas operacionais**: inconsistências detectadas automaticamente (conflito de horário, capacidade excedida, viagem sem motorista/van, dados inconsistentes);
- **Ações prioritárias**: atalhos para vans em manutenção, viagens em andamento, resolver alertas, gerenciar cadastros;
- **Atividade recente**: operações registradas no histórico de auditoria com data, responsável e descrição.

Todos os indicadores utilizam dados reais do banco de dados.

## Tecnologia

- .NET 10 e ASP.NET Core MVC;
- Razor Views;
- Entity Framework Core 8;
- SQLite (desenvolvimento) / PostgreSQL (produção via Supabase);
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

As contas abaixo são criadas somente quando `ASPNETCORE_ENVIRONMENT=Development`:

| Perfil | E-mail | Senha | Condição |
|---|---|---|---|
| Administrador | `admin@easyvan.com` | `123456` | banco ainda sem usuários |
| Motorista | `motorista@easyvan.com` | `123456` | banco ainda sem usuários |
| Aluno | `aluno@easyvan.com` | `123456` | banco ainda sem usuários |
| Administrador | `email@email.com` | `123456` | criada se o e-mail ainda não existir |

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

Em produção no Railway, anexe um Volume ao serviço com Mount Path exatamente `/data`. O banco e as chaves de autenticação ficam nesse volume. A aplicação falha ao iniciar no Railway se o volume não estiver montado corretamente.

Em produção, prefira variáveis de ambiente:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ConnectionStrings__DefaultConnection = "Data Source=/data/easyvan.db"
$env:EASYVAN_ADMIN_EMAIL = "admin@exemplo.com"
$env:EASYVAN_ADMIN_PASSWORD = "use-uma-senha-forte-com-12-ou-mais-caracteres"
$env:GoogleMaps__ApiKey = "sua-chave-do-google-maps"
```

Quando o banco de produção estiver vazio, `EASYVAN_ADMIN_EMAIL` e `EASYVAN_ADMIN_PASSWORD` permitem criar o primeiro administrador. A aplicação não cria senha padrão em produção.

A variável `GoogleMaps__ApiKey` (ou `GOOGLE_MAPS_API_KEY`) ativa a integração opcional com Google Maps para visualização de trajetos. Sem a chave, o sistema exibe um fallback em texto.

Em produção, as chaves do cookie do ASP.NET Core Data Protection são persistidas em `/data/keys`. Esse diretório deve estar em um volume persistente quando o ambiente reiniciar contêineres.

## Docker

Construir e executar:

```powershell
docker build -t easyvan .
docker run --rm -p 8080:8080 `
  -e ASPNETCORE_ENVIRONMENT=Production `
  -e EASYVAN_ADMIN_EMAIL=admin@exemplo.com `
  -e EASYVAN_ADMIN_PASSWORD="defina-uma-senha-forte" `
  -e GoogleMaps__ApiKey="sua-chave-do-google-maps" `
  -v easyvan-data:/data `
  easyvan
```

Acesse `http://localhost:8080`. O Dockerfile configura `ASPNETCORE_URLS=http://0.0.0.0:8080`.

## Estrutura

```text
Controllers/     Fluxos MVC e regras de entrada
Data/            DbContext, inicialização e persistência
Models/          Entidades e ViewModels
Services/        Serviços de domínio (validação, auditoria, centro de operações, fotos, tema)
Views/           Views Razor da aplicação
Migrations/      Migrações do Entity Framework Core
tests/           Projeto de testes xUnit
wwwroot/         CSS, JavaScript, imagens e bibliotecas estáticas
spec/            Especificações funcionais
plan/            Planos de implementação e testes
```

## Testes e validação

```powershell
dotnet restore
dotnet build
dotnet test
```

O projeto inclui **89 testes automatizados** cobrindo:

- Hash de senhas PBKDF2 (legado e atual);
- Validação de senha (6 a 20 caracteres);
- Normalização de papéis de usuário;
- Regras operacionais: conflito de horário (motorista/van), capacidade excedida, viagem sem motorista/van, dados inconsistentes;
- Transições de ciclo de vida de viagens (válidas e inválidas);
- Validação de nova viagem (van/motorista/rota inexistentes, conflitos);
- Agenda de viagens: data/horário no passado, chegada anterior à partida, viagem que atravessa a meia-noite e fuso de São Paulo;
- Criação de viagem com gravação de presenças, ordenação cronológica e separação entre horário previsto e real;
- Criação/vínculo automático de perfil de Aluno/Motorista a partir da conta, incluindo conflitos de e-mail e de vínculo;
- Vans: bloqueio de placa duplicada, motorista/rota obrigatórios, modelo obrigatório no formulário, busca com um caractere e identificação por placa + modelo;
- Status da interface (rótulos e classes CSS);
- Validação brasileira de telefone e CNH.

A compilação também valida as Views Razor.

## Segurança

- Senhas novas são armazenadas com hash PBKDF2, nunca em texto simples.
- A autenticação usa cookie HttpOnly, SameSite e expiração deslizante.
- Ações administrativas requerem o papel "Administrador" no backend.
- Motoristas só podem operar suas próprias viagens.
- Alunos só podem confirmar presença em suas próprias viagens.
- Proteção antiforgery em todos os formulários POST.
- Rate limiting em endpoints de login.
- Segredos de produção devem ser fornecidos por variáveis de ambiente.
- Não versione `easyvan.db`, tokens, senhas ou chaves de Data Protection.
- Upload de fotos valida tipo (JPEG/PNG/WebP) e tamanho (máx. 2 MB) com nomes não previsíveis.

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
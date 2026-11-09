# EasyVan

Sistema web para gestÃ£o de transporte por vans. O EasyVan centraliza vans, motoristas, alunos, rotas, viagens, presenÃ§as e contas de acesso em uma aplicaÃ§Ã£o ASP.NET Core MVC.

## Estado atual

O projeto estÃ¡ funcional para desenvolvimento local e possui:

- login por cookie e autorizaÃ§Ã£o por papel;
- perfis de Administrador, Motorista e Aluno;
- CRUD administrativo de usuarios, vans, motoristas, alunos e Linhas recorrentes;
- geracao automatica de viagens a partir de Linhas, com transicoes de estado e acompanhamento;
- **ciclo de vida formal das viagens**: Planejada Ã¢â€ â€™ Confirmada Ã¢â€ â€™ Em andamento Ã¢â€ â€™ Finalizada, com Cancelada;
- **validaÃ§Ã£o de transiÃ§Ãµes de estado** no backend, bloqueando mudanÃ§as invÃ¡lidas;
- **validadores operacionais centralizados**: conflito de horÃ¡rio (motorista e van), capacidade excedida, viagem sem motorista, viagem sem van, dados inconsistentes;
- registro de presenÃ§a dos alunos nas viagens;
- **confirmaÃ§Ã£o de presenÃ§a dos alunos**: "Vou"/"NÃ£o vou" nas viagens agendadas;
- **marcaÃ§Ã£o de presenÃ§a pelo motorista**: Presente/Ausente durante viagens em andamento;
- busca e filtros combinÃ¡veis de vans por texto, status e capacidade;
- **Centro de OperaÃ§Ãµes**: dashboard do gestor com indicadores reais, alertas operacionais, aÃ§Ãµes prioritÃ¡rias e atividade recente;
- **histÃ³rico de auditoria**: registro de operaÃ§Ãµes relevantes (criaÃ§Ã£o, atualizaÃ§Ã£o, mudanÃ§a de estado, cancelamento, presenÃ§a);
- persistÃªncia com Entity Framework Core: SQLite no desenvolvimento local e PostgreSQL (Supabase) em produÃ§Ã£o;
- hash de senhas com PBKDF2 por meio de `IPasswordService`;
- perfil de usuÃ¡rio com ediÃ§Ã£o de nome, e-mail e foto de perfil;
- tema claro/escuro com preferÃªncia persistida;
- criaÃ§Ã£o de contas restrita ao Administrador, com perfil operacional de Aluno/Motorista criado ou vinculado na mesma transaÃ§Ã£o;
- modelo operacional atual: Linhas recorrentes, organizacao e presencas com estado explicito;
- gestao de Linhas com pontos operacionais de embarque e alocacao manual por van/motorista;
- GeraÃ§Ã£o diÃ¡ria idempotente de viagens e presenÃ§as para uma janela configurÃ¡vel (padrÃ£o de 14 dias Ãºteis);
- ConfirmaÃ§Ã£o ou cancelamento pelo aluno atÃ© o prazo: ida Ã s 21h do dia anterior; volta duas horas antes da saÃ­da. O servidor bloqueia respostas fora do limite.
- interface Razor responsiva com Bootstrap, jQuery e CSS prÃ³prio;
- painel do motorista agrupado por ponto, pendentes separados e links Google Maps segmentados;
- integracao Google Maps com roteirizacao no servidor e fallback aproximado sem chave;
- imagem Docker pronta para publicaÃ§Ã£o em ambiente que forneÃ§a a porta 8080.

O produto ainda nÃ£o implementa GPS em tempo real, reservas, pagamentos, notificaÃ§Ãµes ou administraÃ§Ã£o de mÃºltiplas organizaÃ§Ãµes na interface.

O schema operacional inclui `OrganizacaoId` e filtros globais por organizaÃ§Ã£o. O banco de desenvolvimento mantÃ©m o registro local existente e recebe alteraÃ§Ãµes por migrations; nÃ£o recrie o arquivo SQLite.

A geraÃ§Ã£o automÃ¡tica cria viagens para as Linhas ativas no horizonte de 14 dias Ãºteis; os prazos ficam em `PrazoPresenca`.

## Centro de OperaÃ§Ãµes

O painel administrativo (`/Administrador`) Ã© um centro de acompanhamento operacional com:

- **Visao geral**: vans, motoristas, alunos, Linhas recorrentes e viagens do dia;
- **SituaÃ§Ã£o das viagens**: distribuiÃ§Ã£o por estado (Planejada, Confirmada, Agendada, Em andamento, Finalizada, Cancelada);
- **Alertas operacionais A1–A6**: capacidade, atribuição ausente, prazo sem resposta, aluno sem ponto operacional, viagem sem confirmados e van indisponível;
- **AÃ§Ãµes prioritÃ¡rias**: atalhos para vans em manutenÃ§Ã£o, viagens em andamento, resolver alertas, gerenciar cadastros;
- **Atividade recente**: operaÃ§Ãµes registradas no histÃ³rico de auditoria com data, responsÃ¡vel e descriÃ§Ã£o.

Todos os indicadores utilizam dados reais do banco de dados.

## Tecnologia

- .NET 10 e ASP.NET Core MVC;
- Razor Views;
- Entity Framework Core 10;
- SQLite (desenvolvimento local) / PostgreSQL (produÃ§Ã£o via Supabase);
- Bootstrap e jQuery;
- xUnit para testes automatizados;
- Docker com imagens oficiais do .NET 10.

## PrÃ©-requisitos

- .NET SDK 10;
- Git, caso o projeto seja obtido por repositÃ³rio;
- Docker opcional para execuÃ§Ã£o em contÃªiner.

NÃ£o Ã© necessÃ¡rio executar `npm install`: o projeto nÃ£o possui `package.json` e nÃ£o usa Node.js como parte da aplicaÃ§Ã£o principal.

## Executar localmente

Na raiz do projeto:

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --launch-profile https
```

Acesse `https://localhost:7291` para entrar. O perfil tambÃ©m expÃµe `http://localhost:5256`, mas o cookie de autenticaÃ§Ã£o Ã© `Secure`; o login sÃ³ persiste em HTTPS.

Na primeira inicializaÃ§Ã£o local, a aplicaÃ§Ã£o aplica as migrations EF Core ao arquivo `easyvan.db` e cria dados de demonstraÃ§Ã£o. O banco local Ã© um artefato de desenvolvimento e nÃ£o deve ser versionado. Em produÃ§Ã£o PostgreSQL, o bootstrapper gera o schema com o provider Npgsql e registra o baseline das migrations histÃ³ricas.

### Contas de demonstraÃ§Ã£o

As contas abaixo sÃ£o criadas somente quando `ASPNETCORE_ENVIRONMENT=Development`:

| Perfil | E-mail | Senha | CondiÃ§Ã£o |
|---|---|---|---|
| Administrador | `admin@easyvan.com` | `123456` | banco ainda sem usuÃ¡rios |
| Motorista | `motorista@easyvan.com` | `123456` | banco ainda sem usuÃ¡rios |
| Aluno | `aluno@easyvan.com` | `123456` | banco ainda sem usuÃ¡rios |
| Administrador | `email@email.com` | `123456` | criada se o e-mail ainda nÃ£o existir |

Essas credenciais sÃ£o exclusivas para demonstraÃ§Ã£o local. NÃ£o as utilize em produÃ§Ã£o.

## ConfiguraÃ§Ã£o

A conexÃ£o padrÃ£o Ã© definida em `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=easyvan.db"
  }
}
```

Em produÃ§Ã£o no Railway, configure `ConnectionStrings__DefaultConnection` com a connection string PostgreSQL do Supabase. O volume `/data` persiste as chaves de proteÃ§Ã£o dos cookies; ele nÃ£o armazena o banco PostgreSQL.

Em produÃ§Ã£o, prefira variÃ¡veis de ambiente:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ConnectionStrings__DefaultConnection = "Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require"
$env:EASYVAN_ADMIN_EMAIL = "admin@exemplo.com"
$env:EASYVAN_ADMIN_PASSWORD = "use-uma-senha-forte-com-12-ou-mais-caracteres"
$env:GoogleMaps__ApiKey = "sua-chave-do-google-maps"
$env:GoogleMaps__BrowserApiKey = "chave-restrita-por-referenciador"
```

Quando o banco de produÃ§Ã£o estiver vazio, `EASYVAN_ADMIN_EMAIL` e `EASYVAN_ADMIN_PASSWORD` permitem criar o primeiro administrador. A aplicaÃ§Ã£o nÃ£o cria senha padrÃ£o em produÃ§Ã£o.

`GoogleMaps__ApiKey` (ou `GOOGLE_MAPS_API_KEY`) ativa o Routes API e a geocodificaÃ§Ã£o no servidor. `GoogleMaps__BrowserApiKey` (ou `GOOGLE_MAPS_BROWSER_API_KEY`) ativa o autocomplete no navegador e deve ser restrita por HTTP referrer e APIs habilitadas. Use chaves separadas para navegador e servidor. Sem a chave de servidor, o sistema calcula uma ordem aproximada localmente; sem a chave do navegador, os campos de coordenadas permanecem disponÃ­veis para preenchimento manual.

Em produÃ§Ã£o, as chaves do cookie do ASP.NET Core Data Protection sÃ£o persistidas em `/data/keys`. Esse diretÃ³rio deve estar em um volume persistente quando o ambiente reiniciar contÃªineres.

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
Data/            DbContext, inicializaÃ§Ã£o e persistÃªncia
Models/          Entidades e ViewModels
Services/        ServiÃ§os de domÃ­nio (validaÃ§Ã£o, auditoria, centro de operaÃ§Ãµes, fotos, tema)
Views/           Views Razor da aplicaÃ§Ã£o
Migrations/      MigraÃ§Ãµes do Entity Framework Core
tests/           Projeto de testes xUnit
wwwroot/         CSS, JavaScript, imagens e bibliotecas estÃ¡ticas
spec/            EspecificaÃ§Ãµes funcionais
plan/            Planos de implementaÃ§Ã£o e testes
```

## Testes e validaÃ§Ã£o

```powershell
dotnet restore
dotnet build
dotnet test
```

O projeto inclui **142 testes automatizados** cobrindo:

- Hash de senhas PBKDF2 (legado e atual);
- ValidaÃ§Ã£o de senha (6 a 20 caracteres);
- NormalizaÃ§Ã£o de papÃ©is de usuÃ¡rio;
- Regras operacionais: conflito de horÃ¡rio (motorista/van), capacidade excedida, viagem sem motorista/van, dados inconsistentes;
- TransiÃ§Ãµes de ciclo de vida de viagens (vÃ¡lidas e invÃ¡lidas);
- ValidaÃ§Ã£o de nova viagem (van/motorista/rota inexistentes, conflitos);
- Agenda de viagens: data/horÃ¡rio no passado, chegada anterior ÃƒÂ  partida, viagem que atravessa a meia-noite e fuso de SÃ£o Paulo;
- CriaÃ§Ã£o de viagem com gravaÃ§Ã£o de presenÃ§as, ordenaÃ§Ã£o cronolÃ³gica e separaÃ§Ã£o entre horÃ¡rio previsto e real;
- CriaÃ§Ã£o/vÃ­nculo automÃ¡tico de perfil de Aluno/Motorista a partir da conta, incluindo conflitos de e-mail e de vÃ­nculo;
- Vans: bloqueio de placa duplicada, motorista/rota obrigatÃ³rios, modelo obrigatÃ³rio no formulÃ¡rio, busca com um caractere e identificaÃ§Ã£o por placa + modelo;
- Status da interface (rÃ³tulos e classes CSS);
- ValidaÃ§Ã£o brasileira de telefone e CNH.

A compilaÃ§Ã£o tambÃ©m valida as Views Razor.

## SeguranÃ§a

- Senhas novas sÃ£o armazenadas com hash PBKDF2, nunca em texto simples.
- A autenticaÃ§Ã£o usa cookie HttpOnly, SameSite e expiraÃ§Ã£o deslizante.
- AÃ§Ãµes administrativas requerem o papel "Administrador" no backend.
- Motoristas sÃ³ podem operar suas prÃ³prias viagens.
- Alunos sÃ³ podem confirmar presenÃ§a em suas prÃ³prias viagens.
- ProteÃ§Ã£o antiforgery em todos os formulÃ¡rios POST.
- Rate limiting em endpoints de login.
- Segredos de produÃ§Ã£o devem ser fornecidos por variÃ¡veis de ambiente.
- NÃ£o versione `easyvan.db`, tokens, senhas ou chaves de Data Protection.
- Upload de fotos valida tipo (JPEG/PNG/WebP) e tamanho (mÃ¡x. 2 MB) com nomes nÃ£o previsÃ­veis.

Consulte [SECURITY.md](SECURITY.md) para os controles, limitaÃ§Ãµes conhecidas e responsabilidades de implantaÃ§Ã£o.

## DocumentaÃ§Ã£o do projeto

- [DireÃ§Ã£o de produto](PRODUCT.md)
- [Sistema visual](DESIGN.md)
- [Acessibilidade](ACCESSIBILITY.md)
- [ContribuiÃ§Ã£o](CONTRIBUTING.md)
- [Privacidade](PRIVACY.md)
- [SeguranÃ§a](SECURITY.md)
- [EspecificaÃ§Ã£o de filtros](spec/filtros-e-busca.md)
- [Plano de testes](plan/03-plano-de-testes.md)

## Equipe

- Guilherme Pereira Andraz
- Davi Gato GrijÃ³ da Silva
- Victor Hugo Volpi Pereira
- Caio Candiani Regio
- Pedro Ailton dos Santos Cavalieri
- Enzo Miguel Bertoluci Ovido

## LicenÃ§a

NÃ£o hÃ¡ uma licenÃ§a open source definida neste momento. Consulte [LICENSE](LICENSE) antes de redistribuir ou reutilizar o cÃ³digo.

## Banco de dados e migrations PostgreSQL

A aplicaÃ§Ã£o usa SQLite apenas no desenvolvimento local. Em produÃ§Ã£o, o banco Ã© PostgreSQL via Supabase e o provider do EF Core Ã© `Npgsql.EntityFrameworkCore.PostgreSQL`.

- Configure `ConnectionStrings__DefaultConnection` com uma connection string PostgreSQL vÃ¡lida (TLS habilitado).
- O bootstrapper PostgreSQL cria um schema vazio usando o DDL gerado pelo modelo Npgsql e registra o baseline das migrations legadas; nÃ£o executa SQL SQLite nesse caminho.
- As migrations devem permanecer provider-neutral: nÃ£o fixe `INTEGER`/`TEXT` em `table.Column`, nÃ£o use `AUTOINCREMENT` fora do ramo SQLite e nÃ£o use `COLLATE NOCASE` no PostgreSQL.
- Colunas de chave primÃ¡ria `int` usam `ValueGeneratedOnAdd`; no PostgreSQL o provider Npgsql gera `GENERATED BY DEFAULT AS IDENTITY`. NÃ£o atribua IDs manualmente em dados de seed.
- Antes de publicar migrations novas, gere e revise o script PostgreSQL e execute os testes de integraÃ§Ã£o com PostgreSQL.

Consulte [`docs/database-postgresql.md`](docs/database-postgresql.md) para configuraÃ§Ã£o, testes e procedimento seguro de atualizaÃ§Ã£o.

## Linhas recorrentes e roteirizaÃ§Ã£o (implementaÃ§Ã£o em 08/11/2026)

O fluxo recomendado agora Ã© cadastrar uma **Linha** uma vez em `/Linhas/Create`: nome, origem/destino, dias, chegada na ida, saÃ­da na volta, sentidos, alunos e alocaÃ§Ã£o manual de alunos por van/motorista. O sistema gera ocorrÃªncias diÃ¡rias para atÃ© 14 dias Ãºteis e inicializa a presenÃ§a como SemResposta. A capacidade Ã© validada por van e no conjunto.

A roteirizaÃ§Ã£o usa somente pontos operacionais de alunos confirmados, calcula e armazena ordem/ETA e reutiliza o resultado enquanto o hash do conjunto nÃ£o muda. EndereÃ§os residenciais nÃ£o sÃ£o fallback para a rota. Com `GoogleMaps__ApiKey` configurada, chama a Routes API; sem chave, usa um roteirizador aproximado local e exibe aviso. O motorista recebe passageiros agrupados por ponto, pendentes em seÃ§Ã£o separada e links Google Maps segmentados em atÃ© trÃªs waypoints mÃ³veis por trecho. Consulte [`spec/fase-4-rotas-do-motorista.md`](spec/fase-4-rotas-do-motorista.md).

O painel administrativo exibe a ocupação das próximas viagens e os alertas A1–A6, com regras concentradas no serviço operacional e testes automatizados. Consulte [`spec/fase-5-alertas-e-ocupacao.md`](spec/fase-5-alertas-e-ocupacao.md).

As tabelas legadas de Rota/Parada/Agenda permanecem para compatibilidade histÃ³rica. Viagens ligadas a Linha nÃ£o sÃ£o criadas nem editadas manualmente. A migration `LinhasRecorrentesRoteirizacao` preserva o SQLite existente; aplique-a ao reiniciar a aplicaÃ§Ã£o. O isolamento por OrganizaÃ§Ã£oId agora Ã© aplicado por filtro global nas entidades operacionais.

Consulte `spec/linhas-recorrentes-e-roteirizacao.md` e `docs/google-maps.md` para regras, limites, preÃ§os e decisÃµes pendentes.

> **Estado atualizado em 08/11/2026:** esta seÃ§Ã£o substitui referÃªncias anteriores a 14 dias úteis, fluxo manual de rotas e ausÃªncia de OrganizaÃ§Ã£oId. O conceito atual Ã© Linha recorrente, com geraÃ§Ã£o automÃ¡tica, 14 dias Ãºteis e filtro por organizaÃ§Ã£o.

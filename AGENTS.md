# AGENTS.md â€” EasyVan

> InstruÃ§Ãµes para agentes de IA que trabalham neste repositÃ³rio. Leia antes de alterar qualquer arquivo.
> Documentos complementares: `README.md`, `PRODUCT.md`, `SECURITY.md`, `PRIVACY.md`, `spec/` e
> `docs/auditoria-briefing.md`.

## 1. VisÃ£o geral

EasyVan Ã© um sistema web **ASP.NET Core MVC (.NET 10)** para gerenciar transporte por vans
escolares/universitÃ¡rias. Substitui a organizaÃ§Ã£o feita por grupos de WhatsApp. PersistÃªncia com
**EF Core**: SQLite em desenvolvimento e PostgreSQL/Supabase em produÃ§Ã£o. Front-end com Razor Views,
Bootstrap, jQuery e `wwwroot/css/site.css`.

| Perfil | Papel |
|---|---|
| **Administrador** | Ãšnico gestor. Cria contas, vans, motoristas, rotas, paradas e vÃ­nculos. Ã‰ o cliente do produto. |
| **Motorista** | Apenas consulta. VÃª viagens do dia, passageiros confirmados por parada e abre a rota no Google Maps. |
| **Aluno** | Apenas consulta a prÃ³pria van/motorista/parada/horÃ¡rio e confirma ou cancela ida e volta de cada dia. |

**Diferenciais do produto:** presenÃ§a diÃ¡ria integrada ÃƒÂ  operaÃ§Ã£o (confirmados, ausentes, sem resposta,
ocupaÃ§Ã£o Ã— capacidade) e alertas operacionais por **regras de negÃ³cio**. NÃ£o Ã© IA. NÃ£o use "IA",
"inteligente" ou termos equivalentes na UI, na documentaÃ§Ã£o ou em comentÃ¡rios.

**Fora do MVP (nÃ£o implementar sem pedido explÃ­cito):** GPS em tempo real, otimizaÃ§Ã£o automÃ¡tica de rotas,
financeiro/cobranÃ§a, telas multiempresa, app nativo, push e perfil ResponsÃ¡vel.

NÃ£o existe cadastro pÃºblico. Somente o Admin cria, altera ou desativa contas (RN01).

## 2. Estado real do repositÃ³rio (auditado em 07/11/2026)

Esta seÃ§Ã£o substitui a descriÃ§Ã£o desatualizada do briefing original, que falava em vans em memÃ³ria,
`EnsureCreated()`, cadastro pÃºblico e ausÃªncia de Rota/Viagem/PresenÃ§a. **NÃ£o reimplemente o que jÃ¡ existe.**

**JÃ¡ existe:**

- autenticaÃ§Ã£o por cookie, hash PBKDF2 (`Services/PasswordService`), rate limit de login e revalidaÃ§Ã£o do
  usuÃ¡rio a cada requisiÃ§Ã£o;
- autorizaÃ§Ã£o por papel em controllers e views;
- CRUD administrativo de usuÃ¡rios, vans, motoristas, alunos e rotas;
- CRUD administrativo de agendas das rotas e paradas, alÃ©m de vÃ­nculos de alunos com histÃ³rico;
- EF Core com migraÃ§Ãµes; SQLite local e PostgreSQL/Supabase em produÃ§Ã£o;
- viagens manuais, transiÃ§Ãµes de estado, presenÃ§as, confirmaÃ§Ã£o "Vou/NÃ£o vou", marcaÃ§Ã£o pelo motorista,
  dashboard, auditoria e busca/filtros de vans;
- link de rota do Google Maps sem chave (origem/destino) em `Views/Shared/_MapaRota.cshtml`.

**Parcial ou pendente:**

- `Aluno` continua sendo a pessoa e `VinculoAluno` guarda rota/parada/sentido com histÃ³rico; nÃ£o hÃ¡ entidade `Passageiro` separada;
- `Rota` Ã© o percurso; a agenda fica em `HorarioRota`, separado da rota;
- `Viagem.Sentido` Ã© anulÃ¡vel para preservar viagens legadas; `VanId`/`MotoristaId` sÃ£o anulÃ¡veis para permitir detectar A2;
- `PresencaViagem` usa `Status`/`RespondidoEm`, mantendo temporariamente `StatusConfirmacao`/`ConfirmadoEm` para compatibilidade;
- existe `Organizacao` padrÃ£o, mas ainda nÃ£o hÃ¡ `OrganizacaoId` nas demais entidades nem isolamento multi-tenant; geraÃ§Ã£o automÃ¡tica e prazo de presenÃ§a estÃ£o implementados na Fase 3; alertas A3â€“A6 e paginaÃ§Ã£o permanecem pendentes;
- A1 conta apenas respostas confirmadas e A2 detecta viagem sem van ou motorista; a suÃ­te cobre essas regras;
- a visÃ£o do motorista nÃ£o agrupa por parada e o link do Maps nÃ£o usa `waypoints`;
- **Fase 0 aplicada em 07/11/2026:** as quatro contas legadas foram resetadas para hashes PBKDF2 de senhas
  aleatÃ³rias descartadas (exigem reset administrativo para voltar a autenticar); `easyvan.db` saiu do Ã­ndice
  e passou a ser ignorado (o histÃ³rico ainda contÃ©m o arquivo â€” reescrita exige autorizaÃ§Ã£o e force push);
  as pastas `API/`, `Backend/` e `DataBase/` foram removidas; o termo "Inteligente" foi retirado do README e
  do comentÃ¡rio do serviÃ§o.
- **Fase 2 aplicada em 08/11/2026:** telas de agenda, parada e vÃ­nculo, proteÃ§Ã£o contra exclusÃ£o de registros
  referenciados e sincronizaÃ§Ã£o da conta ativa com os cadastros de aluno/motorista. A decisÃ£o D6 da Fase 1
  mantÃ©m `OrganizacaoId`/RN06 fora desta etapa.
- **Fase 3 aplicada em 08/11/2026:** worker idempotente gera viagens/presencas para 14 dias úteis; respostas do aluno respeitam prazo configuravel. Defaults: ida as 21h do dia anterior; volta duas horas antes da saida. Configuração dos prazos global nesta versão; geração por Linha.

O relatÃ³rio detalhado, com severidade e evidÃªncias, estÃ¡ em `docs/auditoria-briefing.md`.

**Fase 1 aplicada em 08/11/2026:** entidades de organizaÃ§Ã£o, agenda, paradas e vÃ­nculos; sentido e FKs
opcionais em viagens; status de presenÃ§a com backfill compatÃ­vel; Ã­ndice Ãºnico parcial para vÃ­nculo ativo;
contas inativas bloqueadas no login/cookie; seed idempotente de desenvolvimento. A migration foi aplicada
ao SQLite local apÃ³s validaÃ§Ã£o em cÃ³pia. ExecuÃ§Ã£o em PostgreSQL real permanece pendente.

## 3. Regras de negÃ³cio (fonte da verdade)

- **RN01.** Somente o Admin cria, altera ou desativa contas. NÃ£o hÃ¡ cadastro pÃºblico.
- **RN02.** Um aluno pertence a uma rota e a uma parada por vez.
- **RN03.** A confirmaÃ§Ã£o de presenÃ§a sÃ³ pode ser alterada atÃ© o horÃ¡rio limite, configurÃ¡vel por
  organizaÃ§Ã£o. SugestÃ£o inicial: ida atÃ© 21h do dia anterior; volta atÃ© 2h antes da saÃ­da.
- **RN04.** Aluno sem resposta = **Pendente** (nÃ£o presume presente nem ausente). Na lista do motorista
  aparece separado dos confirmados. ApÃ³s o prazo, gera alerta A3. DecisÃ£o a validar com administradores
  reais; manter a regra isolada em um Ãºnico serviÃ§o para facilitar a troca.
- **RN05.** Viagens sÃ£o geradas a partir de rota + dias da semana + sentido (ida/volta).
- **RN06.** Todo dado Ã© filtrado por `OrganizacaoId` do usuÃ¡rio autenticado. Nunca confie em IDs vindos da
  requisiÃ§Ã£o sem validar a organizaÃ§Ã£o.

## 4. Alertas operacionais

| CÃ³digo | Regra | Gravidade |
|---|---|---|
| **A1** | Confirmados > capacidade da van | Alta |
| **A2** | Viagem sem van ou sem motorista | Alta |
| **A3** | Prazo encerrado com alunos sem resposta | MÃ©dia |
| **A4** | Aluno ativo sem rota ou sem parada | MÃ©dia |
| **A5** | Viagem sem nenhum confirmado | Baixa |
| **A6** | Van inativa/em manutenÃ§Ã£o atribuÃ­da a viagem | Alta |

Cada alerta precisa de teste automatizado. NÃ£o implemente alertas no controller ou na view: use um serviÃ§o
em `Services/`. A implementaÃ§Ã£o atual Ã© parcial (ver seÃ§Ã£o 2); nÃ£o considere A3â€“A6 prontos.

## 5. Modelo de dados alvo

| Entidade | Campos principais | SituaÃ§Ã£o no repositÃ³rio |
|---|---|---|
| `Organizacao` | Id, Nome | existe com seed padrÃ£o; sem isolamento multi-tenant |
| `Usuario` | Id, OrganizacaoId, Nome, Email, Perfil (Admin/Motorista/Aluno), SenhaHash, Ativo | existe sem `OrganizacaoId` e sem `Ativo`; `RoleManager` Ã© o papel atual |
| `Van` | Id, OrganizacaoId, Placa, Capacidade, Status, MotoristaId | existe; `Plate`, `Capacity`, `Status`, `MotoristaId` |
| `Rota` | Id, OrganizacaoId, Nome, VanPadraoId, Sentido, HorarioSaida, DiasSemana | percurso separado de `HorarioRota` |
| `Parada` | Id, OrganizacaoId, RotaId, Ordem, Endereco, HorarioPrevisto | existe; sentido diferencia ordem de ida/volta |
| `Passageiro` | Id, OrganizacaoId, UsuarioId, RotaId, ParadaId | `Aluno` + `VinculoAluno` com histÃ³rico por sentido |
| `Viagem` | Id, OrganizacaoId, RotaId, VanId, MotoristaId, Data, Sentido, Status | `Sentido` nullable em legado; FKs de van/motorista opcionais |
| `Presenca` | Id, OrganizacaoId, ViagemId, PassageiroId, Status (Confirmado/Ausente/SemResposta), RespondidoEm | `PresencaViagem.Status` + legado temporÃ¡rio |

No modelo final multi-tenant, todas as entidades, exceto `Organizacao`, terÃ£o `OrganizacaoId`. Esse campo e
o isolamento correspondente ainda nÃ£o existem: RN06 permanece pendente para fase futura. Use migraÃ§Ãµes EF
Core (`dotnet ef migrations add ...`), nunca `EnsureCreated()` em produÃ§Ã£o. NÃ£o renomeie entidades
existentes sem um plano de migraÃ§Ã£o e sem preservar os dados.

## 6. ConvenÃ§Ãµes de cÃ³digo

- C# moderno, nullable habilitado, `async/await` em todo acesso ao banco; nomes de domÃ­nio em portuguÃªs e
  termos tÃ©cnicos em inglÃªs.
- Regras de negÃ³cio em `Services/`, nÃ£o em controllers nem views. Controllers finos.
- ViewModels para entrada/saÃ­da; nunca exponha entidades diretamente em formulÃ¡rios (over-posting).
- `[Authorize]` por padrÃ£o; aÃ§Ãµes administrativas com `[Authorize(Roles = "Administrador")]` (ou polÃ­tica
  equivalente). Valide a organizaÃ§Ã£o em toda consulta.
- `[ValidateAntiForgeryToken]` e validaÃ§Ã£o no servidor (`ModelState.IsValid`) em todo POST.
- Senhas com `PasswordHasher`/PBKDF2; nunca em texto simples, nunca em logs.
- Segredos fora do repositÃ³rio (user-secrets/variÃ¡veis de ambiente); `*.db` no `.gitignore`.
- Mapa: link `https://www.google.com/maps/dir/?api=1&origin=...&destination=...&waypoints=...`, sem chave de
  API. Verifique o limite oficial de paradas por link e divida rotas maiores em trechos.
- Interface responsiva, mobile first, alvos de toque de ao menos 44 px, contraste AA, status nunca sÃ³ por cor.
- Datas de calendÃ¡rio em `America/Sao_Paulo`; instantes reais em UTC (`DateTimeOffset` quando apropriado).
- Sem comentÃ¡rios Ã³bvios; comente apenas decisÃµes nÃ£o triviais.

## 7. Comandos

```powershell
dotnet restore
dotnet build
dotnet run          # http://localhost:5256
dotnet test tests\EasyVan.Tests\EasyVan.Tests.csproj
dotnet ef migrations add <Nome>
dotnet ef database update
```

Rode `dotnet build` depois de qualquer mudanÃ§a em C# ou Razor e `dotnet test` antes de entregar.
Linha de base atual em 08/11/2026: build 0 avisos/0 erros; **133 testes aprovados** após a implementação de Linhas recorrentes.

## 8. Estrutura

`Controllers/`, `Data/`, `Models/`, `Services/`, `Views/`, `Migrations/`, `tests/`, `wwwroot/`, `spec/`,
`plan/`, `docs/`. Uma Ãºnica stack: .NET. NÃ£o recrie `API/`, `Backend/` ou `DataBase/` (Node/TS morto
removido na Fase 0).

## 9. SeguranÃ§a (inegociÃ¡veis)

1. **Nunca** grave ou compare senha em texto simples em cÃ³digo novo. As quatro contas legadas foram
   resetadas em 07/11/2026 para hashes de senhas aleatÃ³rias descartadas; elas sÃ³ voltam a autenticar apÃ³s
   reset administrativo. NÃ£o reintroduza o valor antigo.
2. NÃ£o commite segredos, chaves, connection strings de produÃ§Ã£o nem `easyvan.db`.
3. Toda aÃ§Ã£o administrativa exige checagem de papel.
4. Colete o mÃ­nimo de dados pessoais (LGPD). NÃ£o registre senhas, tokens, nomes, endereÃ§os ou e-mails em
   logs/URLs.
5. Valide e sanitize toda entrada; nunca confie em campos ocultos ou no cliente.
6. CSRF em todo POST; cuidado com over-posting.
7. Rate limiting no login e cookie HttpOnly/Secure/SameSite.
8. Dados de menores: acesso restrito ao necessÃ¡rio; nÃ£o exponha listas de alunos publicamente.

## 10. Fases e critÃ©rio de pronto

| Fase | Tarefa | Status em 08/11/2026 | Pronto quando |
|---|---|---|---|
| **0** | Base segura: hash, `*.db` fora do Git, autorizaÃ§Ã£o, fim do cadastro pÃºblico, remover placeholders, migraÃ§Ãµes | **Quase completa**: senhas legadas resetadas, `easyvan.db` fora do Ã­ndice, pastas mortas removidas e termo corrigido; falta reescrever o histÃ³rico Git (destrutivo, exige autorizaÃ§Ã£o explÃ­cita) | Nenhuma senha em texto simples; telas protegidas; sÃ³ Admin cria usuÃ¡rio |
| **1** | Modelo + migraÃ§Ãµes + `Organizacao` + seed (e `OrganizacaoId` nas entidades operacionais) | **Implementada em 08/11/2026**; ver `spec/fase-1-modelo.md`. MigraÃ§Ã£o SQLite validada em cÃ³pia e aplicada localmente; PostgreSQL real nÃ£o validado. | Executar validaÃ§Ã£o em PostgreSQL quando houver banco de teste |
| **2** | Telas do Admin: vans, motoristas, rotas, paradas, alunos e vÃ­nculos | **Implementada**; isolamento por organizaÃ§Ã£o foi adiado conforme D6 aprovado na Fase 1 | CRUD persistido e protegido por perfil; RN06 tem filtro global; cobertura PostgreSQL real permanece pendente |
| **3** | Geracao diaria + presenca com prazo | **Implementada em 08/11/2026**; janela idempotente de 14 dias úteis e limite configuravel | Build/test passam; ver `spec/fase-3-viagens-e-presenca.md` |
| **4** | VisÃ£o do motorista por parada + Maps | **Parcial**: motorista vÃª o ponto operacional e mapa usa waypoints confirmados com otimizaÃ§Ã£o; falta teste de integraÃ§Ã£o/agrupamento visual e divisÃ£o de rotas longas | Link abre a rota correta |
| **5** | Painel + alertas A1â€“A6 | **Parcial**: dashboard; A1/A2 parciais; A3â€“A6 ausentes | Cada alerta com teste |
| **6** | DocumentaÃ§Ã£o final, concorrentes, LGPD, plano comercial, piloto | **Pendente** | RelatÃ³rio completo e demo ensaiada |

Trabalhe **uma fase por vez**. NÃ£o adiante fases nem implemente itens fora do MVP.

## 11. Como trabalhar

- Leia este arquivo, `README.md`, `PRODUCT.md`, `spec/` e `docs/auditoria-briefing.md` antes de comeÃ§ar.
- FaÃ§a mudanÃ§as pequenas e focadas; uma tarefa, um conjunto coeso de arquivos.
- Para cada feature: atualize/crie uma spec curta em `spec/`, implemente, escreva testes das regras e rode
  build/test.
- Se faltar informaÃ§Ã£o (regra, campo, papel), pergunte em vez de inventar; se assumir, registre a suposiÃ§Ã£o.
- Ao terminar, diga em portuguÃªs o que mudou, quais arquivos foram tocados, como testar manualmente, quais
  decisÃµes foram tomadas e quais dÃºvidas permanecem.
- NÃ£o rode `git push`, force push, rebase destrutivo, nem apague banco/dados sem pedido explÃ­cito.
- NÃ£o crie dependÃªncias novas sem justificar.

## 12. PapÃ©is dos agentes

**Desenvolvedor:** segue as fases; implementa; testa; relata.

**Revisor:** verifica, nesta ordem: seguranÃ§a (hash, autorizaÃ§Ã£o, `OrganizacaoId`, CSRF, over-posting,
segredos/`.db`, PII), regras RN01â€“RN06 e A1â€“A6, corretude (fusos, concorrÃªncia, nulos, capacidade zero,
duplicidade), arquitetura (regra em serviÃ§o, sem placeholders, migraÃ§Ãµes), testes (cada alerta/prazo) e
escopo (nada fora do MVP). Formato: tabela `Arquivo/linha | Severidade | Problema | CorreÃ§Ã£o sugerida` +
veredito (Aprovar / Aprovar com ajustes / Reprovar). Sem elogios vazios; nÃ£o reescreva o que nÃ£o tem problema.

**QA:** testes unitÃ¡rios dos serviÃ§os (geraÃ§Ã£o de viagens, ocupaÃ§Ã£o, prazo, alertas) e de integraÃ§Ã£o das
rotas protegidas (perfis e organizaÃ§Ãµes); roteiro manual com os 3 perfis.

**DocumentaÃ§Ã£o:** mantenha README/spec/relatÃ³rio alinhados ao cÃ³digo real; registre todo prompt em
`docs/prompts-ia.md` (data, objetivo, prompt, resultado, aceito/alterado); nÃ£o afirme o que o sistema nÃ£o faz.

## 13. Testes

- xUnit; SQLite em memÃ³ria para testes com banco; `EnsureCreated()` apenas em testes.
- Cobertura mÃ­nima: caminho feliz e ao menos um caso de erro por regra; cada alerta e cada prazo com teste.
- Testes de autorizaÃ§Ã£o entre perfis e entre organizaÃ§Ãµes quando `OrganizacaoId` existir.
- Rode `dotnet test tests\EasyVan.Tests\EasyVan.Tests.csproj` antes de entregar.

## 14. DocumentaÃ§Ã£o e idioma

- Responda e escreva documentaÃ§Ã£o, mensagens de commit e textos de interface em **portuguÃªs do Brasil**.
- Mantenha `README.md`, `PRODUCT.md`, `spec/` e `docs/auditoria-briefing.md` atualizados.
- O professor exige o registro dos prompts de IA em `docs/prompts-ia.md`, convertido para `.docx` na entrega.
- NÃ£o use "IA", "inteligente" ou termos equivalentes para descrever os alertas: sÃ£o regras de negÃ³cio.

## 15. RestriÃ§Ãµes finais

- NÃ£o invente dados de mercado, preÃ§os ou concorrentes; marque como "a pesquisar".
- NÃ£o adicione dependÃªncias pesadas sem justificar.
- NÃ£o exponha dados pessoais em logs, URLs ou repositÃ³rio.
- Em dÃºvida entre simplicidade e sofisticaÃ§Ã£o, escolha a simplicidade que cumpre a regra de negÃ³cio.

## Estado atual â€” complemento de 08/11/2026

Este complemento substitui afirmaÃ§Ãµes anteriores deste arquivo que indiquem ausÃªncia de `OrganizacaoId`, RN06 pendente, viagens manuais como fluxo principal ou agenda de sete dias. Agora hÃ¡ escopo global de consulta por `OrganizacaoId`, entidade `Linha`, alocaÃ§Ãµes de vans/alunos, geraÃ§Ã£o idempotente por 14 dias Ãºteis, presenÃ§a SemResposta e roteirizaÃ§Ã£o com hash/cache; `spec/linhas-recorrentes-e-roteirizacao.md` Ã© a fonte atual do fluxo. As tabelas de Rota/Parada/Agenda permanecem legadas para compatibilidade. O navegador e o servidor precisam de chaves Google separadas (`GoogleMaps__BrowserApiKey` e `GoogleMaps__ApiKey`) com restriÃ§Ãµes apropriadas. Ãšltima verificaÃ§Ã£o: build sem avisos/erros e 133 testes aprovados; validaÃ§Ã£o com PostgreSQL real ainda pendente.

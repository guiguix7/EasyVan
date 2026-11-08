# Auditoria do briefing — EasyVan

| | |
|---|---|
| **Data** | 07/11/2026 |
| **Escopo** | Código da branch `main`, banco local `easyvan.db`, documentação e histórico Git. Nenhuma feature de produto foi implementada; a remediação da Fase 0 está registrada na seção 8. |
| **Método** | Leitura de controllers, models, services, `Data/ApplicationDbContext.cs`, migrations e docs; inspeção do SQLite via Python; `git ls-files`; `dotnet build`; `dotnet test`. |
| **Linha de base** | `dotnet build`: 0 avisos, 0 erros. `dotnet test`: **92 aprovados**, 0 falhas, 0 ignorados. |

## 1. Resumo executivo

O repositório está **mais avançado** do que a seção 2 do briefing original descreve: já usa EF Core com
migrações, não tem cadastro público, já possui Rota, Viagem e Presença e já tem autenticação por cookie com
autorização por papel. As lacunas reais estão em **isolamento por organização**, **modelo de paradas,
passageiros e agenda**, **prazo de confirmação** e **alertas A3–A6**.

Três achados críticos encontrados:

1. **Quatro contas legadas tinham senha literal de 6 caracteres** em `Usuarios.PasswordHasher`. O
   `PasswordService` espera hash Base64 e rejeitava esses valores; essas contas não autenticavam.
   **Remediado na Fase 0** (ver A01 e seção 8).
2. **`easyvan.db` estava versionado no Git** e contém dados reais de 14 usuários, 5 alunos, 5 motoristas,
   3 vans e 2 viagens. O `.gitignore` não bloqueava `*.db`. **Removido do índice na Fase 0**; o histórico
   ainda contém o arquivo (ver A02).
3. **Os arquivos `*.Designer.cs` das migrations estavam sintaticamente inválidos**: 396 cadeias
   `b.Property<...>` sem `;`, herdadas da remoção de linhas `.HasColumnType(...)` que continham o terminador.
   Um build limpo falhava com centenas de `CS1002`. **Corrigido nesta auditoria** (ver A03).

O termo “Centro de Operações **Inteligente**” também aparecia no README e em um comentário, contrariando a
regra de não usar “IA”/“inteligente”; foi substituído por “Centro de Operações” na Fase 0 (A08).

## 2. Status das fases

| Fase | Status | Evidência | O que falta |
|---|---|---|---|
| **0 — Base segura** | **Quase completa** | Hash PBKDF2 para senhas novas, autorização por papel, cadastro público removido, migrações EF Core em uso, 4 senhas literais resetadas, `easyvan.db` fora do índice, pastas mortas removidas e termo corrigido. | Apenas a reescrita do histórico Git para tirar `easyvan.db` (destrutiva; exige autorização explícita e force push). |
| **1 — Modelo + OrganizacaoId + seed** | **Pendente** | Não há `Organizacao`, `Parada` nem `Passageiro` separado; Rota/Viagem/Presença existem sem os campos-alvo. | A04, A07, A12, A13. |
| **2 — Telas do Admin** | **Parcial** | CRUD de usuários, vans, motoristas, alunos e rotas existe e está protegido por papel. | Paradas, vínculos aluno↔rota↔parada e isolamento por organização. |
| **3 — Viagens diárias + presença com prazo** | **Parcial** | Viagens são criadas manualmente; aluno confirma “Vou/Não vou”; motorista marca presença. | Geração por rota/dias/sentido (A11) e horário limite (A06). |
| **4 — Visão do motorista + Maps** | **Parcial** | Visão do motorista e link sem chave para origem/destino existem. | Agrupar por parada e usar `waypoints` (A10). |
| **5 — Painel + alertas A1–A6** | **Parcial** | Dashboard e auditoria existem; A1/A2 parciais; conflitos de horário e capacidade são detectados. | A3–A6, correção de A1/A2 e testes por alerta (A05). |
| **6 — Documentação final** | **Pendente** | — | Concorrentes, LGPD, plano comercial, relato do piloto e demo. |

## 3. Achados

| ID | Severidade | Achado | Evidência | Correção sugerida | Status |
|---|---|---|---|---|---|
| **A01** | **Crítica** | Quatro contas tinham senha em texto simples (6 caracteres), não hash. O login falhava porque `PasswordService.Verificar` tenta decodificar Base64. | Antes: `SELECT count(*) FROM Usuarios WHERE length(PasswordHasher) = 6` → 4; `Services/IPasswordService.cs:42` (`Verificar`); as outras 10 contas tinham hash de 68/72 caracteres. | **Resolvido em 07/11/2026:** as 4 contas (Ids 1–4) foram resetadas para hashes PBKDF2 v2 de senhas aleatórias descartadas; a senha antiga não confere mais e as contas exigem reset administrativo. Smoke test em HTTPS confirmou login administrativo e bloqueio da senha antiga. Ver seção 8. | **Corrigido** |
| **A02** | **Crítica** | `easyvan.db` estava versionado e contém dados reais. | Antes: `git ls-files "*.db"` → `easyvan.db`; `.gitignore` sem `*.db`; banco com 14 usuários, 5 alunos, 5 motoristas, 3 vans e 2 viagens. | **Parcialmente resolvido em 07/11/2026:** `*.db`, `*.db-shm` e `*.db-wal` adicionados ao `.gitignore`; `git rm --cached easyvan.db` removeu o arquivo do índice (o arquivo local foi preservado). O histórico Git ainda contém o arquivo; a reescrita/force push não foi executada por ser destrutiva e exigir autorização explícita. | **Parcial** |
| **A03** | **Crítica** | Migrations `*.Designer.cs` e o snapshot estavam inválidos: 396 propriedades sem `;`, quebrando qualquer build limpo. | `dotnet test` falhava com `CS1002` em `20261103174311_InitialCreate.Designer.cs`, `ApplicationDbContextModelSnapshot.cs` e outros; o commit `a60a3db` removeu linhas `.HasColumnType(...)` que continham o `;`. | Corrigido nesta sessão: 396 `;` adicionados, preservando BOM e conteúdo. `git diff` tem 396 inserções/396 remoções; build 0/0; 92 testes. Recomendado validar build limpo em CI. | **Corrigido** |
| **A04** | **Alta** | Não há `Organizacao` nem `OrganizacaoId`; todas as consultas são globais. RN06 não é cumprida. | `Data/ApplicationDbContext.cs` não tem `DbSet<Organizacao>`; models e controllers não filtram por organização. | Fase 1: criar `Organizacao`, adicionar `OrganizacaoId` nullable, backfill com organização padrão, tornar obrigatório, filtro global no DbContext e testes cruzados entre organizações. | Aberto |
| **A05** | **Alta** | Alertas incompletos: A1 conta todas as presenças (não só confirmados); A2 é inalcançável porque `VanId`/`MotoristaId` são `int` obrigatórios; A3–A6 não existem. | `Services/ValidacaoOperacional.cs:40` (`AnalisarAlertas`), `:48` e `:160` (todas as presenças), `:282` e `:300` (checagens de zero); não há código para A3–A6. | Serviço único de alertas; A1 deve contar `StatusConfirmacao == true`; A2 deve usar FKs anuláveis ou estado explícito “não atribuída”; implementar A3–A6 e um teste por alerta. | Aberto |
| **A06** | **Alta** | Não há horário limite de confirmação (RN03). | `Controllers/HomeController.cs:97` (`ConfirmarPresenca`), `:116` (bloqueia Finalizada/Cancelada) e `:122-123` (bloqueia apenas data anterior ao dia atual). | Serviço de prazo configurável por organização; defaults 21h do dia anterior (ida) e 2h antes da saída (volta); bloquear no servidor e testar os limites. Manter RN04 isolada. | Aberto |
| **A07** | **Alta** | Modelo sem paradas, passageiro separado e agenda de rota; RN02/RN05 não são cumpridas. | `Models/Rota.cs` (sem `VanPadraoId`, `Sentido`, `HorarioSaida`, `DiasSemana`); `Models/Aluno.cs` (sem `RotaId`/`ParadaId`); `Models/Viagem.cs` (sem `Sentido`). | Fase 1: decidir entre evoluir `Aluno` ou criar `Passageiro`; criar `Parada`; completar `Rota`/`Viagem`; migração com backfill e testes. | Aberto |
| **A08** | **Média** | Termo “Inteligente” contraria o briefing. | Antes: `README.md:20`, `README.md:33` e `Services/CentroOperacoesService.cs:8`. | **Resolvido em 07/11/2026:** “Centro de Operações Inteligente” substituído por “Centro de Operações” no README e no comentário do serviço. | **Corrigido** |
| **A09** | **Média** | Pastas placeholder com código Node/TS morto, contrariando “uma única stack .NET”. | Antes: `git ls-files API Backend DataBase` → `API/api.ts`, `Backend/DataBase/db.ts`, `Backend/SRC/AUTH/auth.ts`, `Backend/SRC/Data/dataAssets.js`, `Backend/SRC/Helper/helper.js`, `Backend/SRC/JS/server.js`, `Backend/SRC/Routes/routes.ts`. | **Resolvido em 07/11/2026:** as pastas `API/`, `Backend/` e `DataBase/` foram removidas; não há referências no `.csproj`, `.sln` ou `program.cs`. | **Corrigido** |
| **A10** | **Média** | Link do Maps só tem origem/destino; não usa `waypoints`. A chave de API opcional é injetada no HTML e usada pelo `maps.js`. | `Views/Shared/_MapaRota.cshtml:5`; `Views/Shared/_Layout.cshtml:43` (meta `google-maps-key`) e `:303` (`maps.js`); `Services/GoogleMapsOptions.cs`. | Link keyless com `waypoints`; conferir o limite oficial de paradas e dividir rotas; se a chave continuar opcional, restringi-la por referrer e documentar que o link principal não depende dela. Atualizar `PRODUCT.md`, que diz que mapas estão fora de escopo. | Aberto |
| **A11** | **Média** | Não há geração automática de viagens por rota + dias da semana + sentido. | `ViagensController` cria viagens manualmente; não há `BackgroundService` nem rotina de geração. | Fase 3: serviço de geração idempotente, com testes de dias/sentido/duplicidade. | Aberto |
| **A12** | **Baixa** | `PresencaViagem` usa `Presente` (bool) e `StatusConfirmacao` (bool?), não o enum `Confirmado/Ausente/SemResposta` + `RespondidoEm`. | `Models/PresencaViagem.cs`; `Data/ApplicationDbContext.cs` (índice único `ViagemId, AlunoId`). | Fase 1/3: enum e `RespondidoEm`; migração compatível, preservando o significado atual (`null` = sem resposta). | Aberto |
| **A13** | **Baixa** | `Usuario` não tem `Ativo`; a desativação existe só em `Aluno`/`Motorista`. | `Models/Usuarios.cs`; `Models/Aluno.cs` e `Models/Motorista.cs` têm `Ativo`. | Adicionar `Usuario.Ativo`, validar no login/revalidação de cookie e refletir em RN01. | Aberto |
| **A14** | **Baixa** | Listagens não têm paginação. | Controllers usam `ToListAsync()` sem `Skip`/`Take`. | Fora do MVP; reavaliar quando a frota/alunos crescerem. | Aceito |
| **A15** | **Baixa** | Documentação divergente do código. | Antes: `README.md:161` dizia “89 testes”; README dizia “não versionar `easyvan.db`” mas o arquivo estava versionado; `PRODUCT.md` diz que mapas estão fora de escopo, mas o README anuncia integração opcional; “Inteligente” no README. | **Parcialmente resolvido em 07/11/2026:** README atualizado para 92 testes e “Centro de Operações”; `easyvan.db` saiu do índice. A divergência sobre mapas no `PRODUCT.md` continua e será tratada na fase de documentação. | **Parcial** |
| **A16** | **Informativo** | A seção 2 do briefing está desatualizada. | O briefing diz “vans em memória”, “`EnsureCreated()`”, “cadastro público” e “faltam Rota, Parada, Passageiro, Viagem e Presença”. O código já usa EF Core/migrações, não tem cadastro público e já tem Rota/Viagem/Presença. | Usar a seção 2 do `AGENTS.md` e este relatório; não reimplementar o que já existe. | Registrado |

## 4. O que já está pronto (não refazer)

- Login por cookie, `PasswordService` PBKDF2 versionado (v1 100.000, v2 600.000 iterações), rehash no login,
  revalidação do usuário a cada requisição e rate limit de 5 tentativas/minuto por IP.
- Autorização por papel: Admin em usuários, vans (escrita), motoristas, alunos e rotas; Motorista em viagens;
  Aluno/Admin em presença; `HomeController` com ações separadas por perfil.
- Cadastro público removido; `UsuariosController.Create` é Admin-only e cria/víncula o perfil de Aluno ou
  Motorista na mesma transação (`Services/PerfilUsuarioService.cs`).
- EF Core com migrações; `PostgresDatabaseBootstrapper` usa `GenerateCreateScript` + `Migrate` no PostgreSQL e
  `Migrate()` no SQLite. `EnsureCreated()` aparece apenas em testes.
- CRUD de vans persistido em EF Core (não é mais lista em memória), com placa única, modelo, busca e filtros.
- Viagens com validação de data/horário, transições de estado, edição, ordenação cronológica, horários
  previstos × reais (`InicioReal`/`FimReal`/`DataChegada`) e presenças.
- Dashboard, auditoria (`LogOperacao`) e confirmação de presença pelo aluno/motorista.
- Link de rota sem chave para origem/destino em `_MapaRota.cshtml` (falta `waypoints`).
- 92 testes automatizados passando.

## 5. Verificação executada

```powershell
dotnet build --nologo
# Compilação com êxito. 0 Aviso(s), 0 Erro(s).

dotnet test tests\EasyVan.Tests\EasyVan.Tests.csproj --nologo
# Aprovado! Falha: 0, Aprovado: 92, Ignorado: 0, Total: 92.
```

- `git diff --stat -- Migrations`: 8 arquivos, 396 inserções e 396 remoções (apenas os `;`).
- `git diff --check -- Migrations`: sem erros de whitespace.
- Inspeção do SQLite: 14 usuários, 3 vans, 5 motoristas, 5 alunos, 3 rotas, 2 viagens, 5 presenças e
  4 registros de auditoria; 4 senhas de 6 caracteres e 10 hashes de 68/72.
- Não foi executado teste manual no navegador nesta auditoria; não houve alteração de comportamento em
  runtime além do reparo dos arquivos gerados de migration.

## 6. Decisões tomadas e pendentes

1. **Quatro senhas legadas — decidido:** reset para hashes PBKDF2 de senhas aleatórias descartadas; as contas
   não autenticam até um reset administrativo. Não foram deletadas.
2. **`easyvan.db` no histórico — parcialmente decidido:** removido do índice e ignorado; a reescrita do
   histórico continua pendente de autorização explícita, backup e force push. O arquivo não deve voltar a ser
   commitado.
3. **Passageiro:** evoluir `Aluno` com `RotaId`/`ParadaId` (menor migração de dados) ou criar `Passageiro`
   separado? O briefing pede `Passageiro`; a decisão precisa preservar os 5 alunos existentes.
4. **`OrganizacaoId`:** aplicar nullable → backfill → obrigatório, com filtro global e testes entre
   organizações. Não confiar em IDs da requisição.
5. **Mapa:** confirmar na documentação oficial o limite de `waypoints` por link antes de implementar; dividir
   rotas maiores em trechos. Não inventar o limite.
6. **Escopo imediato — decidido:** a Fase 0 foi aplicada; a próxima é a Fase 1, uma fase por vez.

## 7. Próximos passos recomendados

1. **Fase 0 — concluída, exceto histórico:** A01 resolvido, A02 removido do índice, A08/A09 corrigidos e
   `.gitignore` atualizado. A reescrita do histórico Git permanece pendente e só deve ocorrer com autorização
   explícita, backup e force push.
2. **Fase 1:** `Organizacao`, `OrganizacaoId`, `Parada`, `Passageiro` (ou evolução de `Aluno`), agenda de
   `Rota`, `Sentido` em `Viagem` e enum de presença — com migração, seed e testes.
3. Adicionar verificação de build/test em CI para impedir que migrations geradas quebrem o build novamente.
4. Só então avançar para as fases 2–6, uma por vez.

# Especificação — Fase 1: modelo operacional

| | |
|---|---|
| **Fase** | 1 — modelo de dados, migrações e seed |
| **Status** | Implementada em 08/11/2026; migration aplicada ao SQLite local após validação em cópia. Validação de execução PostgreSQL permanece pendente. |
| **Depende de** | Fase 0 (base segura) |
| **Relacionado** | `AGENTS.md`, `docs/auditoria-briefing.md`, `spec/filtros-e-busca.md` |

> Objetivo: fechar a modelagem antes de gerar migrations. As decisões da seção 9 foram aprovadas em
> 08/11/2026. Esta fase **não** implementa as telas novas da Fase 2, a geração
> automática da Fase 3, a visão por parada da Fase 4 nem os alertas A3–A6 da Fase 5.

## 1. Entidades atuais

```text
Usuarios 1 ── 0..1 Aluno
Usuarios 1 ── 0..1 Motorista
Motorista 1 ── 0..N Van
Rota     1 ── 0..N Van
Rota     1 ── 0..N Viagem
Van      1 ── 0..N Viagem
Motorista 1 ── 0..N Viagem
Viagem   1 ── 0..N PresencaViagem
Aluno    1 ── 0..N PresencaViagem
LogOperacao (independente)
```

| Entidade | Campos atuais | Observação |
|---|---|---|
| `Usuarios` | Id, Nome, Email, RoleManager, PasswordHasher, FotoPerfil, Tema | Sem `Ativo` e sem `OrganizacaoId`; e-mail único. |
| `Aluno` | Id, Nome, Email, Telefone, Endereco, Ativo, UsuarioId? | Acumula o papel de passageiro; não tem rota/parada. |
| `Motorista` | Id, Nome, Email, Telefone, CNH, Ativo, UsuarioId? | Conta 1:1 opcional. |
| `Rota` | Id, Nome, Descricao, Origem, Destino, Ativo | Sem agenda, sentido ou paradas. |
| `Van` | Id, Plate, Modelo?, Driver, Capacity, Route, Schedule, Status, Description, MotoristaId?, RotaId? | `Driver`, `Route` e `Schedule` são textos legados que duplicam relações. |
| `Viagem` | Id, VanId, MotoristaId, RotaId, Data, HoraInicio?, HoraFim?, DataChegada?, InicioReal?, FimReal?, Status, Observacao, DataCriacao | `VanId`/`MotoristaId`/`RotaId` obrigatórios; sem `Sentido`. |
| `PresencaViagem` | Id, ViagemId, AlunoId, Presente, StatusConfirmacao?, ConfirmadoEm?, Observacao | `StatusConfirmacao` é bool? (null = sem resposta); `Presente` é o embarque efetivo. |
| `LogOperacao` | auditoria | Não participa das relações de transporte. |

Dados atuais do SQLite: 14 usuários, 3 vans, 5 motoristas, 5 alunos, 3 rotas, 2 viagens, 5 presenças,
4 logs. As 5 presenças têm `StatusConfirmacao = NULL` e uma tem `Presente = 1`.

## 2. Entidades propostas

```text
Organizacao (isolada nesta fase; sem FK nas demais tabelas)

Rota 1 ── N HorarioRota
Rota 1 ── N Parada
Rota 1 ── N Viagem
Van  1 ── N Viagem
Motorista 1 ── N Viagem
HorarioRota 0..1 ── N Viagem

Aluno 1 ── N VinculoAluno
Rota  1 ── N VinculoAluno
Parada 1 ── N VinculoAluno

Viagem 1 ── N PresencaViagem
Aluno  1 ── N PresencaViagem

Usuarios 1 ── 0..1 Aluno
Usuarios 1 ── 0..1 Motorista
```

Os nomes físicos das tabelas novas serão fixados explicitamente no `OnModelCreating`
(`Organizacoes`, `HorariosRota`, `Paradas`, `VinculosAluno`) para não depender do pluralizador do EF Core.

### 2.1 `Organizacao`

| Campo | Tipo | Regra |
|---|---|---|
| Id | int | PK |
| Nome | string(120) | obrigatório |
| Ativo | bool | padrão `true` |

- Criar a tabela e uma linha padrão (`Id = 1`, `Nome = "EasyVan"`) via `DbInitializer` idempotente.
- **Não** adicionar `OrganizacaoId` nas demais tabelas nesta fase. Adicionar a coluna sem filtro global
  criaria uma falsa sensação de isolamento e violaria RN06. O isolamento completo (claim, filtro global,
  testes entre organizações) fica para uma fase própria, depois da Fase 2.
- Todo dado existente pertence, implicitamente, à organização 1.

### 2.2 `Usuarios` (ajuste pequeno)

| Campo novo | Tipo | Regra |
|---|---|---|
| Ativo | bool | padrão `true`; login e revalidação do cookie recusam conta inativa |

- Recomendado para RN01 (desativar contas sem apagar histórico). Se o escopo precisar ser menor, pode ir
  para a Fase 2 sem afetar o restante do modelo.
- As quatro contas resetadas na Fase 0 podem ser marcadas `Ativo = false`, com registro em
  `docs/auditoria-briefing.md`; reativar somente após reset administrativo de senha.

### 2.3 `Rota`

Mantém `Id, Nome, Descricao, Origem, Destino, Ativo`. A rota passa a representar o **percurso**, não a
agenda. `Origem`/`Destino` continuam como referência textual; a direção de cada operação fica explícita
em `HorarioRota`, `Parada`, `VinculoAluno` e `Viagem`.

### 2.4 `HorarioRota` (novo)

| Campo | Tipo | Regra |
|---|---|---|
| Id | int | PK |
| RotaId | int | FK `Rota`, obrigatório, `Restrict` |
| Sentido | enum `Ida`/`Volta` | obrigatório |
| DiasSemana | flags (`Domingo`=1 … `Sabado`=64), int | pelo menos um dia |
| HorarioSaida | `TimeSpan` | obrigatório, 00:00–23:59 |
| VanPadraoId | int? | FK `Van`, `SetNull`; sugestão, não imposição |
| Ativo | bool | padrão `true` |

Índice único em `(RotaId, Sentido, HorarioSaida, DiasSemana)` para evitar agenda duplicada.
Índice de consulta em `(RotaId, Ativo)`.

### 2.5 `Parada` (novo)

| Campo | Tipo | Regra |
|---|---|---|
| Id | int | PK |
| RotaId | int | FK `Rota`, obrigatório, `Restrict` |
| Sentido | enum `Ida`/`Volta` | obrigatório; permite ordens diferentes na ida e na volta |
| Ordem | int | >= 1; única por `(RotaId, Sentido, Ordem)` |
| Nome | string(120)? | rótulo curto para a interface |
| Endereco | string(200) | obrigatório |
| HorarioPrevisto | `TimeSpan?` | horário previsto de passagem; pode ser calculado depois |
| Ativo | bool | padrão `true` |

### 2.6 `VinculoAluno` (novo)

Representa o vínculo do aluno com a rota e o ponto de embarque. **Não** cria uma segunda identidade de
passageiro: `Aluno` continua sendo a pessoa; `VinculoAluno` é a matrícula operacional.

| Campo | Tipo | Regra |
|---|---|---|
| Id | int | PK |
| AlunoId | int | FK `Aluno`, obrigatório, `Restrict` |
| RotaId | int | FK `Rota`, obrigatório, `Restrict` |
| ParadaId | int | FK `Parada`, obrigatório, `Restrict` |
| Sentido | enum `Ida`/`Volta` | obrigatório |
| Ativo | bool | padrão `true` |
| DataInicio | `DateTime?` | data-calendário (meia-noite UTC do dia local) |
| DataFim | `DateTime?` | data-calendário; nula enquanto vigente |
| Observacao | string(300)? | opcional |

Regras:
- `Parada.RotaId` deve ser igual a `VinculoAluno.RotaId`; validar no serviço e, se possível, com check
  constraint.
- `Parada.Sentido` deve ser igual a `VinculoAluno.Sentido`.
- No máximo **um vínculo ativo por `(AlunoId, Sentido)`**. Garantir em serviço transacional e, quando
  possível, com índice único parcial (`WHERE Ativo = 1` no SQLite; `WHERE Ativo = TRUE` no PostgreSQL).
- Um aluno pode ter um vínculo de ida e outro de volta, com paradas diferentes. Isso atende RN02 sem
  impedir mudança futura de rota: o vínculo antigo é desativado e mantém `DataInicio`/`DataFim`.
- **Interpretação da RN02:** “uma rota e uma parada por vez” é aplicada **por sentido**. Ida e volta podem
  usar pontos diferentes; se a operação exigir a mesma rota nos dois sentidos, o serviço valida que os dois
  vínculos ativos apontem para a mesma `Rota`. O Admin nunca cria dois vínculos ativos para o mesmo sentido.

### 2.7 `Viagem`

| Campo novo/alterado | Tipo | Regra |
|---|---|---|
| Sentido | enum `Ida`/`Volta`? | obrigatório em viagens novas; nulo apenas em legado não classificado |
| HorarioRotaId | int? | FK `HorarioRota`, `SetNull`; preenchido em viagens geradas |
| VanId | int? | passa a nullable para permitir A2 (“viagem sem van”) |
| MotoristaId | int? | passa a nullable para permitir A2 (“viagem sem motorista”) |

- `RotaId` continua obrigatório.
- As FKs de `Van`/`Motorista` continuam `Restrict`: não apagar van/motorista com viagens; usar status
  inativo para A6.
- Viagens legadas sem sentido ficam com `Sentido = NULL` e a interface mostra “Não informado”. Não
  inventar sentido a partir de `Origem`/`Destino`.
- `HorarioRotaId` fica nulo em viagens manuais e legadas.

### 2.8 `PresencaViagem`

| Campo novo/alterado | Tipo | Regra |
|---|---|---|
| Status | enum `SemResposta`=0, `Confirmado`=1, `Ausente`=2 | padrão `SemResposta` |
| RespondidoEm | `DateTime?` | substitui `ConfirmadoEm`; instante UTC |
| Presente | bool | **mantido**: embarque efetivo marcado pelo motorista |
| StatusConfirmacao | bool? | **legado temporário**; removido na migração de limpeza |

- `Status` é a resposta do aluno; `Presente` é o comparecimento real. São conceitos diferentes:
  `Ausente` significa “não vou”; `Presente` significa “embarcou”.
- Backfill: `StatusConfirmacao = true` → `Confirmado`; `false` → `Ausente`; `NULL` → `SemResposta`.
- Todos os 5 registros atuais são `SemResposta`.
- Manter o índice único `(ViagemId, AlunoId)`.

## 3. Relacionamentos e cardinalidades

| Principal | Dependente | Cardinalidade | FK | Delete |
|---|---|---|---|---|
| `Usuarios` | `Aluno` | 1 : 0..1 | `Aluno.UsuarioId` | `SetNull` |
| `Usuarios` | `Motorista` | 1 : 0..1 | `Motorista.UsuarioId` | `SetNull` |
| `Rota` | `HorarioRota` | 1 : N | `HorarioRota.RotaId` | `Restrict` |
| `Rota` | `Parada` | 1 : N | `Parada.RotaId` | `Restrict` |
| `Rota` | `Viagem` | 1 : N | `Viagem.RotaId` | `Restrict` |
| `Van` | `Viagem` | 1 : N | `Viagem.VanId` (nullable) | `Restrict` |
| `Motorista` | `Viagem` | 1 : N | `Viagem.MotoristaId` (nullable) | `Restrict` |
| `HorarioRota` | `Viagem` | 1 : N (opcional) | `Viagem.HorarioRotaId` | `SetNull` |
| `Aluno` | `VinculoAluno` | 1 : N | `VinculoAluno.AlunoId` | `Restrict` |
| `Rota` | `VinculoAluno` | 1 : N | `VinculoAluno.RotaId` | `Restrict` |
| `Parada` | `VinculoAluno` | 1 : N | `VinculoAluno.ParadaId` | `Restrict` |
| `Viagem` | `PresencaViagem` | 1 : N | `PresencaViagem.ViagemId` | `Cascade` |
| `Aluno` | `PresencaViagem` | 1 : N | `PresencaViagem.AlunoId` | `Restrict` |

Regras de integridade que não cabem em FK simples:
- `VinculoAluno.RotaId == Parada.RotaId`.
- `VinculoAluno.Sentido == Parada.Sentido`.
- `Viagem.RotaId == HorarioRota.RotaId` quando `HorarioRotaId` não for nulo.
- `Viagem.Sentido == HorarioRota.Sentido` quando `HorarioRotaId` não for nulo.
- Um vínculo ativo por `(AlunoId, Sentido)`.
- Uma presença por `(ViagemId, AlunoId)`.

## 4. Estratégia para preservar os registros existentes

1. **Backup antes de qualquer migration.**
   - SQLite: copiar o arquivo com a API `sqlite3.Connection.backup` (o CLI `sqlite3` não está instalado),
     com o app parado.
   - PostgreSQL: `pg_dump` do banco de produção/homologação.
2. **Não apagar nem renomear tabelas/colunas existentes na primeira migration.** Apenas adicionar.
3. `Organizacao`: inserir a linha padrão via `DbInitializer`, não via `HasData`, para evitar problema de
   sequência no PostgreSQL.
4. `Usuarios.Ativo`: `true` para todos; as quatro contas resetadas podem ficar `false`.
5. `Van` e `Rota`: manter todos os registros e colunas atuais. `Van.Driver`, `Van.Route` e
   `Van.Schedule` continuam existindo, mas deixam de ser fonte da verdade; serão removidos em migração
   posterior, depois de as telas usarem as relações.
6. `HorarioRota` e `Parada`: não inferir a partir de `Van.Schedule` nem de endereços livres. Criar dados
   de demonstração apenas no seed de **Development**; em produção, o Admin cria na Fase 2.
7. `VinculoAluno`: não converter `Aluno.Endereco` em parada automaticamente. No seed de Development,
   criar vínculos explícitos para os alunos de demonstração. Em dados legados sem vínculo, o sistema
   mostra “sem vínculo” e a Fase 2 orienta o Admin a cadastrar.
8. `Viagem`: preservar as 2 viagens; `Sentido = NULL` e `HorarioRotaId = NULL` no legado. As FKs de van e
   motorista já estão preenchidas e continuam válidas.
9. `PresencaViagem`: adicionar `Status`, preencher pelo `StatusConfirmacao` e manter `Presente`. As 5
   presenças ficam `SemResposta`; a que tem `Presente = 1` continua com esse valor.
10. Conferir, depois da migration: mesmos totais (14/3/5/5/3/2/5/4), `PRAGMA foreign_key_check` vazio no
    SQLite e `PRAGMA integrity_check = ok`.

## 5. Plano de migrations

Nome proposto: `Fase1ModeloOperacional`.

### 5.1 Migration 1 — expansão (sem perda)

1. Criar `Organizacoes`, `HorariosRota`, `Paradas` e `VinculosAluno`.
2. Adicionar `Usuarios.Ativo` (`true`).
3. Adicionar `Viagens.Sentido` (nullable), `Viagens.HorarioRotaId` (nullable) e tornar
   `Viagens.VanId`/`MotoristaId` nullable.
4. Adicionar `PresencasViagem.Status` (default 0) e `RespondidoEm` (nullable).
5. Backfill de `Status` com SQL compatível com SQLite e PostgreSQL:

```sql
UPDATE "PresencasViagem"
SET "Status" = CASE
    WHEN "StatusConfirmacao" THEN 1
    WHEN NOT "StatusConfirmacao" THEN 2
    ELSE 0
END;
```

6. Copiar `ConfirmadoEm` para `RespondidoEm`.
7. Criar índices e FKs. O índice único parcial de `VinculoAluno` deve ser criado com SQL por provider:
   - SQLite: `WHERE "Ativo" = 1`
   - PostgreSQL: `WHERE "Ativo" = TRUE`
8. **Não** remover `StatusConfirmacao`, `ConfirmadoEm` nem as colunas de texto legadas nesta migration.

### 5.2 Migration 2 — limpeza (depois da validação)

1. Remover `PresencasViagem.StatusConfirmacao` e `ConfirmadoEm` quando nenhum código as usar.
2. Remover `Van.Driver`, `Van.Route` e `Van.Schedule` quando as telas usarem apenas as relações.
3. Tornar `Viagens.Sentido` obrigatório depois de classificar ou arquivar o legado.
4. Essa migration só é gerada depois de a Fase 2 estar estável; não faz parte da entrega inicial da
   Fase 1.

### 5.3 SQLite

- Aplicar primeiro em uma **cópia** de `easyvan.db`, com o app parado.
- EF Core 8+ faz rebuild de tabela para alterações de coluna; conferir FKs e índices no SQL gerado.
- Rodar `PRAGMA foreign_key_check` e `PRAGMA integrity_check`.
- Só então aplicar no `easyvan.db` real. O arquivo continua fora do Git.

### 5.4 PostgreSQL/Supabase

- Gerar o script com `dotnet ef migrations script`.
- Testar em banco vazio (o `PostgresDatabaseBootstrapper` usa `GenerateCreateScript` e baseline) e em
  uma cópia com dados.
- Conferir `__EFMigrationsHistory`, sequência de `Organizacoes` e o `setval` após o seed.
- O script não pode depender de `EnsureCreated()`.

### 5.5 Revisão dos arquivos gerados

O repositório já teve 396 propriedades sem `;` nos `*.Designer.cs`/snapshot. Depois de gerar a migration:
1. `dotnet build`;
2. revisar o `.Designer.cs` e o snapshot;
3. rodar `dotnet test`;
4. não commitar migration que não compile.

## 6. Impactos nos CRUDs existentes

| Área | Impacto na Fase 1 | Fase |
|---|---|---|
| `AlunosController` / `AlunoFormViewModel` | A entidade e o serviço de vínculo entram na Fase 1; o formulário para escolher rota, parada e sentido, a transação de criação/atualização e a exibição em `Index`/`Details` ficam para a Fase 2. Enquanto isso, o seed de Development cria vínculos explícitos. | 1 (entidade/serviço); 2 (formulário) |
| `RotasController` / views | Continuam com o CRUD atual. Novas telas de `HorarioRota` e `Parada` devem ser controllers/viewmodels separados, não campos soltos na entidade `Rota`. Com as FKs `Restrict`, `DeleteConfirmed` deve bloquear a exclusão de rota com horários, paradas, viagens ou vínculos e orientar a desativação. | 2 (telas); 1 (bloqueio de exclusão) |
| `ViagensController` / `ViagemFormViewModel` | Incluir `Sentido`; permitir `HorarioRotaId`; aceitar van/motorista nulos no modelo (formulário manual ainda exige). `CriarPresencasAsync` passa a usar `VinculoAluno` da rota/sentido; se não houver vínculos, mantém o comportamento legado (todos os alunos ativos) com aviso. | 1 |
| `HomeController.Aluno` | Trocar `StatusConfirmacao` por `Status`; exibir rota/parada do vínculo ativo; confirmar grava `Status` e `RespondidoEm`. | 1 |
| `HomeController.Motorista` | Exibir `Status` (confirmados × sem resposta) e a parada; agrupamento visual por parada fica para a Fase 4. `MarcarPresenca` continua gravando `Presente`. | 1 (status); 4 (agrupamento) |
| `VansController` | Sem mudança de contrato. `RotaId`/`MotoristaId` continuam; campos de texto legados continuam no formulário até a Fase 2. | 1 |
| `AlunosController` (exclusão) | Com `VinculoAluno` e `PresencaViagem` em `Restrict`, `DeleteConfirmed` deve bloquear a exclusão de aluno com vínculo ou presença e oferecer desativação (`Ativo = false`). | 1 |
| `AlunosController` / `MotoristasController` (ativo) | Ao desativar ou reativar um aluno/motorista, sincronizar `Aluno.Ativo`/`Motorista.Ativo` com `Usuario.Ativo` da conta vinculada, para que o status operacional e o login não divirjam. | 1 |
| `UsuariosController` (desativação) | `Usuario.Ativo` não pode ser usado para o Admin desativar a própria conta nem o último Admin ativo; validar no serviço/controller e testar. | 1 |
| `CentroOperacoesService` / `ValidacaoOperacional` | A1 passa a contar somente `Status == Confirmado`; A2 passa a detectar `VanId == null`/`MotoristaId == null`. A3–A6 não entram nesta fase. | 1 |
| `DbInitializer` | Garantir organização padrão; seed de desenvolvimento com horários, paradas e vínculos explícitos; nada de seed em produção. | 1 |
| Views e testes | Ajustar nomes (`Status`, `RespondidoEm`, `Sentido`), estados nulos de van/motorista e textos “Não informado”/“A definir”. | 1 |

## 7. Testes necessários

### 7.1 Serviços e regras

- `HorarioRota`: rejeitar `DiasSemana = 0`, horário inválido e agenda duplicada.
- `Parada`: exigir `Ordem >= 1`, endereço e ordem única por rota/sentido.
- `VinculoAluno`: impedir dois vínculos ativos para o mesmo aluno/sentido; exigir parada da mesma rota e
  sentido; desativar vínculo antigo ao trocar de rota.
- `Presenca`: backfill `true → Confirmado`, `false → Ausente`, `null → SemResposta`; `Presente` não é
  alterado pelo backfill.
- `Viagem`: nova viagem exige `Sentido`; viagem legada pode ter `null`; `VanId`/`MotoristaId` nulos são
  aceitos e geram A2.
- A1 conta apenas confirmados; A2 detecta van/motorista nulos.
- `CriarPresencasAsync`: cria presenças somente para alunos com vínculo ativo na rota/sentido; sem
  vínculos, usa o fallback legado e registra aviso.

### 7.2 Integração e migration

- Aplicar a migration em SQLite vazio e em cópia com os dados atuais; comparar totais e integridade.
- Testar o script PostgreSQL em banco vazio e em cópia; conferir histórico e sequência.
- Testar CRUD de aluno criando/atualizando vínculo em transação.
- Testar confirmação do aluno e atualização de `Status`/`RespondidoEm`.
- Testar que o motorista continua marcando `Presente` sem alterar a resposta do aluno.
- Regressão: os 92 testes atuais continuam passando; páginas de login, dashboard, vans, rotas, viagens e
  alunos continuam abrindo.

### 7.3 Manual

1. Backup e subir com `dotnet run --launch-profile https`.
2. Login admin, abrir `/Alunos`, vincular um aluno a uma rota/parada/sentido.
3. Criar uma viagem com sentido e conferir as presenças geradas.
4. Login aluno, confirmar “Vou” e conferir `Status`/`RespondidoEm`.
5. Login motorista, conferir confirmados × sem resposta e marcar `Presente`.
6. Rodar `PRAGMA integrity_check` e conferir os totais.

## 8. Riscos de compatibilidade

| Risco | Impacto | Mitigação |
|---|---|---|
| Remover `StatusConfirmacao` cedo demais | Alto: quebra views/serviços e perde a resposta antiga | Migration em duas etapas; manter a coluna até a Fase 2; backup |
| Rebuild de tabela no SQLite com FKs | Alto: perda/integridade | Testar em cópia; `foreign_key_check`; app parado |
| Baseline/sequência no PostgreSQL | Alto: migration não aplica ou ids duplicados | Testar em banco vazio e cópia; ajustar `setval`; revisar `__EFMigrationsHistory` |
| `VanId`/`MotoristaId` nullable | Médio: `NullReferenceException` em views/serviços | Compilar, revisar acessos com `!`, testar A2 e telas com “A definir” |
| Dois vínculos ativos por aluno/sentido | Médio: dados inconsistentes | Serviço transacional + índice único parcial; teste de concorrência |
| `VinculoAluno` sem dados legados | Médio: viagens sem presenças | Fallback temporário para todos os alunos ativos; seed de Development; aviso na UI |
| Campos `Van.Driver/Route/Schedule` obsoletos | Baixo/médio: dados divergentes | Manter somente leitura; fonte da verdade são as relações; remover na Fase 2 |
| `OrganizacaoId` adiado | Médio: migration futura maior | Modelo e plano de isolamento documentados; não implementar multiempresa pela metade |
| Migration gerada com sintaxe quebrada | Alto: build falha | `dotnet build` + revisão do `.Designer.cs`/snapshot antes de aplicar |
| Credencial demo `email@email.com/123456` | Alto se exposta | Trocar antes de qualquer ambiente acessível; não usar em produção |
| `easyvan.db` no histórico Git | Alto se o repositório for público | Não fazer push antes da avaliação; reescrever histórico só com autorização e backup |
| Escopo crescer para Fase 2/3 | Médio: atraso e retrabalho | Fase 1 entrega modelo + adaptações mínimas; telas e geração ficam para as fases seguintes |

## 9. Decisões aprovadas

Em 08/11/2026, o usuário aprovou seguir a proposta D1–D8. As decisões implementadas foram:

| # | Decisão aprovada | Alternativa |
|---|---|---|
| D1 | Manter `Aluno` como pessoa e criar `VinculoAluno` para rota/parada/sentido, com histórico. | Colocar `RotaId`/`ParadaId` direto em `Aluno` (mais simples, menos flexível). |
| D2 | Separar `Rota` (percurso) de `HorarioRota` (dias, horário, sentido, van padrão). | Guardar agenda na própria `Rota`, como no briefing original. |
| D3 | `Parada` tem `Sentido`, permitindo ordens diferentes na ida e na volta. | `Parada` sem sentido, com uma única ordem para os dois sentidos. |
| D4 | `PresencaViagem.Status` (enum) + `Presente` (embarque real). | Usar só o enum e perder a marcação de embarque. |
| D5 | `VanId`/`MotoristaId` nullable para A2 funcionar. | Manter obrigatórios e tratar A2 apenas como van/motorista inativos. |
| D6 | Criar `Organizacao` + seed, **sem** `OrganizacaoId` nas tabelas nesta fase. | Adicionar `OrganizacaoId` nullable em tudo agora. |
| D7 | Incluir `Usuarios.Ativo` para RN01. | Adiar para a Fase 2. |
| D8 | Migration em duas etapas (expansão e limpeza). | Uma migration única, com remoção imediata das colunas antigas. |

## 10. Critério de pronto da Fase 1

- Migration aplica do zero no SQLite e no PostgreSQL.
- Migration aplica sobre cópia dos dados atuais sem perder registros.
- `dotnet build` sem avisos/erros e `dotnet test` com regressão e novos testes das regras.
- Seed de Development cria organização, agenda, paradas e vínculos explícitos.
- Nenhuma tela nova da Fase 2, nenhuma geração automática e nenhum alerta A3–A6 implementado.
- `docs/prompts-ia.md` atualizado com a decisão e o resultado.

## 11. Resultado da implementação

- `20261108011958_SincronizarModeloAtual` materializa as entidades/campos da proposta; `20261108211217_ConcluirFase1Modelo` migra respostas legadas para `Status`/`RespondidoEm` e impõe um vínculo ativo por aluno/sentido com índice único parcial.
- `Organizacao` padrão é criada de forma idempotente; dados demonstrativos de agenda, paradas e vínculos só são criados em Development quando as tabelas centrais estão vazias, evitando associar registros legados a dados de exemplo.
- Login e validação do cookie recusam usuários inativos; o CRUD de usuários sincroniza o estado e impede desativar a própria conta ou o último administrador ativo.
- As regras de agenda/parada/vínculo e os respectivos testes estão em `Services/` e `tests/EasyVan.Tests/Fase1ModeloTests.cs`.
- Validação feita: migrations aplicadas em banco SQLite vazio e em cópia do banco atual, com contagens preservadas, respostas `true/false/null` convertidas para `Confirmado/Ausente/SemResposta`, `PRAGMA foreign_key_check` vazio e `PRAGMA integrity_check = ok`; migration aplicada também ao SQLite local após backup em `%LOCALAPPDATA%\Temp\easyvan-fase1-backup.db`. Build: 0 avisos/erros; testes: 101 aprovados.
- Na base local já existente, a inicialização criou a organização padrão; como as tabelas centrais tinham dados legados, nenhum horário, parada ou vínculo demonstrativo foi associado a esses registros. Os totais operacionais permaneceram iguais.
- Não havia conexão PostgreSQL de teste configurada; a migration/provider PostgreSQL não foi executada contra um servidor real.

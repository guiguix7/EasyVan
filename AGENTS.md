# AGENTS.md — EasyVan

> Instruções para agentes de IA que trabalham neste repositório. Leia antes de alterar qualquer arquivo.
> Documentos complementares: `README.md`, `PRODUCT.md`, `SECURITY.md`, `PRIVACY.md`, `spec/` e
> `docs/auditoria-briefing.md`.

## 1. Visão geral

EasyVan é um sistema web **ASP.NET Core MVC (.NET 10)** para gerenciar transporte por vans
escolares/universitárias. Substitui a organização feita por grupos de WhatsApp. Persistência com
**EF Core**: SQLite em desenvolvimento e PostgreSQL/Supabase em produção. Front-end com Razor Views,
Bootstrap, jQuery e `wwwroot/css/site.css`.

| Perfil | Papel |
|---|---|
| **Administrador** | Único gestor. Cria contas, vans, motoristas, rotas, paradas e vínculos. É o cliente do produto. |
| **Motorista** | Apenas consulta. Vê viagens do dia, passageiros confirmados por parada e abre a rota no Google Maps. |
| **Aluno** | Apenas consulta a própria van/motorista/parada/horário e confirma ou cancela ida e volta de cada dia. |

**Diferenciais do produto:** presença diária integrada à operação (confirmados, ausentes, sem resposta,
ocupação × capacidade) e alertas operacionais por **regras de negócio**. Não é IA. Não use "IA",
"inteligente" ou termos equivalentes na UI, na documentação ou em comentários.

**Fora do MVP (não implementar sem pedido explícito):** GPS em tempo real, otimização automática de rotas,
financeiro/cobrança, telas multiempresa, app nativo, push e perfil Responsável.

Não existe cadastro público. Somente o Admin cria, altera ou desativa contas (RN01).

## 2. Estado real do repositório (auditado em 07/11/2026)

Esta seção substitui a descrição desatualizada do briefing original, que falava em vans em memória,
`EnsureCreated()`, cadastro público e ausência de Rota/Viagem/Presença. **Não reimplemente o que já existe.**

**Já existe:**

- autenticação por cookie, hash PBKDF2 (`Services/PasswordService`), rate limit de login e revalidação do
  usuário a cada requisição;
- autorização por papel em controllers e views;
- CRUD administrativo de usuários, vans, motoristas, alunos e rotas;
- EF Core com migrações; SQLite local e PostgreSQL/Supabase em produção;
- viagens manuais, transições de estado, presenças, confirmação "Vou/Não vou", marcação pelo motorista,
  dashboard, auditoria e busca/filtros de vans;
- link de rota do Google Maps sem chave (origem/destino) em `Views/Shared/_MapaRota.cshtml`.

**Parcial ou pendente:**

- `Aluno` acumula o papel de passageiro; não há `RotaId`/`ParadaId` nem entidade `Passageiro` separada;
- `Rota` não tem `VanPadraoId`, `Sentido`, `HorarioSaida`, `DiasSemana` nem `Paradas`;
- `Viagem` não tem `Sentido`; as FKs `VanId`/`MotoristaId` são obrigatórias;
- `PresencaViagem` usa `Presente`/`StatusConfirmacao` (bool/bool?), não o enum `Confirmado/Ausente/SemResposta`;
- não há `Organizacao`/`OrganizacaoId`, isolamento multi-tenant, geração automática de viagens, prazo de
  confirmação (RN03), alertas A3–A6 nem paginação;
- A1/A2 existem de forma parcial: A1 conta todas as presenças, não só confirmados; A2 é inalcançável na
  prática porque as FKs são obrigatórias;
- a visão do motorista não agrupa por parada e o link do Maps não usa `waypoints`;
- **Fase 0 aplicada em 07/11/2026:** as quatro contas legadas foram resetadas para hashes PBKDF2 de senhas
  aleatórias descartadas (exigem reset administrativo para voltar a autenticar); `easyvan.db` saiu do índice
  e passou a ser ignorado (o histórico ainda contém o arquivo — reescrita exige autorização e force push);
  as pastas `API/`, `Backend/` e `DataBase/` foram removidas; o termo "Inteligente" foi retirado do README e
  do comentário do serviço.

O relatório detalhado, com severidade e evidências, está em `docs/auditoria-briefing.md`.

## 3. Regras de negócio (fonte da verdade)

- **RN01.** Somente o Admin cria, altera ou desativa contas. Não há cadastro público.
- **RN02.** Um aluno pertence a uma rota e a uma parada por vez.
- **RN03.** A confirmação de presença só pode ser alterada até o horário limite, configurável por
  organização. Sugestão inicial: ida até 21h do dia anterior; volta até 2h antes da saída.
- **RN04.** Aluno sem resposta = **Pendente** (não presume presente nem ausente). Na lista do motorista
  aparece separado dos confirmados. Após o prazo, gera alerta A3. Decisão a validar com administradores
  reais; manter a regra isolada em um único serviço para facilitar a troca.
- **RN05.** Viagens são geradas a partir de rota + dias da semana + sentido (ida/volta).
- **RN06.** Todo dado é filtrado por `OrganizacaoId` do usuário autenticado. Nunca confie em IDs vindos da
  requisição sem validar a organização.

## 4. Alertas operacionais

| Código | Regra | Gravidade |
|---|---|---|
| **A1** | Confirmados > capacidade da van | Alta |
| **A2** | Viagem sem van ou sem motorista | Alta |
| **A3** | Prazo encerrado com alunos sem resposta | Média |
| **A4** | Aluno ativo sem rota ou sem parada | Média |
| **A5** | Viagem sem nenhum confirmado | Baixa |
| **A6** | Van inativa/em manutenção atribuída a viagem | Alta |

Cada alerta precisa de teste automatizado. Não implemente alertas no controller ou na view: use um serviço
em `Services/`. A implementação atual é parcial (ver seção 2); não considere A3–A6 prontos.

## 5. Modelo de dados alvo

| Entidade | Campos principais | Situação no repositório |
|---|---|---|
| `Organizacao` | Id, Nome | não existe |
| `Usuario` | Id, OrganizacaoId, Nome, Email, Perfil (Admin/Motorista/Aluno), SenhaHash, Ativo | existe sem `OrganizacaoId` e sem `Ativo`; `RoleManager` é o papel atual |
| `Van` | Id, OrganizacaoId, Placa, Capacidade, Status, MotoristaId | existe; `Plate`, `Capacity`, `Status`, `MotoristaId` |
| `Rota` | Id, OrganizacaoId, Nome, VanPadraoId, Sentido, HorarioSaida, DiasSemana | existe sem os campos de agenda |
| `Parada` | Id, OrganizacaoId, RotaId, Ordem, Endereco, HorarioPrevisto | não existe |
| `Passageiro` | Id, OrganizacaoId, UsuarioId, RotaId, ParadaId | `Aluno` acumula o papel |
| `Viagem` | Id, OrganizacaoId, RotaId, VanId, MotoristaId, Data, Sentido, Status | existe sem `Sentido`/`OrganizacaoId` |
| `Presenca` | Id, OrganizacaoId, ViagemId, PassageiroId, Status (Confirmado/Ausente/SemResposta), RespondidoEm | `PresencaViagem` com bool/bool? |

Todas as entidades, exceto `Organizacao`, têm `OrganizacaoId`. Use migrações EF Core
(`dotnet ef migrations add ...`), nunca `EnsureCreated()` em produção. Não renomeie entidades existentes sem
um plano de migração e sem preservar os dados.

## 6. Convenções de código

- C# moderno, nullable habilitado, `async/await` em todo acesso ao banco; nomes de domínio em português e
  termos técnicos em inglês.
- Regras de negócio em `Services/`, não em controllers nem views. Controllers finos.
- ViewModels para entrada/saída; nunca exponha entidades diretamente em formulários (over-posting).
- `[Authorize]` por padrão; ações administrativas com `[Authorize(Roles = "Administrador")]` (ou política
  equivalente). Valide a organização em toda consulta.
- `[ValidateAntiForgeryToken]` e validação no servidor (`ModelState.IsValid`) em todo POST.
- Senhas com `PasswordHasher`/PBKDF2; nunca em texto simples, nunca em logs.
- Segredos fora do repositório (user-secrets/variáveis de ambiente); `*.db` no `.gitignore`.
- Mapa: link `https://www.google.com/maps/dir/?api=1&origin=...&destination=...&waypoints=...`, sem chave de
  API. Verifique o limite oficial de paradas por link e divida rotas maiores em trechos.
- Interface responsiva, mobile first, alvos de toque de ao menos 44 px, contraste AA, status nunca só por cor.
- Datas de calendário em `America/Sao_Paulo`; instantes reais em UTC (`DateTimeOffset` quando apropriado).
- Sem comentários óbvios; comente apenas decisões não triviais.

## 7. Comandos

```powershell
dotnet restore
dotnet build
dotnet run          # http://localhost:5256
dotnet test tests\EasyVan.Tests\EasyVan.Tests.csproj
dotnet ef migrations add <Nome>
dotnet ef database update
```

Rode `dotnet build` depois de qualquer mudança em C# ou Razor e `dotnet test` antes de entregar.
Linha de base em 07/11/2026: build 0 avisos/0 erros; **92 testes aprovados**.

## 8. Estrutura

`Controllers/`, `Data/`, `Models/`, `Services/`, `Views/`, `Migrations/`, `tests/`, `wwwroot/`, `spec/`,
`plan/`, `docs/`. Uma única stack: .NET. Não recrie `API/`, `Backend/` ou `DataBase/` (Node/TS morto
removido na Fase 0).

## 9. Segurança (inegociáveis)

1. **Nunca** grave ou compare senha em texto simples em código novo. As quatro contas legadas foram
   resetadas em 07/11/2026 para hashes de senhas aleatórias descartadas; elas só voltam a autenticar após
   reset administrativo. Não reintroduza o valor antigo.
2. Não commite segredos, chaves, connection strings de produção nem `easyvan.db`.
3. Toda ação administrativa exige checagem de papel.
4. Colete o mínimo de dados pessoais (LGPD). Não registre senhas, tokens, nomes, endereços ou e-mails em
   logs/URLs.
5. Valide e sanitize toda entrada; nunca confie em campos ocultos ou no cliente.
6. CSRF em todo POST; cuidado com over-posting.
7. Rate limiting no login e cookie HttpOnly/Secure/SameSite.
8. Dados de menores: acesso restrito ao necessário; não exponha listas de alunos publicamente.

## 10. Fases e critério de pronto

| Fase | Tarefa | Status em 07/11/2026 | Pronto quando |
|---|---|---|---|
| **0** | Base segura: hash, `*.db` fora do Git, autorização, fim do cadastro público, remover placeholders, migrações | **Quase completa**: senhas legadas resetadas, `easyvan.db` fora do índice, pastas mortas removidas e termo corrigido; falta reescrever o histórico Git (destrutivo, exige autorização explícita) | Nenhuma senha em texto simples; telas protegidas; só Admin cria usuário |
| **1** | Modelo + migrações + `Organizacao` + seed (sem `OrganizacaoId` nas demais tabelas nesta fase) | **Especificada**: ver `spec/fase-1-modelo.md`; decisões D1–D8 aguardam aprovação; sem migration | Migração aplica do zero; dados de exemplo |
| **2** | Telas do Admin: vans, motoristas, rotas, paradas, alunos e vínculos | **Parcial**: CRUD existe; faltam paradas, vínculos e isolamento | CRUD persistido, protegido por perfil e organização |
| **3** | Geração diária + presença com prazo | **Parcial**: viagens manuais e confirmação; faltam geração e prazo | Aluno confirma/cancela; bloqueio após prazo; testes |
| **4** | Visão do motorista por parada + Maps | **Parcial**: visão existe; falta agrupar e usar waypoints | Link abre a rota correta |
| **5** | Painel + alertas A1–A6 | **Parcial**: dashboard; A1/A2 parciais; A3–A6 ausentes | Cada alerta com teste |
| **6** | Documentação final, concorrentes, LGPD, plano comercial, piloto | **Pendente** | Relatório completo e demo ensaiada |

Trabalhe **uma fase por vez**. Não adiante fases nem implemente itens fora do MVP.

## 11. Como trabalhar

- Leia este arquivo, `README.md`, `PRODUCT.md`, `spec/` e `docs/auditoria-briefing.md` antes de começar.
- Faça mudanças pequenas e focadas; uma tarefa, um conjunto coeso de arquivos.
- Para cada feature: atualize/crie uma spec curta em `spec/`, implemente, escreva testes das regras e rode
  build/test.
- Se faltar informação (regra, campo, papel), pergunte em vez de inventar; se assumir, registre a suposição.
- Ao terminar, diga em português o que mudou, quais arquivos foram tocados, como testar manualmente, quais
  decisões foram tomadas e quais dúvidas permanecem.
- Não rode `git push`, force push, rebase destrutivo, nem apague banco/dados sem pedido explícito.
- Não crie dependências novas sem justificar.

## 12. Papéis dos agentes

**Desenvolvedor:** segue as fases; implementa; testa; relata.

**Revisor:** verifica, nesta ordem: segurança (hash, autorização, `OrganizacaoId`, CSRF, over-posting,
segredos/`.db`, PII), regras RN01–RN06 e A1–A6, corretude (fusos, concorrência, nulos, capacidade zero,
duplicidade), arquitetura (regra em serviço, sem placeholders, migrações), testes (cada alerta/prazo) e
escopo (nada fora do MVP). Formato: tabela `Arquivo/linha | Severidade | Problema | Correção sugerida` +
veredito (Aprovar / Aprovar com ajustes / Reprovar). Sem elogios vazios; não reescreva o que não tem problema.

**QA:** testes unitários dos serviços (geração de viagens, ocupação, prazo, alertas) e de integração das
rotas protegidas (perfis e organizações); roteiro manual com os 3 perfis.

**Documentação:** mantenha README/spec/relatório alinhados ao código real; registre todo prompt em
`docs/prompts-ia.md` (data, objetivo, prompt, resultado, aceito/alterado); não afirme o que o sistema não faz.

## 13. Testes

- xUnit; SQLite em memória para testes com banco; `EnsureCreated()` apenas em testes.
- Cobertura mínima: caminho feliz e ao menos um caso de erro por regra; cada alerta e cada prazo com teste.
- Testes de autorização entre perfis e entre organizações quando `OrganizacaoId` existir.
- Rode `dotnet test tests\EasyVan.Tests\EasyVan.Tests.csproj` antes de entregar.

## 14. Documentação e idioma

- Responda e escreva documentação, mensagens de commit e textos de interface em **português do Brasil**.
- Mantenha `README.md`, `PRODUCT.md`, `spec/` e `docs/auditoria-briefing.md` atualizados.
- O professor exige o registro dos prompts de IA em `docs/prompts-ia.md`, convertido para `.docx` na entrega.
- Não use "IA", "inteligente" ou termos equivalentes para descrever os alertas: são regras de negócio.

## 15. Restrições finais

- Não invente dados de mercado, preços ou concorrentes; marque como "a pesquisar".
- Não adicione dependências pesadas sem justificar.
- Não exponha dados pessoais em logs, URLs ou repositório.
- Em dúvida entre simplicidade e sofisticação, escolha a simplicidade que cumpre a regra de negócio.

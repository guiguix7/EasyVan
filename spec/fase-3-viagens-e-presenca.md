# Especificação — Fase 3: geração de viagens e prazo de presença

| Campo | Valor |
|---|---|
| Fase | 3 — viagens diárias e presença |
| Status | Implementada em 08/11/2026 |
| Dependência | Fases 1 e 2 aplicadas |

## Objetivo

Gerar viagens a partir das agendas ativas e permitir que o aluno altere sua resposta até o prazo definido, mantendo as datas de calendário em `America/Sao_Paulo` e os instantes de resposta em UTC.

## Escopo e decisões

- Serviço idempotente gera uma viagem por agenda ativa e data cujo dia da semana corresponda à agenda. Rota, sentido, horário, van padrão e motorista vinculado à van são copiados para a viagem; sem alocação, as FKs opcionais permanecem nulas para o A2.
- Uma rotina hospedada gera a janela ao iniciar a aplicação e recalcula os próximos dias a cada 15 minutos; agendas novas ou alteradas entram no ciclo rapidamente e reinicializações recuperam o período. O horizonte é configurável.
- Índice único `(HorarioRotaId, Data)` evita duplicidade também sob concorrência; viagens manuais/legadas com `HorarioRotaId = NULL` continuam permitidas.
- Ida: pode alterar até 21h do dia anterior. Volta: pode alterar até duas horas antes do horário de saída. Opções são configuráveis por seção de configuração/variável de ambiente.
- Como RN06/`OrganizacaoId` foi adiado, a configuração de prazo é única para a organização padrão nesta versão; não existe configuração por organização ainda.
- Viagem legada sem sentido informado usa o mais cedo entre os dois limites, sem inferir se era ida ou volta.
- O aluno pode mudar entre Confirmado e Ausente até o limite. Depois disso o endpoint bloqueia a alteração mesmo que a página esteja aberta; `RespondidoEm` e coluna legada são atualizados em UTC.
- A geração inicial de presenças reutiliza `PresencaViagemService` existente e a seleção de vínculos definida na Fase 1.

## Fora de escopo

Alertas A3–A6 e painel correspondente (Fase 5), agrupamento de passageiros por parada e waypoints (Fase 4), configuração por organização e implementação de `OrganizacaoId` (RN06), notificações e otimização de rotas.

## Interfaces de teste confirmadas

- Serviços públicos de geração e avaliação de prazo: datas, dias, sentido, idempotência, limites e timezone.
- Ação MVC `HomeController.ConfirmarPresenca`: autorização do aluno pelo vínculo com a presença, bloqueio após o prazo e gravação da resposta.
- SQLite em memória como banco real dos testes de integração; relógio controlado por `TimeProvider`.

## Critérios de aceite

- Gera viagem para cada dia/sentido da agenda dentro da janela; ignora agendas/dias inativos e não duplica em reexecução ou concorrência.
- Viagem gerada inclui presenças iniciais e herda van/motorista quando configurados.
- Ida permite resposta exatamente até 21h do dia anterior e bloqueia após; volta permite exatamente até saída menos duas horas e bloqueia após.
- Hora do servidor e calendário usam `America/Sao_Paulo`; `RespondidoEm` é UTC.
- A interface mostra o prazo e deixa claro quando a resposta foi encerrada; o servidor mantém a regra mesmo com POST manual.
- Testes cobrem sucesso e casos-limite dos serviços e da ação MVC. Build/test passam.

## Resultado da implementação

- `GeracaoViagemService` gera uma viagem por agenda ativa/data/dia correspondente e prepara presencas; a rotina hospedada recupera a janela ao iniciar e verifica o dia local a cada 15 minutos.
- Horizonte padrao: sete dias inclusivos, configuravel em `GeracaoViagens`. Indices unicos por agenda/data protegem contra duplicidade concorrente.
- `PrazoPresencaService` aplica 21h do dia anterior na ida e duas horas antes da saida na volta; limites configuraveis em `PrazoPresenca`. A resposta do aluno e bloqueada no endpoint depois do limite.
- Viagens legadas sem sentido usam o mais cedo entre os dois limites quando ha horario; sem horario ficam bloqueadas com orientacao.
- Datas de viagem sao datas de calendario local em Sao Paulo; instantes `RespondidoEm` permanecem UTC. Consultas de hoje no painel do aluno/motorista e no Centro de Operacoes comparam datas locais.
- Migration `20261108220412_GeracaoViagensDiarias` substitui o indice de agenda por um indice unico em agenda/data. Aplicada ao SQLite local apos backup; migration de banco vazio coberta pelos testes. PostgreSQL real nao estava configurado.
- Build sem avisos/erros; 125 testes aprovados.

### Verificação manual

1. Entrar como Administrador, criar agenda ativa para hoje e conferir a viagem no calendario. Reabrir a aplicacao e confirmar que nao ha duplicacao.
2. Entrar como Aluno, confirmar e cancelar antes do limite; verificar que a resposta persiste.
3. Repetir apos o limite, inclusive enviando POST com a pagina aberta; o servidor deve recusar e a tela deve indicar o encerramento.
4. Testar ida e volta e ajustar `PrazoPresenca__HorarioLimiteIda` / `PrazoPresenca__AntecedenciaLimiteVolta`; configurar horizonte em `GeracaoViagens__HorizonteDias`.

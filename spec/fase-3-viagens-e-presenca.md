# Especificação — Fase 3: geração de viagens e prazo de presença

| Campo | Valor |
|---|---|
| Fase | 3 — viagens diárias e presença |
| Status | Implementada; fluxo atual integrado a Linhas recorrentes |
| Data de revisão | 08/11/2026 |
| Dependências | Fases 1 e 2 aplicadas |

## Objetivo

Gerar viagens e presenças a partir das Linhas recorrentes e permitir que cada aluno confirme ou cancele ida e volta até o prazo, preservando a data local de calendário e registrando o instante da resposta em UTC.

## Regras e fluxo atual

- `GeracaoViagensHostedService` roda no início da aplicação e, por padrão, a cada 15 minutos. Calcula uma janela de 14 dias úteis inclusiva, sem considerar feriados municipais, e processa cada `OrganizacaoId`.
- Para cada Linha ativa, dia da semana configurado, sentido ativo e alocação de van, `GeracaoViagensLinhaService` cria uma ocorrência. Passageiros da van alocada recebem presença `SemResposta`.
- A geração pode ser repetida. Índices únicos e checagem antes da gravação protegem contra duplicidade; conflitos concorrentes são reconhecidos como ocorrência já existente.
- O gerador legado de `HorarioRota` permanece ativo para compatibilidade com cadastros antigos. Novos fluxos administrativos usam Linha; rotas, agendas e viagens legadas permanecem disponíveis para histórico.
- Ida: o aluno pode alterar a resposta até 21h do dia anterior. Volta: até duas horas antes do horário de saída. Os valores padrão são configuráveis em `PrazoPresenca` (`HorarioLimiteIda` e `AntecedenciaLimiteVolta`). Nesta versão, a configuração é comum às organizações.
- `PrazoPresencaService` compara o prazo com o horário de `America/Sao_Paulo`. O endpoint `HomeController.ConfirmarPresenca` valida novamente o prazo no servidor e grava `RespondidoEm` em UTC; página aberta antes do limite não permite contornar o bloqueio.
- Viagens sem sentido legado usam o mais cedo entre os limites de ida e volta quando há horário de saída. Sem horário, a resposta fica bloqueada com orientação.
- A presença do aluno altera o conjunto de confirmados usado pela roteirização da Linha; mudança no conjunto invalida o hash e permite recálculo.

## Escopo fora desta fase

Alertas A3–A6 e painel completo (Fase 5), experiência visual de passageiros agrupados por parada e validação de rotas longas no Maps (Fase 4), configuração de prazo independente por organização e feriados municipais. GPS em tempo real, push e cobrança permanecem fora do MVP.

## Critérios de aceite e cobertura

- Gerar ocorrências para dias, sentidos e alocações ativos; manter idempotência e não gerar para dias/sentidos desativados.
- Inicializar presenças como `SemResposta` e preservar respostas já registradas ao reexecutar o worker.
- Permitir resposta exatamente até o limite e bloquear após, tanto para ida quanto volta, inclusive em POST direto.
- Usar datas de calendário locais em São Paulo e instantes UTC para `RespondidoEm`.
- Testes automatizados cobrem geração/idempotência, isolamento entre organizações, limites e ação MVC.
- Build e suíte xUnit passam.

## Verificação desta revisão

- `dotnet build --nologo`: êxito, 0 avisos e 0 erros.
- `dotnet test tests\\EasyVan.Tests\\EasyVan.Tests.csproj --nologo`: 134 aprovados, 0 falhas na revisão da Fase 3.
- A primeira execução concorrente de build e teste disputou um arquivo intermediário em `obj`; a suíte passou quando executada isoladamente.

## Teste manual

1. Como Administrador, crie uma Linha com dias úteis, ida e volta, alunos e alocação de van; confirme as viagens geradas para até 14 dias úteis.
2. Reexecute o gerador/reinicie a aplicação e confira que não surgem ocorrências duplicadas nem respostas duplicadas.
3. Como Aluno, confirme e cancele ida/volta antes do limite; após o limite, envie também um POST com a página já aberta e confirme o bloqueio.
4. Confira que aluno sem resposta permanece pendente, não é contado como confirmado e não aparece como presente.
5. Como Motorista, confira as viagens atribuídas à sua operação; a apresentação e o agrupamento detalhado por parada seguem a Fase 4.

## Decisões e limitações

- A janela de 14 dias úteis exclui sábado e domingo, mas não feriados.
- Os limites são globais nesta versão, embora o worker e os dados sejam processados no escopo da organização.
- A regra de pendência e os horários sugeridos devem ser validados com administradores reais.
- A geração por agenda legada foi mantida para compatibilidade; Linha é o fluxo recomendado para novas operações.

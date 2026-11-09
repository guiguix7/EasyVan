# Fase 6 — Produto, LGPD, mercado e piloto

**Data de corte da pesquisa:** 08/11/2026  
**Objetivo:** consolidar a proposta do EasyVan e preparar validação acadêmica sem tratar hipóteses como resultados.

## Resumo

O EasyVan é um MVP web para organizar linhas recorrentes de transporte por vans. O administrador configura alunos, pontos operacionais, veículos, motoristas, dias e horários; o sistema gera viagens, coleta respostas diárias e apresenta ocupação e alertas. O motorista consulta a viagem e seus passageiros; o aluno responde se vai ou não em cada sentido.

O repositório e os documentos fornecidos descrevem o problema como informação operacional dispersa em mensagens e planilhas. A vantagem a validar é reunir confirmação diária, capacidade e alertas no fluxo do gestor. A pesquisa pública encontrou soluções brasileiras com escopos maiores, incluindo frequência, comunicação com responsáveis, rastreamento e cobrança. Portanto, o posicionamento plausível para o MVP é deliberadamente mais estreito: operação recorrente e presença, sem GPS, financeiro ou aplicativo nativo.

**Estado da validação:** não foi encontrado registro de entrevistas ou piloto real no repositório. Nenhum depoimento, economia de tempo, taxa de adesão ou resultado comercial é afirmado neste relatório. O protocolo de piloto e o roteiro de demonstração abaixo estão prontos para execução; a demonstração ao vivo ainda depende de ambiente e dados sintéticos preparados.

## Produto e escopo confirmado

| Perfil | Tarefa principal demonstrável |
|---|---|
| Administrador | Cadastrar a operação, configurar Linha e alocações, acompanhar viagens, ocupação e alertas. |
| Aluno | Consultar o próprio transporte e confirmar ou cancelar ida e volta até o prazo vigente. |
| Motorista | Consultar viagens, passageiros confirmados por ponto, pendentes separados e abrir trechos no Google Maps. |

O registro técnico atual descreve autenticação por papéis, cadastros administrativos, Linhas recorrentes, geração idempotente de viagens, estados de presença, painel e regras A1–A6. A versão atual não oferece interface de gestão multi-organização, GPS em tempo real, pagamentos, notificações push, aplicativo nativo ou perfil de responsável. A implementação de localização Google pode depender de chaves de navegador e servidor; endereços sem localização podem ser salvos, mas não produzem rota navegável calculada.

### Concorrência observada

Pesquisa exploratória em páginas públicas dos próprios fornecedores. As funcionalidades e ofertas abaixo são **declarações comerciais publicadas**, não testes independentes de qualidade. Valores podem mudar e devem ser confirmados diretamente antes de qualquer comparação de custo.

| Alternativa | O que a página pública anuncia | Oferta/preço público observado | Leitura para o EasyVan |
|---|---|---|---|
| WhatsApp e planilhas | Conversa e organização manual; não há estrutura própria de viagem, presença e capacidade. | WhatsApp pode ser usado sem assinatura do EasyVan; custo total de trabalho não medido. | Concorrente de hábito e custo de troca, não de funcionalidades equivalentes. O piloto precisa medir o trabalho manual atual. |
| Vanzinha | Gestão de rotas, chamadas, faltas, histórico, comunicação com responsáveis, PIX e manutenção; app para responsáveis e acesso web para motorista. | Página anuncia plano gratuito até 20 alunos e planos pagos entre R$ 19,90 e R$ 69,90/mês. | Oferta ampla e entrada gratuita são referências de expectativa do segmento. O EasyVan não deve alegar superioridade; hoje tem escopo menor e não inclui responsáveis ou cobrança. |
| VanUp | Anuncia cadastro de alunos, controle de presença, rotas, alertas, comunicação, financeiro e acesso multiusuário. | Não localizado preço público na página consultada. | Sobreposição direta em presença e operação; comparar em demonstração e entrevista, pois a página não permite avaliar detalhes de fluxo ou disponibilidade. |
| TotalVan | A plataforma web anuncia gestão operacional, frequência, rotas, financeiro, GPS e relatórios. Manual público descreve teste inicial e planos. | Manual consultado anuncia 30 dias de teste e R$ 129,90/mês, R$ 109,90/mês no semestral e R$ 99,90/mês no anual. Confirmar oferta antes de citar em apresentação. | Escopo e preço publicados mostram que existe oferta comercial, mas não demonstram disposição de pagamento do público do EasyVan nem equivalência de pacote. |

### Posicionamento e hipótese comercial

**Hipótese de cliente inicial:** operador independente ou pequeno gestor de transporte escolar/universitário que administra uma ou poucas vans e concentra a organização diária. Essa hipótese vem do escopo e das personas do projeto; não é estimativa de tamanho de mercado.

**Proposta a testar:** “configure a linha uma vez e acompanhe diariamente quem confirmou, quem está pendente e se a capacidade atende”. A promessa precisa ser limitada à operação realmente implementada e não pode sugerir rastreamento em tempo real, embarque verificado ou economia já comprovada.

**Modelo a validar:** assinatura recorrente, inicialmente comparando cobrança por van ativa com faixa por número de passageiros. A cobrança por veículo aparece como hipótese no relatório de ideia; não existe preço definido para EasyVan. Não adotar os preços dos concorrentes como preço próprio sem entrevistas e cálculo de custos.

Antes de escolher preço, medir: custo de hospedagem e banco por conta ativa; chamadas e faturamento Google Maps; suporte e implantação; tributos e meios de pagamento; quantidade de vans e passageiros por cliente; disposição de pagar e motivo de cancelamento. Fazer entrevistas sem apresentar preço como decisão fechada; depois testar faixas de preço com operadores distintos. Concorrentes e valores devem ser revisados no dia da apresentação.

## LGPD e proteção de dados

Esta seção é uma análise técnica do produto, não parecer jurídico. O operador de transporte tende a decidir finalidade e meios da operação cotidiana e pode atuar como controlador; o papel do EasyVan e de cada provedor depende do contrato e do tratamento efetivo. A ANPD orienta que controlador e operador sejam classificados conforme as decisões e atividades concretas, não apenas pelo nome dado no contrato.

Quando configurados, o autocomplete e a geocodificação podem transmitir endereços ao Google Maps Platform; cálculo de rota usa coordenadas. Esta transferência deve constar de aviso transparente, contratos e avaliação de fornecedor antes de dados reais.

### Dados e riscos do fluxo atual

| Dado tratado no produto | Uso operacional | Cuidado necessário |
|---|---|---|
| Nome, e-mail e telefone | Conta e comunicação operacional | Acesso por perfil, correção, retenção limitada e não exposição em logs/URLs. |
| Endereço e ponto de embarque/localização | Associar aluno a parada e calcular rota | Dado de localização pode revelar rotina; coletar só ponto operacional, restringir listas e não usar endereço residencial como fallback. |
| Escola e associação a Linha/van | Identificar contexto do aluno e planejar embarque | Dado de criança/adolescente exige avaliação documentada de melhor interesse e acesso estrito. |
| Presença, viagem e horário | Planejar ocupação e exibir operação | Evitar uso secundário, exportação pública ou retenção sem prazo definido. |
| CNH e dados de veículo do motorista, quando cadastrados | Cadastro operacional | Limitar campos e acesso à finalidade da operação. |
| Hash de senha, cookie e registros técnicos | Autenticação e segurança | Nunca armazenar senha em claro; não registrar token ou identificadores desnecessários. |

### Ações antes de um piloto com dados reais

1. Definir por escrito controlador, operador e suboperadores (hospedagem, banco e Google Maps), finalidades, instruções, responsabilidades, atendimento de direitos e encerramento do contrato. A classificação deve ser revisada no arranjo real.
2. Registrar uma hipótese de base legal por finalidade e categoria de titular. Não usar “consentimento” genérico para todo o sistema. Para crianças e adolescentes, avaliar o melhor interesse caso a caso; o Enunciado CD/ANPD nº 1 permite as hipóteses dos arts. 7º e 11, sempre com melhor interesse prevalente. A aplicação concreta e eventual representação legal exigem revisão jurídica.
3. Atualizar `PRIVACY.md` com o canal real de contato, destinatário dos pedidos, finalidade de geolocalização, provedor Maps, transferências internacionais pertinentes, retenção e procedimento de exclusão/anonimização. O contato ainda não existe no repositório.
4. Definir retenção para contas desativadas, vínculos históricos, presenças, auditoria, backups e incidentes; implementar procedimento verificável de exportação, correção, exclusão ou justificativa de retenção.
5. Restringir dados de alunos a Admin autorizado, ao próprio aluno e ao motorista designado na viagem. Verificar essa matriz com testes HTTP e uma base com duas organizações antes de uso comercial; a operação multi-organização ainda tem pendências documentadas.
6. Documentar resposta a incidentes, contatos e responsabilidades. A regra geral da Resolução CD/ANPD nº 15/2024 prevê comunicação de incidente de risco ou dano relevante em três dias úteis; a Resolução nº 2/2022 prevê prazo em dobro para agentes de pequeno porte nas situações nela descritas. Confirmar enquadramento e contagem no caso concreto.
7. Realizar avaliação de impacto proporcional ao risco antes de incluir menores e localização de rotina em produção. Revisar chaves Google com restrição por origem/API, volume enviado e termos do fornecedor.

**Conclusão de prontidão:** `PRIVACY.md` atual é descritivo, mas não configura sozinho uma operação comercial em conformidade. O canal do controlador, papéis contratuais, bases legais, retenção, direitos, transferência e processo de incidentes continuam pré-requisitos para piloto com dados pessoais reais. Preferir ambiente de teste com dados sintéticos até resolver esses itens.

## Protocolo de piloto

**Estado:** planejado; não executado nem há resultados disponíveis no repositório.

### Preparação

- Recrutar de 3 a 5 administradores/organizadores e, se possível, motoristas e alunos que utilizem transporte por van; documentar perfil, contexto e autorização de participação.
- Começar com dados sintéticos. Se houver dados reais, concluir as ações LGPD acima antes da importação.
- Registrar a rotina de referência por um período comparável, sem ampliar coleta: tempo gasto para montar listas, número de mensagens operacionais por dia, percentual de respostas no prazo e erros percebidos/ajustes de última hora.
- Testar primeiro uma Linha e uma van. Validar prazos de resposta e significados de “ausente” e “sem resposta” com organizadores reais; a regra atual deve permanecer configurável e não ser descrita como verdade validada.
- Definir duração, responsável, canal de suporte, critério de interrupção, autorização de depoimento e plano de remoção dos dados ao fim do piloto.

### Medidas e registro

| Medida | Como registrar | Resultado |
|---|---|---|
| Tempo administrativo | Minutos para consolidar e revisar a lista diária, antes e durante, por operação comparável. | A coletar |
| Resposta dos passageiros | Respostas recebidas antes do prazo ÷ passageiros ativos previstos, por sentido/dia. | A coletar |
| Trabalho manual | Mensagens ou contatos usados para confirmar presença e corrigir listas, com definição consistente antes/depois. | A coletar |
| Problemas detectados | Alertas verdadeiros identificados antes da saída, falsos positivos e problemas não detectados. | A coletar |
| Uso e retenção | Administradores e passageiros que retornam semanalmente; motivos de abandono. | A coletar |
| Usabilidade | Conclusão de tarefas, erros e pedidos de ajuda por perfil; não usar NPS isolado como prova de valor. | A coletar |

Publicar apenas contagens agregadas e consentidas; não expor nomes, endereços, rotas individuais, capturas com dados reais ou depoimentos sem autorização específica. O relatório pós-piloto precisa trazer período, participantes, método, denominadores, perdas de dados, limitações e citações autorizadas.

### Perguntas de entrevista

1. Como você registra hoje quem usa cada sentido da viagem? O que faz quando a pessoa não responde?
2. Quanto tempo leva para preparar e revisar a lista de uma manhã típica? Como mediu esse tempo?
3. Qual erro operacional recente exigiu retrabalho? Como foi detectado?
4. Quem precisa ver nomes, pontos e horários? Quem não deve ter acesso?
5. O que impediria motorista ou aluno de usar uma página web no celular?
6. Qual solução usa hoje e quais custos diretos e de trabalho ela tem?
7. O que precisaria funcionar para você manter o sistema depois do piloto? Qual recurso é indispensável e qual é dispensável?
8. Como prefere avaliar cobrança (por van ou por passageiros) e que evidência justificaria pagar? Não sugerir valor antes da resposta espontânea.

## Roteiro de demonstração

**Duração prevista:** 5 a 7 minutos. Usar conta e dados sintéticos próprios para demonstração; nunca projetar dados reais de alunos.

### Pré-condições

- Banco de demonstração restaurável, com uma van operacional, motorista, três alunos sintéticos e uma Linha recorrente; confirmar data, sentidos, horários e viagens geradas.
- Chaves Google configuradas e restritas se for demonstrar autocomplete/geocodificação/Routes API. Sem chaves ou pontos localizados, apresentar o aviso de indisponibilidade e não prometer um link navegável calculado.
- Confirmar prazo configurado e preparar viagens de teste coerentes com o relógio. Não alterar relógio de produção para fabricar alerta.
- Verificar acesso dos três papéis e preparar recuperação da base após a demonstração.

### Sequência

| Tempo | Perfil e ação | Evidência a apresentar |
|---|---|---|
| 0:00–0:30 | Contexto: explicar a rotina atual de mensagens e listas, usando exemplo fictício. | Problema sem mostrar conversas nem dados de terceiros. |
| 0:30–2:00 | Administrador: abrir Linha, passageiros, ponto, veículo/motorista e agenda; mostrar viagens geradas. | Configuração recorrente, capacidade e persistência. |
| 2:00–3:00 | Aluno A confirma ida; Aluno B cancela volta; Aluno C fica sem resposta. | Resposta altera somente a própria presença; pendente continua separado. |
| 3:00–4:00 | Administrador: atualizar painel e explicar ocupação e alerta pertinente. | Mostrar somente alertas reais; A3 exige prazo encerrado, A1 exige excesso de confirmados. |
| 4:00–5:30 | Motorista: abrir a viagem, identificar passageiros por ponto e pendentes; abrir trecho no Maps se configurado. | O motorista consulta sua operação; a navegação acontece no Google Maps. |
| 5:30–7:00 | Encerrar com limitações e convite ao piloto. | Sem alegar GPS, embarque confirmado, resultado comercial ou economia não medida. |

### Ensaio e aceite

Fazer ao menos um ensaio cronometrado por integrante que apresentará. Registrar duração, bloqueios por perfil, falhas de dados, legibilidade em celular, disponibilidade do Maps e recuperação do ambiente. O ensaio real continua pendente; este roteiro não é evidência de que a apresentação foi executada.

## Pendências para fechar a validação

- Executar entrevistas e documentar método/resultados sem inventar amostra.
- Conduzir piloto autorizado e preencher medidas com dados reais agregados.
- Nomear canal de contato LGPD e revisar política/termos com responsável jurídico.
- Validar acesso por perfil e isolamento organizacional; testar PostgreSQL real.
- Configurar chaves Maps restritas e ensaiar a demonstração num ambiente estável.
- Rever funcionalidades e preços de concorrentes no dia da apresentação.

## Fontes consultadas

### Concorrentes — páginas dos fornecedores

- [Vanzinha — funcionalidades e planos](https://www.vanzinha.com.br/)
- [Vanzinha — política de privacidade](https://www.vanzinha.com.br/privacidade)
- [VanUp — funcionalidades](https://vanup.com.br/)
- [TotalVan — manual público e planos](https://www.totalvan.com.br/manual)
- [TotalVan — apresentação da plataforma](https://www.totalvan.com.br/sobre)

### LGPD — fontes oficiais

- [Lei nº 13.709/2018 — texto atualizado, Câmara dos Deputados](https://www2.camara.leg.br/legin/fed/lei/2018/lei-13709-14-agosto-2018-787077-norma-atualizada-pl.html)
- [Enunciado CD/ANPD nº 1/2023 — melhor interesse de crianças e adolescentes](https://www.gov.br/anpd/pt-br/assuntos/noticias/anpd-divulga-enunciado-sobre-o-tratamento-de-dados-pessoais-de-criancas-e-adolescentes)
- [ANPD — direitos dos titulares](https://www.gov.br/anpd/pt-br/assuntos/titular-de-dados/direito-dos-titulares)
- [Resolução CD/ANPD nº 2/2022 — agentes de tratamento de pequeno porte](https://www.gov.br/anpd/pt-br/acesso-a-informacao/institucional/atos-normativos/regulamentacoes_anpd/resolucao-cd-anpd-no-2-de-27-de-janeiro-de-2022)
- [Resolução CD/ANPD nº 15/2024 — comunicação de incidente de segurança](https://www.gov.br/anpd/pt-br/acesso-a-informacao/institucional/atos-normativos/regulamentacoes_anpd/resolucao-cd-anpd-no-15-de-24-de-abril-de-2024)
- [ANPD — conceito de controlador e operador](https://www.gov.br/anpd/pt-br/centrais-de-conteudo/materiais-educativos-e-publicacoes/guia-orientativo-para-definicoes-dos-agentes-de-tratamento-de-dados-pessoais-e-do-encarregado)

### Documentos do projeto

- `EasyVan — Relatório da Ideia e do MVP.pdf`
- `EasyVan — Roteiros de uso por perfil.pdf`
- `EasyVan — Relatório de Produto SaaS, UX/UI e Desenvolvimento.pdf`
- `AGENTS.md`, `PRODUCT.md`, `PRIVACY.md`, `SECURITY.md`, `README.md` e specs atuais.

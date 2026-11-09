# Especificação — Fase 2: cadastros operacionais

| Campo | Valor |
|---|---|
| Fase | 2 — telas do Administrador |
| Status | Implementada em 08/11/2026 |
| Dependência | Fase 1 aplicada |

## Objetivo

Permitir que o Administrador mantenha a agenda das rotas, paradas e vínculos de alunos por rota/parada/sentido. Completar os CRUDs existentes com validação de relações e proteção do histórico.

## Escopo

- CRUD administrativo de `HorarioRota` e `Parada` em controllers e ViewModels próprios.
- Listagem e criação/troca de `VinculoAluno`; a troca encerra o vínculo ativo anterior do mesmo aluno e sentido e preserva o histórico.
- Exibir vínculos ativos/históricos no contexto administrativo do aluno e facilitar o acesso Ã  gestão de vínculos.
- Corrigir exclusões de aluno/rota para bloquear quando houver presenças, vínculos, paradas, agendas ou viagens dependentes.
- Manter o uso de ViewModels em POST, validação server-side, autorização Admin e CSRF.
- Sincronizar `Usuarios.Ativo` com `Aluno.Ativo`/`Motorista.Ativo` nos formulários operacionais existentes.

## Decisão sobre organização

Seguindo D6 aprovado na Fase 1, esta fase continua no modelo de uma organização padrão sem `OrganizacaoId` nas entidades e sem isolamento multi-tenant. Não há telas multiempresa. RN06 e a filtragem completa por organização permanecem pendentes para uma fase futura dedicada; não aceitar IDs de organização enviados pelo cliente.

## Regras

- Horário de rota precisa de rota, sentido válido, ao menos um dia válido, horário entre 00:00 e 23:59 e van padrão existente quando informada.
- Parada precisa de rota, sentido válido, ordem positiva única por rota/sentido e endereço não vazio.
- Vínculo precisa de aluno, rota e parada existentes/ativos; a parada precisa pertencer Ã  rota e ao sentido escolhidos.
- No máximo um vínculo ativo por aluno/sentido. Criar outro desativa o anterior na mesma transação e mantém data fim/histórico.
- Não excluir aluno com presenças ou vínculos, parada com vínculos, agenda com viagens nem rota com qualquer entidade dependente. Orientar a desativação ou troca de vínculo.
- Operações administrativas só podem ser executadas por Administrador e todos os POSTs validam antiforgery.

## Fora de escopo

Geração automática de viagens, prazo de presença, visão do motorista agrupada por parada, mapas com waypoints, alertas A3–A6, `OrganizacaoId`/isolamento multi-tenant e telas multiempresa.

## Critério de aceite

- Admin cria/edita/desativa agenda e paradas; os selects mostram registros ativos e preservam a seleção ao validar erros.
- Admin vincula aluno Ã  rota/parada/sentido, pode trocar o vínculo e consulta o histórico.
- Relações inconsistentes, referências inválidas, duplicidade e exclusões perigosas são bloqueadas pelo servidor.
- Testes cobrem caminho feliz, validações, troca de vínculo, restrição de exclusão e acesso por perfil.
- `dotnet build` e `dotnet test tests\EasyVan.Tests\EasyVan.Tests.csproj` passam.

## Resultado da implementação

- CRUD de agendas por rota/sentido/dias e CRUD de paradas por rota/sentido, com validação de duplicidade e relações.
- Gestão administrativa de vínculos por sentido, troca transacional do vínculo ativo e consulta de histórico.
- Views de detalhes da rota e do aluno dão acesso aos respectivos horários, paradas e vínculos.
- Formulários existentes de aluno/motorista mantêm `Usuarios.Ativo` sincronizado com o cadastro operacional.
- Exclusões de aluno e rota verificam dependências; agendas e paradas também preservam histórico relacionado.
- A autorização das novas áreas está restrita a `Administrador`; POSTs usam antiforgery e ViewModels.
- Não foi necessária migration nesta fase. `OrganizacaoId` e RN06 continuam pendentes conforme D6 da Fase 1.
- Testes: 111 aprovados; build sem avisos/erros.

### Verificação manual

1. Entrar como Administrador e abrir **Agenda das rotas**, cadastrar ida/volta e selecionar dias da semana.
2. Abrir **Paradas**, cadastrar paradas na rota e validar ordem/sentido.
3. Abrir um aluno, criar vínculos de ida e volta, trocar um vínculo e conferir o histórico.
4. Tentar excluir rota/aluno/parada/agenda com dependências; a operação deve ser bloqueada com orientação.
5. Entrar como Motorista ou Aluno e confirmar que as novas áreas administrativas não são autorizadas.

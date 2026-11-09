# Especificação — Operação da rota do motorista

| Campo | Valor |
|---|---|
| Fase | 4 — visão do motorista + Maps |
| Status | Parcial, implementada em 08/11/2026 |
| Dependências | Agenda recorrente e vínculos rota/parada/aluno da Fase 3 |

## Comportamento

- A agenda ativa da rota gera viagens por data e sentido; o administrador não precisa cadastrar a mesma viagem todos os dias.
- O painel do motorista mostra o local operacional de embarque vinculado ao aluno, agrupável pela ordem definida na parada. Não usa o endereço cadastral do aluno para traçar a rota.
- A rota da viagem usa origem e destino da rota, invertidos no sentido Volta. O mapa interativo considera apenas paradas ativas de alunos com status `Confirmado`, ordenadas pela sequência cadastrada e pede ao Google Maps JavaScript Directions que otimize a ordem.
- Uma resposta `Ausente` remove a parada do próximo carregamento da visão do motorista; a mudança não altera uma navegação que já foi iniciada no aplicativo externo.
- Sem chave Maps válida, permanece o fallback; o link externo usa no máximo três waypoints para compatibilidade móvel. A visualização interativa aceita até 25 paradas intermediárias neste fluxo.

## Limites e decisões

- A otimização depende da chave já configurável em `GoogleMaps__ApiKey` e de Maps JavaScript API, Routes API e faturamento habilitados no projeto Google. O pedido atual otimiza a ordem com foco em tempo/distância/curvas; não seleciona rota energeticamente eficiente nem garante economia de combustível.
- A aplicação ainda não divide automaticamente rotas acima de 25 paradas em vários trechos, não calcula chegada-alvo nem escolhe a van por ocupação. Alertas de capacidade pertencem à Fase 5.
- O Maps recebe os endereços das paradas operacionais para geocodificação/rota. Usar pontos de embarque definidos pelo administrador; não usar endereço residencial do cadastro do aluno.
- Referências: [Routes Library](https://developers.google.com/maps/documentation/javascript/routes/route-class-overview), [otimização de paradas](https://developers.google.com/maps/documentation/javascript/routes/optimize-stops) e [limites de Maps URLs](https://developers.google.com/maps/documentation/urls/get-started).

## Validação

- Conferir no painel que aluno `Confirmado` entra no trajeto e `Ausente`/`SemResposta` não entra.
- Conferir ida e volta com origem/destino invertidos, rota sem chave e rota com mais de três paradas no mapa interativo.
- Build e suíte de testes do projeto devem passar. A validação visual em navegador ainda requer uma chave Maps válida.

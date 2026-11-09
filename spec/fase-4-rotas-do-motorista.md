# Especificação — Fase 4: visão do motorista e Google Maps

| Campo | Valor |
|---|---|
| Fase | 4 — visão do motorista + Maps |
| Status | Implementada em 08/11/2026 |
| Dependências | Linhas recorrentes, geração de viagens e presença |

## Regras

- O Administrador define um ponto operacional de embarque por aluno na Linha. Endereço e coordenadas do ponto são persistidos separados do endereço cadastral do aluno; o endereço residencial não é usado na roteirização nem enviado ao Google Maps.
- Somente alunos com presença `Confirmado` compõem as paradas da rota. `Ausente` não entra no trajeto. `SemResposta` aparece em seção própria, agrupada pelo ponto operacional, para o motorista conferir antes de sair.
- Passageiros que compartilham as mesmas coordenadas de embarque são exibidos juntos em uma parada. Ordem e ETA vêm da rota calculada; quando o serviço externo falha, a rota aproximada e o aviso permanecem visíveis.
- Os links externos usam origem explícita, pontos operacionais confirmados, destino da Linha e a ordem já calculada. Em dispositivos móveis cada URL contém no máximo três paradas intermediárias; rotas maiores são segmentadas, preservando como origem do trecho seguinte o destino do trecho anterior.
- Ida e volta invertem origem e destino. Quando a volta termina no último aluno, esse aluno torna-se o destino final.
- Registros legados continuam usando vínculos/paradas antigas, sem misturar endereços pessoais com o fluxo de Linha.

## Persistência

A migration `Fase4PontosDeEmbarque` adiciona endereço e coordenadas opcionais a `LinhaPassageiro` e `ViagemParada`. Campos legados ficam nulos até a administração revisar e cadastrar o ponto operacional; não há backfill automático do endereço residencial. As paradas calculadas guardam um snapshot do ponto usado na viagem.

## Privacidade e Maps

As rotas de Linha e URLs enviam coordenadas dos pontos operacionais, sem nome de passageiro ou endereço residencial. A consulta de dados permanece no escopo da organização e motorista vê apenas viagens atribuídas à sua operação. Links abrem em nova aba com `rel="noopener noreferrer"`.

O limite móvel de três waypoints intermediários e o limite geral de 2.048 caracteres vêm da [documentação oficial de Maps URLs](https://developers.google.com/maps/documentation/urls/get-started). A implementação usa coordenadas curtas e segmentação em até três pontos intermediários por link.

## Cobertura e validação

- Testes de agrupamento de alunos no mesmo ponto, ordem de paradas e divisão de links.
- Teste garante que endereço/coordenadas residenciais não são usados sem ponto operacional.
- Teste de fallback garante que as coordenadas salvas na parada são as do ponto operacional.
- Build e suíte xUnit devem passar.

### Roteiro manual

1. Como Administrador, crie/edite uma Linha, selecione alunos e informe o ponto operacional de embarque de cada um por texto ou Google Maps.
2. Gere uma viagem e, como Aluno, confirme duas pessoas no mesmo ponto, confirme outra em local distinto e deixe uma sem resposta.
3. Como Motorista, confira os dois pontos confirmados agrupados, a pessoa pendente em seção separada e as ETAs em ordem.
4. Abra os trechos no Google Maps e confirme que a origem, os pontos e o destino correspondem à viagem; use quatro ou mais pontos para conferir a segmentação.
5. Altere o ponto operacional e reabra/recalcule a viagem. O endereço residencial do cadastro não deve aparecer no trajeto.

## Limitação

Linhas existentes antes desta migration podem não ter pontos operacionais. O sistema informa a falta de ponto e não usa o endereço residencial como fallback; a Administração precisa completar o cadastro para produzir rota.

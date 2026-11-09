# Fase 5 — Painel, ocupação e alertas operacionais

## Escopo

O painel administrativo resume a ocupação das viagens dos próximos sete dias e apresenta alertas operacionais derivados de regras de negócio. A análise permanece em `ValidacaoOperacional`; o serviço do painel apenas carrega e organiza os dados para a view.

## Regras implementadas

- **A1 (Alta):** presenças Confirmadas excedem a capacidade da van.
- **A2 (Alta):** viagem ativa sem van ou motorista associado.
- **A3 (Média):** prazo de resposta encerrado com presença SemResposta.
- **A4 (Média):** aluno ativo sem Linha/vínculo operacional com ponto de embarque válido.
- **A5 (Baixa):** viagem ativa sem presença Confirmada.
- **A6 (Alta):** viagem ativa associada a van não operacional (inativa/manutenção).

A ocupação separa Confirmados, Ausentes e SemResposta; o percentual usa a capacidade da van quando positiva. Estados de viagens encerradas/canceladas não geram alertas de viagem.

## Verificação

Testes de serviço cobrem A1–A6, incluindo casos positivos e negativos das regras e as situações de prazo, vínculo operacional e van em manutenção. A validação completa é `dotnet build --nologo -c Release` e `dotnet test tests\EasyVan.Tests\EasyVan.Tests.csproj --nologo -c Release`.

## Limites conhecidos

Os prazos ainda são configuração global nesta versão. O isolamento multi-organização/RN06 continua pendente. A tela de criação de Linhas permite coordenadas manuais quando as chaves Google não estiverem configuradas; autocomplete e geocodificação dependem das respectivas chaves.

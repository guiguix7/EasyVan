# Especificação — Filtros e Busca de Vans

| | |
|---|---|
| **Feature** | Filtros e Busca |
| **Tela** | `Vans/Index` (`GET /Vans`) |
| **Entidade** | `Van` |
| **Versão** | 1.0 |
| **Relacionado** | `/plan/02-planejamento-filtros-e-busca.md`, `/plan/03-plano-de-testes.md` |

## 1. Descrição

A listagem de vans passa a aceitar uma **busca textual** e **filtros** (status e faixa de capacidade),
combináveis entre si, aplicados no servidor e refletidos na URL (query string).

## 2. Requisitos funcionais

| ID | Requisito |
|---|---|
| RF01 | O sistema deve exibir, em `/Vans`, um formulário com busca, filtro de status e capacidade mínima/máxima. |
| RF02 | O sistema deve buscar o texto informado em Placa, Motorista, Rota e Descrição. |
| RF03 | O sistema deve filtrar por Status usando os status existentes nos dados. |
| RF04 | O sistema deve filtrar por capacidade mínima e/ou máxima (inclusivas). |
| RF05 | O sistema deve combinar todos os critérios informados (E lógico). |
| RF06 | O sistema deve exibir "Exibindo X de Y vans". |
| RF07 | O sistema deve exibir mensagem quando nenhuma van corresponder aos critérios. |
| RF08 | O sistema deve manter os valores informados nos campos após filtrar. |
| RF09 | O sistema deve oferecer ação "Limpar filtros" que restaura a lista completa. |
| RF10 | O sistema deve validar as entradas e exibir mensagens em português, ignorando apenas o critério inválido. |
| RF11 | Sem nenhum critério, o comportamento deve ser idêntico ao anterior (lista completa). |

## 3. Requisitos não funcionais

| ID | Requisito |
|---|---|
| RNF01 | Sem novas dependências (NuGet/npm/CDN) e sem JavaScript novo. |
| RNF02 | Sem alteração de banco de dados, `program.cs` ou `ApplicationDbContext`. |
| RNF03 | Requisição `GET` idempotente, sem efeitos colaterais. |
| RNF04 | Saída HTML escapada (proteção contra XSS); busca sem SQL/regex (sem injeção/ReDoS). |
| RNF05 | Interface responsiva usando Bootstrap já presente. |
| RNF06 | Mensagens e rótulos em português, seguindo o padrão do projeto. |
| RNF07 | Desempenho: filtragem O(n) em memória; adequada à escala atual da frota. |
| RNF08 | Código segue a convenção atual (controller MVC, models em `EasyVan.Models`, validação por Data Annotations). |

## 4. Entrada (parâmetros da query string)

| Parâmetro | Tipo | Obrigatório | Padrão | Restrições |
|---|---|---|---|---|
| `busca` | texto | não | vazio | até 100 caracteres após `Trim` |
| `status` | texto | não | vazio (= todos) | igual (sem caixa) a um status existente |
| `capacidadeMin` | inteiro | não | vazio | 0 a 1000 |
| `capacidadeMax` | inteiro | não | vazio | 0 a 1000; deve ser ≥ `capacidadeMin` |

Exemplo: `/Vans?busca=centro&status=Operacional&capacidadeMin=12`

## 5. Saída

Página HTML (`200 OK`) contendo:

1. Formulário preenchido com os valores recebidos;
2. Mensagens de validação (se houver);
3. Texto "Exibindo X de Y vans." (X = resultados, Y = total de vans);
4. Tabela com as vans que atendem aos critérios, **mesmas colunas e ordem de antes**, ou linha "Nenhuma van encontrada…".

Resposta é sempre `200`; erros de entrada **não** geram 400/404/500.

## 6. Comportamento da busca

1. `busca` sofre `Trim`; vazia/espaços ⇒ ignorada.
2. Texto e dados são normalizados: minúsculas e **sem acentos** (decomposição Unicode FormD removendo marcas diacríticas).
3. O texto é dividido em termos por espaço; cada termo deve ser **substring** de pelo menos um dos campos
   `Plate`, `Driver`, `Route`, `Description` (equivale a "E" entre termos, "OU" entre campos).
4. Para a placa, considera-se também a versão sem hífen (`ABC1234`).
5. Caracteres como `%`, `_`, `*`, `'` são literais.
6. A ordem original é preservada.

## 7. Comportamento dos filtros

| Filtro | Efeito |
|---|---|
| Status | Mantém vans cujo `Status` seja igual (OrdinalIgnoreCase) ao selecionado. |
| Capacidade mín. | Mantém vans com `Capacity >= mín`. |
| Capacidade máx. | Mantém vans com `Capacity <= máx`. |
| Combinação | Interseção de todos os critérios válidos informados, incluindo a busca. |

## 8. Regras de validação

| Situação | Resultado |
|---|---|
| `busca` > 100 caracteres | Mensagem "A busca deve ter no máximo 100 caracteres."; busca ignorada. |
| `status` fora da lista | Mensagem "Status inválido."; status ignorado. |
| `capacidadeMin`/`Max` não inteiro, negativo, > 1000 ou estouro de int | Mensagem "A capacidade mínima/máxima deve ser um número inteiro entre 0 e 1000."; campo ignorado. |
| `capacidadeMin` > `capacidadeMax` | Mensagem "A capacidade mínima não pode ser maior que a máxima."; **ambos** ignorados. |
| Qualquer critério vazio | Não filtra, sem mensagem. |

## 9. Casos de uso

**UC01 — Buscar van por texto.** Ator: usuário. Pré-condição: estar em `/Vans`.
Fluxo: digita termo → Filtrar → vê resultados. Alternativo: sem resultados → vê mensagem e link "Limpar filtros".

**UC02 — Filtrar por status.** Fluxo: escolhe status → Filtrar → vê apenas vans naquele status.

**UC03 — Filtrar por capacidade.** Fluxo: informa mín e/ou máx → Filtrar. Alternativo: mín > máx → erro, filtro ignorado.

**UC04 — Combinar critérios.** Fluxo: usa busca + status + capacidade → vê a interseção.

**UC05 — Limpar filtros.** Fluxo: clica "Limpar filtros" → `/Vans` com lista completa.

**UC06 — Compartilhar consulta.** Fluxo: copia a URL filtrada → outra pessoa abre → vê o mesmo resultado.

## 10. Critérios de aceitação

Ver CA01–CA14 em `/plan/02-planejamento-filtros-e-busca.md` e a matriz de testes (T01…) em `/plan/03-plano-de-testes.md`.

## 11. Fora de escopo

Paginação, ordenação por coluna, busca em `Usuarios`, migração de Vans para EF Core, autenticação/autorização, busca por horário (`Schedule`).

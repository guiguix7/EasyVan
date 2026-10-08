# 02 — Planejamento da feature "Filtros e Busca"

> Base: análise em `01-analise-do-projeto.md`. Especificação formal em `/spec/filtros-e-busca.md`.
> Testes em `03-plano-de-testes.md`.

## 1. Objetivo

Permitir que o usuário **pesquise por texto** e **filtre** a listagem de Vans (`/Vans`) por status e faixa de
capacidade, de forma combinável, rápida e compartilhável por URL.

## 2. Problema que resolve

Hoje `Vans/Index` mostra sempre a frota inteira. Conforme a frota cresce, localizar uma van (pela placa, motorista,
rota) ou responder perguntas operacionais ("quais estão em manutenção?", "quais comportam 12+ passageiros?")
exige leitura manual da tabela.

## 3. Usuário

Quem consulta a listagem de vans: administrador (gestão da frota), motorista e aluno (consulta de rota/status).
O projeto ainda **não tem autenticação/autorização por rota**; portanto a feature não cria restrição de perfil
(mesmo comportamento atual de `/Vans`).

## 4. Regras de negócio

| ID | Regra |
|---|---|
| RN01 | Busca textual considera **Placa, Motorista, Rota e Descrição**. |
| RN02 | Busca ignora maiúsculas/minúsculas e **acentos** (`joao` encontra `João`; `manutencao` encontra `manutenção`). |
| RN03 | Busca com várias palavras exige que **todas** apareçam (E lógico), em qualquer um dos campos. |
| RN04 | Placa pode ser digitada com ou sem hífen (`abc1234` ≡ `ABC-1234`). |
| RN05 | Filtro **Status**: igualdade exata (sem diferenciar caixa) com um dos status existentes. Opções do dropdown são geradas dos dados reais. |
| RN06 | Filtro **Capacidade**: mínima e máxima, **inclusivas**, ambas opcionais. |
| RN07 | Todos os critérios informados são combinados com **E** lógico. |
| RN08 | Critério vazio/em branco = **não filtra**. Nenhum critério = lista completa (comportamento atual). |
| RN09 | Critério **inválido** não derruba a página: mostra mensagem e **ignora só aquele critério**; os demais continuam valendo. |
| RN10 | A ordem dos resultados é a mesma da listagem original (por `Id`). |

## 5. Critérios de aceitação

- CA01: `/Vans` sem parâmetros exibe as 3 vans, como antes.
- CA02: Buscar `maria` exibe somente a van DEF-5678.
- CA03: Buscar `joao` (sem acento) exibe a van ABC-1234.
- CA04: Buscar `abc1234` exibe a van ABC-1234.
- CA05: Buscar `centro silva` exibe vans cuja rota/motorista/descrição contenham ambas as palavras.
- CA06: Filtrar status `Manutenção` exibe somente DEF-5678.
- CA07: Capacidade mínima 12 exibe ABC-1234 e TUF-6769; máxima 10 exibe DEF-5678; min 10 e max 12 exibe ABC-1234 e DEF-5678.
- CA08: Combinar busca + status + capacidade aplica todos juntos.
- CA09: Sem resultados, aparece "Nenhuma van encontrada…" com link para limpar filtros.
- CA10: Mínima > máxima, números negativos, texto não numérico, status inexistente ou busca > 100 caracteres mostram mensagem em português, sem erro 500.
- CA11: Os valores digitados permanecem nos campos após a pesquisa.
- CA12: A página mostra "Exibindo X de Y vans".
- CA13: Botão "Limpar filtros" volta a `/Vans`. "Detalhes" continua funcionando.
- CA14: Valores digitados são exibidos escapados (sem XSS).

## 6. Fluxo de utilização

1. Usuário abre **Vans** no menu → vê a lista completa e o formulário de filtros vazio.
2. Digita um termo e/ou escolhe status e/ou capacidade → clica **Filtrar** (ou Enter).
3. Navegador faz `GET /Vans?busca=...&status=...&capacidadeMin=...&capacidadeMax=...`.
4. Controller valida, filtra e renderiza a mesma tela com resultados, contador e campos preenchidos.
5. Sem resultados → mensagem vazia; erro de validação → alerta em vermelho + critério inválido ignorado.
6. **Limpar filtros** → `GET /Vans`.

Usa **GET** (e não POST) porque é consulta sem efeito colateral: a URL pode ser favoritada/compartilhada,
o botão "Voltar" funciona e não é necessário token anti-forgery.

## 7. Alterações no front-end (`Views/Vans/Index.cshtml`)

- `@model` passa de `IEnumerable<Van>` para `VanIndexViewModel`.
- Formulário `method="get"` com: campo de busca, `<select>` de status (opção "Todos" + status vindos do ViewModel), campos numéricos de capacidade mín/máx (`type="number" min="0"`), botões **Filtrar** e **Limpar filtros**.
- `asp-validation-summary` (ou bloco equivalente) para exibir as mensagens de validação.
- Contador "Exibindo X de Y vans".
- Linha "Nenhuma van encontrada" (com `colspan`) quando a lista filtrada estiver vazia.
- Tabela e badges de status **preservados** (sem duplicar markup).
- Botão "Atualizar" passa a preservar os filtros ativos.
- Somente Bootstrap já existente; **nenhum JavaScript novo** e nenhuma dependência nova.

## 8. Alterações no back-end

- **`Models/VanFiltro.cs` (novo)**: parâmetros do filtro com Data Annotations (`StringLength` na busca, `Range` na capacidade) e propriedade auxiliar `TemFiltroAtivo`.
- **`Models/VanIndexViewModel.cs` (novo)**: `Filtro`, `Vans` (resultado), `TotalGeral`, `StatusDisponiveis`.
- **`Controllers/VansController.cs`**:
  - `Index([FromQuery] VanFiltro filtro)`;
  - validação (mensagens em português; substitui as mensagens padrão em inglês do model binder);
  - método privado estático `AplicarFiltros(...)` + `NormalizarTexto(...)` (remove acentos/caixa);
  - `Details` e a lista `_vans` permanecem inalterados.

## 9. Banco de dados

**Nenhuma alteração.** As vans vêm da lista estática em memória. A tabela `Vans` (vazia) e `ApplicationDbContext` não são tocados.
Se no futuro as Vans migrarem para EF Core, o `AplicarFiltros` pode receber `IQueryable<Van>`; a normalização de
acentos precisaria virar `EF.Functions.Like`/collation, pois não é traduzível para SQL (anotado nas melhorias futuras).

## 10. Rotas / API

| Método | Rota | Parâmetros (query) | Retorno |
|---|---|---|---|
| GET | `/Vans` (ou `/Vans/Index`) | `busca`, `status`, `capacidadeMin`, `capacidadeMax` (todos opcionais) | HTML (200) |

Não há endpoint JSON novo: o projeto é MVC server-rendered e `API/` e `Backend/` são placeholders vazios.
A rota padrão `{controller=Home}/{action=Index}/{id?}` já atende; **nenhuma mudança em `program.cs`**.

## 11. Validações

| Campo | Regra | Mensagem |
|---|---|---|
| busca | até 100 caracteres (após `Trim`) | "A busca deve ter no máximo 100 caracteres." |
| status | deve ser um dos status existentes | "Status inválido." |
| capacidadeMin / Max | inteiro entre 0 e 1000 | "A capacidade mínima/máxima deve ser um número inteiro entre 0 e 1000." |
| mín × máx | `min <= max` | "A capacidade mínima não pode ser maior que a máxima." |

Segurança: busca usa `string.Contains` em memória (sem SQL, sem `LIKE`, sem regex ⇒ sem injeção nem ReDoS);
saída HTML escapada pelo Razor.

## 12. Tratamento de erros

- Falha de binding (ex.: `capacidadeMin=abc` ou número gigante) → mensagem amigável; critério ignorado.
- Nenhum erro de validação resulta em 500 ou em `NotFound`.
- Lista vazia não é erro: é um estado normal com mensagem.
- Exceções inesperadas continuam caindo no `UseExceptionHandler("/Home/Error")` já configurado.

## 13. Casos extremos

- Busca só com espaços ⇒ tratada como vazia.
- Espaços múltiplos entre palavras ⇒ ignorados.
- Parâmetro repetido na URL (`?busca=a&busca=b`) ⇒ o model binder usa apenas o primeiro valor; sem erro.
- Caracteres especiais (`%`, `_`, `<`, `'`, `"`) ⇒ tratados como texto literal.
- Status com caixa diferente (`manutenção` minúsculo) ⇒ aceito.
- `capacidadeMin=0` é válido e equivale a "sem mínimo" na prática.
- Lista de status vazia (se `_vans` ficar vazia) ⇒ dropdown só com "Todos".

## 14. Passos de implementação (para outro desenvolvedor)

1. Criar `Models/VanFiltro.cs` e `Models/VanIndexViewModel.cs`.
2. Alterar `VansController.Index` conforme a seção 8.
3. Alterar `Views/Vans/Index.cshtml` conforme a seção 7.
4. Atualizar `README.md`.
5. Executar o plano de testes (`03-plano-de-testes.md`).

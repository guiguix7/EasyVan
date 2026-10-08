# 03 — Plano de testes: Filtros e Busca

O projeto **não possui testes automatizados** (nenhum projeto xUnit/NUnit/MSTest), então os testes abaixo são
**manuais**, executados pela URL ou pelo formulário de `/Vans`. Os dados de teste são os 3 registros fixos
do `VansController`:

| Id | Placa | Motorista | Cap. | Rota | Status |
|---|---|---|---|---|---|
| 1 | ABC-1234 | João Silva | 12 | Centro - Universidade | Operacional |
| 2 | DEF-5678 | Maria Oliveira | 10 | Zona Leste - Centro | Manutenção |
| 3 | TUF-6769 | Aurudo da Silva | 15 | Centro - Distrito Industrial | Operacional |

> **Estado da verificação:** o ambiente em que a feature foi gerada **não tinha o SDK .NET** (`dotnet`) nem acesso à rede,
> então o projeto **não foi compilado nem executado lá**. Os resultados esperados abaixo foram conferidos por
> um port fiel do algoritmo (mesma normalização Unicode, mesma ordem de validação) contra os dados reais: 40/40 casos coerentes.
> **Falta rodar `dotnet build` e a tabela abaixo no seu computador** (passo a passo na seção 1).

## 1. Como executar

```powershell
dotnet restore
dotnet build            # deve terminar sem erros
dotnet run              # abrir http://localhost:5256/Vans
```

## 2. Smoke test (projeto inicia)

| # | Verificação | Esperado |
|---|---|---|
| S1 | `dotnet build` | 0 erros |
| S2 | `dotnet run` e abrir `/` | Tela de login carrega |
| S3 | Abrir `/Vans` | Formulário de filtros + 3 vans + "Exibindo 3 de 3 vans." |

## 3. Busca, filtros e combinações

Sufixo da URL após `/Vans`. "Resultado" = placas exibidas. "Msg" = alerta de validação.

| ID | Cenário | URL | Resultado | Msg |
|---|---|---|---|---|
| T01 | Sem filtros | `/Vans` | ABC-1234, DEF-5678, TUF-6769 | — |
| T02 | Busca por motorista | `?busca=maria` | DEF-5678 | — |
| T03 | Busca sem acento | `?busca=joao` | ABC-1234 | — |
| T04 | Maiúsculas + acento | `?busca=JO%C3%83O` | ABC-1234 | — |
| T05 | Placa sem hífen | `?busca=abc1234` | ABC-1234 | — |
| T06 | Placa com hífen | `?busca=ABC-1234` | ABC-1234 | — |
| T07 | Busca por rota | `?busca=zona%20leste` | DEF-5678 | — |
| T08 | Busca por descrição | `?busca=preventiva` | DEF-5678 | — |
| T09 | Descrição sem acento | `?busca=manutencao` | DEF-5678 | — |
| T10 | Várias palavras (E lógico) | `?busca=centro%20silva` | ABC-1234, TUF-6769 | — |
| T11 | Espaços extras | `?busca=%20%20centro%20%20%20%20silva%20` | ABC-1234, TUF-6769 | — |
| T12 | Busca sem resultado | `?busca=xyz` | nenhuma + "Nenhuma van encontrada para os filtros informados." + link "Limpar filtros" | — |
| T13 | Busca só espaços | `?busca=%20%20%20` | as 3 | — |
| T14 | Busca vazia | `?busca=` | as 3 | — |
| T15 | Caractere `%` literal | `?busca=%25` | nenhuma | — |
| T16 | HTML na busca (XSS) | `?busca=%3Cscript%3Ealert(1)%3C/script%3E` | nenhuma; texto aparece **escapado** no campo, sem executar | — |
| T17 | Status Operacional | `?status=Operacional` | ABC-1234, TUF-6769 | — |
| T18 | Status Manutenção | `?status=Manuten%C3%A7%C3%A3o` | DEF-5678 | — |
| T19 | Status em minúsculas | `?status=manuten%C3%A7%C3%A3o` | DEF-5678 | — |
| T20 | Status inexistente | `?status=Quebrada` | as 3 (filtro ignorado) | "Status inválido." |
| T21 | Capacidade mín. | `?capacidadeMin=12` | ABC-1234, TUF-6769 | — |
| T22 | Capacidade máx. | `?capacidadeMax=10` | DEF-5678 | — |
| T23 | Faixa | `?capacidadeMin=10&capacidadeMax=12` | ABC-1234, DEF-5678 | — |
| T24 | Limite inclusivo | `?capacidadeMin=12&capacidadeMax=12` | ABC-1234 | — |
| T25 | Mín. sem resultado | `?capacidadeMin=16` | nenhuma + mensagem de vazio | — |
| T26 | Mín. = 0 | `?capacidadeMin=0` | as 3 | — |
| T27 | Mín. > máx. | `?capacidadeMin=15&capacidadeMax=10` | as 3 (ambos ignorados) | "A capacidade mínima não pode ser maior que a máxima." |
| T28 | Mín. negativa | `?capacidadeMin=-1` | as 3 | "A capacidade mínima deve ser um número inteiro entre 0 e 1000." |
| T29 | Máx. não numérica | `?capacidadeMax=abc` | as 3 | "A capacidade máxima deve ser um número inteiro entre 0 e 1000." |
| T30 | Mín. acima do limite | `?capacidadeMin=1001` | as 3 | msg de capacidade mínima |
| T31 | Estouro de inteiro | `?capacidadeMin=99999999999` | as 3 | msg de capacidade mínima (em português, sem erro 500) |
| T32 | Decimal | `?capacidadeMin=1.5` | as 3 | msg de capacidade mínima |
| T33 | Busca + status | `?busca=silva&status=Operacional` | ABC-1234, TUF-6769 | — |
| T34 | Busca + status (vazio) | `?busca=silva&status=Manuten%C3%A7%C3%A3o` | nenhuma | — |
| T35 | Status + capacidade | `?status=Operacional&capacidadeMin=13` | TUF-6769 | — |
| T36 | Todos combinados | `?busca=centro&status=Operacional&capacidadeMin=12&capacidadeMax=14` | ABC-1234 | — |
| T37 | Válido + inválido | `?busca=maria&capacidadeMin=-5` | DEF-5678 (busca vale, capacidade ignorada) | msg de capacidade mínima |
| T38 | Busca com 101 caracteres | `?busca=` + 101×`a` | as 3 | "A busca deve ter no máximo 100 caracteres." |
| T39 | Busca com 100 caracteres | `?busca=` + 100×`a` | nenhuma | — |
| T40 | 100 caracteres + espaços laterais | `?busca=%20` + 100×`a` + `%20` | nenhuma (limite medido após Trim) | — |

## 4. Comportamento de interface

| ID | Verificação | Esperado |
|---|---|---|
| U01 | Preencher busca + status + capacidades e clicar **Filtrar** | URL muda para `/Vans?busca=...`; campos permanecem preenchidos; select mostra o status escolhido |
| U02 | Pressionar Enter no campo de busca | Equivale a Filtrar |
| U03 | Clicar **Limpar filtros** | Vai para `/Vans`, campos vazios, 3 vans |
| U04 | Contador | "Exibindo X de 3 vans." coerente com a tabela |
| U05 | Copiar a URL filtrada e abrir em outra aba | Mesmo resultado |
| U06 | Botão **Atualizar** com filtros ativos | Recarrega mantendo os filtros |
| U07 | Layout em tela estreita (< 768 px) | Campos empilhados, sem rolagem horizontal da página |
| U08 | Opções do select | "Todos", "Manutenção", "Operacional" (ordem alfabética, sem repetição) |
| U09 | Navegador: digitar `-3` em capacidade | Validação nativa do navegador impede o envio (o servidor valida de qualquer forma — T28 pela URL) |

## 5. Regressão (funcionalidades antigas)

| ID | Verificação | Esperado |
|---|---|---|
| R01 | `/Vans` → **Detalhes** de cada van | Abre `/Vans/Details/{id}` com os dados corretos |
| R02 | `/Vans/Details/999` | 404 (como antes) |
| R03 | Badges de status | Verde para "Operacional", amarelo para os demais |
| R04 | Menu superior (Home, Detalhes, Vans, Cadastro, Usuários) | Todos os links abrem |
| R05 | Login válido e inválido | Redireciona por perfil / mostra "Credenciais inválidas." |
| R06 | Cadastro de usuário e CRUD em `/Usuarios` | Funciona como antes (nenhum arquivo foi alterado) |
| R07 | Banco `easyvan.db` | Inalterado; app inicia normalmente |

## 6. Testes automatizados

Não foram criados: o projeto não tem projeto de testes e adicionar um exige restaurar pacotes NuGet (xUnit),
o que não foi possível sem rede. Se a disciplina exigir, o próximo passo natural é um projeto xUnit que
chame `AplicarFiltros`/`NormalizarTexto` (hoje `private static` em `VansController`; bastaria extraí-los para uma
classe `internal`/`public`, p.ex. `Models/VanFiltroExtensions`).

# 04 — Redesign de produto, UI/UX e frontend

> Base: auditoria do repositório em 2026-11-03, `AGENTS.md`, `PRODUCT.md`, `DESIGN.md`, relatório de produto
> anexado e inspeção do app em execução. Este documento registra o diagnóstico e a direção **antes** da
> implementação; o resultado final é conferido em `DESIGN.md`.

## 1. Inventário auditado

| Área | Situação encontrada |
|---|---|
| Shell autenticado | Sidebar clara, topbar e conteúdo. Já migrado para a identidade petróleo/âmbar, mas com overflow no topbar em 850 px e hierarquia genérica. |
| Login / cadastro | Layout dividido com marca. Funcional, porém com card flutuante, tipografia pesada e pouca relação com a operação. |
| Dashboard admin | Seis cards de métrica + atalhos empilhados. Sem leitura operacional da frota e com densidade desequilibrada no tablet. |
| Vans | Listagem e filtros funcionais, bem estruturados; tabela e cards mobile ainda com acabamento básico. |
| Motoristas, alunos, rotas, viagens, usuários | Listagens padronizadas; formulários e detalhes ainda em Bootstrap cru, com cartões estreitos, `shadow-sm` e rodapés de ação inconsistentes. |
| Detalhes | Vans e viagens usam cards de Bootstrap; `Motoristas/Details`, `Alunos/Details` e `Rotas/Details` **não existem**, apesar de os links estarem nas listagens. |
| Estados | Vazio e erro existem, mas sem unidade. `Error.cshtml` está em inglês e no layout antigo. `AcessoRestrito` usa emoji como ícone. `Privacy` é o texto padrão do template, em inglês. |
| Acessibilidade | Skip link e foco presentes. Faltam `aria-invalid`, associação de ajuda/erro em formulários, ícones consistentes (há emoji e setas textuais) e revisão de overflow. |
| Design system | Tokens duplicados no CSS, estilos legados do template (fundo em gradiente, `PT Serif`, `login-card`) e componentes ad hoc repetidos em cada view. |

## 2. Direção visual — “Rota clara”

Refinamento da identidade existente, não substituição. O logo, o azul-petróleo e o âmbar permanecem.

- **Mundo:** uma prancheta de operação de transporte. Fundo levemente esverdeado, superfícies brancas, linhas finas, tipografia de trabalho e uma única cor de ação. O âmbar marca o ponto ativo da rota, nunca compete com o primário.
- **Assinatura:** a **linha de rota** — traço com paradas — usada onde há sequência real (andamento da viagem, etapas de presença) e a **placa** tratada como objeto (chip com tracking e números tabulares).
- **Marca:** sidebar com logo em placa branca; login com painel petróleo e a mesma linha de rota ligando os módulos reais (Vans, Rotas, Viagens).
- **Tipografia:** pilha nativa do sistema, corpo 16 px, escala fixa de 12/13/14/16/18/22/28 px. Peso 600–650 em títulos, `-0.02em` no máximo. Números tabulares em placas, contadores e horários.
- **Cor:** estratégia contida. Petróleo para ação e navegação ativa; âmbar para foco e marcador; verde/âmbar/vermelho apenas para estado, sempre com rótulo.
- **Forma:** raio 10 px em controles, 14 px em superfícies. Elevação só em overlay; superfícies usam borda. Sem gradientes decorativos, sem vidro, sem sombra de cartão.
- **Densidade:** desktop do gestor com tabela de 56–60 px por linha e ações visíveis; mobile com cartões de registro e botões de 44 px.
- **Movimento:** 150–200 ms em hover/focus/active; nenhuma coreografia de carregamento.

## 3. Arquitetura de interface

- Sidebar agrupada por domínio: **Operação** (visão geral, viagens), **Cadastros** (vans, motoristas, alunos, rotas), **Administração** (usuários).
- Topbar com breadcrumb curto e truncamento seguro; sem duplicar o bloco de usuário do rodapé da sidebar.
- Cabeçalho de página com título, descrição de uma linha e ação primária.
- Listagens: barra de contexto (contagem/ação), superfície única com tabela no desktop e cartões no mobile.
- Formulários: campos agrupados por assunto, grid de duas colunas, painel lateral de orientação; ações no rodapé do formulário.
- Detalhes: cabeçalho de registro + grade de dados + ações contextuais.
- Estados vazios explicam o que falta e oferecem a próxima ação real.

## 4. Componentes a consolidar

- `_IconSprite` + `_Icon`: conjunto único de ícones SVG, traço 1,8 px, 24×24.
- `_StatusBadge`: status de van, viagem, motorista/aluno/rota e perfil, com rótulo e ponto.
- `StatusUi`: normalização de rótulo e variante visual em um só lugar.
- `_PageHeader` não será abstraído: as ações variam por página e um partial com HTML livre reduziria legibilidade.
- CSS em camadas: tokens → base → shell → componentes → páginas → responsivo → acessibilidade.

## 5. Correções funcionais incluídas

- Criar as views `Motoristas/Details`, `Alunos/Details` e `Rotas/Details`, hoje ausentes e referenciadas pelas listagens.
- Traduzir e redesenhar `Error`, `AcessoRestrito` e `Privacy` (LGPD), sem inventar compromissos legais.
- Não alterar contratos de controller, rotas, banco ou autorização, exceto onde a view depende de dados já existentes.

## 6. Fora de escopo

- Novas entidades, multi-tenancy, GPS, notificações, pagamentos e reservas.
- Alteração do cadastro público por perfil (registrado como dívida de segurança).
- Migração de stack ou dependências novas.

## 7. Verificação prevista

- `dotnet build` após cada bloco C#/Razor.
- Inspeção no navegador em desktop e mobile dos fluxos: login, dashboard, listagens, filtros, formulários, detalhes, viagens e estados vazios/erro.
- Verificação de overflow, foco por teclado, contraste e `prefers-reduced-motion`.

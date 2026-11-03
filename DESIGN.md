# EasyVan — Design System e direção visual

> **Status:** direção visual documentada e aplicada parcialmente na branch \`design/redesign-premium-ui\`.
> **Base:** marca e código existentes em \`wwwroot/images/logo.png\`, \`wwwroot/css/site.css\`, Razor Views, \`AGENTS.md\` e relatório de produto EasyVan.
> **Escopo:** identidade, padrões de interface, navegação, componentes, responsividade e acessibilidade do frontend MVC.

## 1. Princípio de design

O EasyVan é uma ferramenta operacional para organizar transporte por vans. A interface deve permitir que gestores consultem e mantenham cadastros com eficiência, motoristas encontrem informações de viagens rapidamente e passageiros localizem os dados de seu transporte sem navegação complexa.

O design prioriza leitura rápida, previsibilidade, clareza de estado e consistência entre telas. A sofisticação vem da hierarquia, proporção, espaçamento, legibilidade e tratamento dos estados — não de efeitos decorativos.

## 2. Identidade visual observada

A identidade visual não foi criada do zero. O logo já presente em \`wwwroot/images/logo.png\` associa o nome EasyVan a um veículo e elementos de rota, usando azul-petróleo e âmbar. O CSS existente já registrava uma paleta coerente com o logo. A implementação consolida essa direção e remove conflitos dos estilos antigos.

### Tokens de cor

| Token CSS | Valor | Uso |
|---|---|---|
| \`--ev-petroleo\` | \`#0F4C5C\` | Navegação, ação primária e identidade |
| \`--ev-petroleo-escuro\` | \`#0B3744\` | Hover e ênfase |
| \`--ev-ambar\` | \`#F5A524\` | Destaque e acento |
| \`--ev-fundo\` | \`#F6F7F9\` | Fundo da aplicação |
| \`--ev-superficie\` | \`#FFFFFF\` | Conteúdo e painéis |
| \`--ev-texto\` | \`#1F2933\` | Texto principal |
| \`--ev-texto-secundario\` | \`#52616B\` | Texto auxiliar |
| \`--ev-linha\` | \`#DCE3E7\` | Divisórias e bordas |
| \`--ev-verde\` | \`#1B873F\` | Estado operacional |
| \`--ev-vermelho\` | \`#C0392B\` | Situação de atenção/manutenção |

Status não deve ser comunicado somente pela cor: mantenha sempre o rótulo textual.

## 3. Tipografia

A aplicação utiliza a pilha sans-serif nativa do sistema operacional para preservar legibilidade, evitar dependência externa e manter carregamento rápido. O corpo padrão é 16px. Títulos usam peso e escala moderados, com hierarquia clara entre título de página, título de seção, rótulo de campo e metadados.

Números operacionais devem utilizar algarismos tabulares quando isso melhorar comparação. Texto de formulário e tabela não deve ser reduzido para acomodar layouts; reorganize a composição em telas estreitas.

## 4. Forma e composição

- Base de espaçamento: múltiplos de 4px, com passos usuais de 8, 12, 16, 24 e 32px.
- Raio de controles: 8px.
- Raio de painéis e superfícies: 12px.
- Bordas discretas em superfícies; sombras reservadas a contextos em que indiquem elevação.
- Ações primárias em azul-petróleo; âmbar deve permanecer acento, sem competir com ações principais.
- Evitar cartões aninhados e usar painéis somente quando ajudarem a agrupar informação relacionada.

## 5. Navegação e arquitetura de interface

### Desktop

A navegação principal utiliza uma sidebar clara e persistente, com marca no topo, opções de acordo com o perfil e identificação da conta na parte inferior. A área de conteúdo ocupa a largura restante e possui cabeçalho contextual.

### Mobile

A sidebar transforma-se em offcanvas Bootstrap acionado por botão com nome acessível. O conteúdo passa a uma coluna única. Listagens com muitos campos devem apresentar cartões compactos quando a tabela comprometer a leitura; os fluxos essenciais permanecem acessíveis.

### Perfis e navegação existente

- **Administrador:** Visão geral, Vans, Motoristas, Alunos, Rotas, Viagens e Usuários.
- **Motorista:** Meu painel e Viagens.
- **Aluno:** Início e Minha van.

A navegação respeita as rotas existentes; não cria páginas ou capacidades ainda não implementadas.

## 6. Componentes e padrões

- **Botões:** primário (azul-petróleo), secundário neutro, contorno e perigo. Ações devem possuir texto claro.
- **Campos:** rótulos visíveis, altura confortável, foco evidente e mensagens de validação junto ao campo.
- **Tabelas:** cabeçalho com contraste, densidade consistente, ações alinhadas e alternativa mobile quando a quantidade de colunas exigir.
- **Filtros:** controles com rótulos, filtros combináveis, estado ativo identificável e ação explícita para limpar.
- **Estados vazios:** explicar o que não foi encontrado e indicar uma próxima ação real.
- **Status:** rótulo textual com tratamento visual redundante.
- **Alertas:** texto direto, motivo identificável e sem depender somente de cor.

## 7. Responsividade

A implementação utiliza os breakpoints do Bootstrap existentes. A adaptação deve considerar o conteúdo, não apenas reduzir larguras.

- Até 767px: composição de uma coluna, ações com área de toque adequada e cards móveis para vans.
- 768px a 991px: composição intermediária, indicadores em duas colunas e navegação em offcanvas.
- A partir de 992px: sidebar persistente e maior densidade de informação operacional.

Alvos interativos devem buscar pelo menos 44px de altura/largura quando aplicável. Evitar overflow horizontal na página; tabelas excepcionalmente largas podem ter rolagem própria quando não houver alternativa fiel.

## 8. Acessibilidade e movimento

- Idioma do documento: \`pt-BR\`.
- Usar semântica HTML, títulos hierárquicos e labels associados aos controles.
- Preservar navegação por teclado e foco visível.
- Não depender somente de cor para status e mensagens.
- Mensagens de validação devem permanecer próximas aos campos e não apagar os valores digitados.
- Respeitar \`prefers-reduced-motion\`.
- Manter contraste compatível com WCAG 2.2 AA; a validação de contraste computado e os testes com leitor de tela precisam ocorrer no navegador.

## 9. Restrições funcionais

Este documento descreve a interface, não amplia o escopo funcional. Não apresentar como existentes funcionalidades de GPS em tempo real, reservas, notificações, cobrança recorrente, multi-tenancy ou integração externa sem implementação e validação correspondentes.

Dados apresentados no dashboard devem vir do modelo fornecido pelo backend. A interface não deve criar métricas, viagens ou associações de passageiros fictícias.

## 10. Verificação pendente

A branch foi editada por meio da API do GitHub, sem checkout local disponível neste ambiente. Portanto, ainda estão pendentes:

- \`dotnet build\` e testes automatizados;
- execução do ASP.NET e inspeção visual no navegador;
- verificação de viewport desktop, tablet e mobile;
- contraste computado, navegação por teclado e leitor de tela;
- detecção automatizada Impeccable e revisão visual final.

Essas verificações não devem ser consideradas aprovadas até serem executadas no ambiente de desenvolvimento.

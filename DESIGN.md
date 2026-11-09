# EasyVan: sistema visual

**Status:** direção visual consolidada e aplicada ao frontend MVC. A validação visual completa ainda depende de revisão no navegador em todos os breakpoints.

## Direção

O EasyVan é uma ferramenta operacional de transporte, não um ERP genérico. A interface prioriza leitura rápida, previsibilidade, clareza de estado e consistência. A sofisticação vem da hierarquia, proporção, espaçamento e legibilidade, não de efeitos decorativos.

## Identidade

A marca usa o logo existente em `wwwroot/images/logo.png`, com azul-petróleo e âmbar como referências principais.

| Token | Valor | Uso |
|---|---|---|
| `--ev-petroleo` | `#0F4C5C` | identidade, navegação e ação primária |
| `--ev-petroleo-escuro` | `#0B3744` | hover e ênfase |
| `--ev-ambar` | `#F5A524` | destaque e acento |
| `--ev-fundo` | `#F6F7F9` | fundo da aplicação |
| `--ev-superficie` | `#FFFFFF` | conteúdo e painéis |
| `--ev-texto` | `#1F2933` | texto principal |
| `--ev-texto-secundario` | `#52616B` | texto auxiliar |
| `--ev-texto-tenue` | `#687781` | metadados e rótulos de apoio |
| `--ev-linha` | `#DCE3E7` | bordas e divisórias |
| `--ev-verde` | `#1B873F` | estado operacional |
| `--ev-vermelho` | `#C0392B` | atenção e manutenção |

Status nunca deve ser comunicado somente pela cor: use sempre o rótulo textual.
O esquema de cores nativo do navegador acompanha o tema selecionado; placeholders usam a cor de texto auxiliar para manter contraste legível.

## Tipografia e forma

- Usar a pilha sans-serif nativa para preservar legibilidade e reduzir dependências externas.
- Manter corpo de 16 px e hierarquia clara entre título, seção, campo e metadado.
- Usar espaçamentos em múltiplos de 4 px, com passos usuais de 8, 12, 16, 24 e 32 px.
- Usar controles com raio de 8 px e painéis com raio de até 12 px.
- Reservar sombras para contextos que indiquem elevação.
- Manter o âmbar como acento, sem competir com ações primárias.

## Navegação por perfil

- **Administrador:** visão geral, vans, motoristas, alunos, rotas, viagens e usuários.
- **Motorista:** painel e viagens.
- **Aluno:** início e consulta da própria van.

A navegação deve respeitar as rotas implementadas e nunca sugerir capacidades fora do produto atual.

## Componentes

- Botões com texto claro para comandos importantes.
- Campos com labels visíveis, foco evidente e mensagens de validação próximas.
- Tabelas com cabeçalho contrastante e ações alinhadas.
- Filtros combináveis, estado ativo identificável e ação explícita para limpar.
- Estados vazios com explicação e próxima ação real.
- Alertas diretos, sem depender somente da cor.

## Responsividade

- Até 767 px: uma coluna, controles grandes e cartões quando tabelas prejudicarem a leitura.
- De 768 a 991 px: composição intermediária e navegação adaptada.
- A partir de 992 px: maior densidade de informação e navegação persistente.
- Em telas compactas e dispositivos com toque, ações compactas mantêm alvos de pelo menos 44 px.

Evite overflow horizontal da página. Tabelas excepcionalmente largas podem ter rolagem própria. Alvos interativos devem buscar pelo menos 44 px.

## Acessibilidade

Usar semântica HTML, títulos hierárquicos, labels associados, foco visível, navegação por teclado, contraste compatível com WCAG 2.2 AA e suporte a `prefers-reduced-motion`. Consulte [ACCESSIBILITY.md](ACCESSIBILITY.md).

## Limites funcionais

O design não deve inventar GPS, reservas, notificações, cobrança, multi-tenancy ou integrações externas. Informações de dashboard devem vir dos modelos do backend.

## Verificação pendente

A compilação e a execução local já foram validadas. Ainda devem ser feitas, antes de uma entrega pública, a inspeção visual em desktop, tablet e mobile, a verificação de contraste computado, os testes de teclado e a revisão com leitor de tela.

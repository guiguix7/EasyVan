# Acessibilidade

O EasyVan é uma aplicação web em Razor Views, com interface em português do Brasil e uso previsto em desktop e celular. A referência de projeto é WCAG 2.2 nível AA.

## Compromissos de interface

- Usar HTML semântico, títulos em ordem e `lang="pt-BR"`.
- Manter labels visíveis e associados aos campos.
- Exibir erros de validação junto ao campo correspondente.
- Garantir foco visível e navegação por teclado.
- Usar rótulos textuais junto a cores para estados como operacional e manutenção.
- Buscar alvos de toque de pelo menos 44 px em fluxos de uso móvel.
- Em telas compactas e dispositivos com toque, botões compactos, ações de linha, opções da conta e controles de senha preservam área mínima de 44 px.
- Preservar leitura em telas estreitas sem exigir rolagem horizontal da página.
- Respeitar `prefers-reduced-motion` em animações e transições.
- Manter contraste suficiente entre texto, fundo, controles e estados.

## Perfis de uso

- O Administrador usa tabelas, filtros e formulários em sessões longas.
- O Motorista precisa de ações grandes e leitura rápida em celular.
- O Aluno precisa consultar sua informação de transporte sem acessar menus administrativos.

## Verificação

As regras acima orientam a implementação, mas não constituem uma certificação WCAG. Antes de uma entrega pública, valide as páginas principais com teclado, leitor de tela, zoom, contraste computado e viewports desktop e mobile.

Ao alterar uma View ou o CSS, execute `dotnet build` e revise também os estados vazio, erro, carregamento, foco e validação.

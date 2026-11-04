# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Três perfis usam o EasyVan no navegador:

- **Administrador / gestor:** passa boa parte do dia em desktop, mantendo frota, motoristas, alunos, rotas, viagens e contas. Precisa localizar registros rápido, comparar status e corrigir cadastros sem perder contexto.
- **Motorista:** usa principalmente celular, em pé ou dentro da van. Precisa abrir a viagem do dia, iniciar/finalizar o percurso e marcar presença com poucos toques.
- **Aluno / passageiro:** consulta a van, a rota e o horário do próprio transporte. Tem pouca familiaridade técnica e precisa de uma resposta direta, sem navegação extensa.

## Product Purpose

Centralizar a operação de transporte por vans em um único sistema: veículos, motoristas, alunos, rotas e viagens. O sucesso significa que o gestor encontra e mantém a informação sem planilhas paralelas, o motorista consegue operar a viagem no celular e o aluno entende onde e quando embarca.

## Positioning

O EasyVan é uma ferramenta operacional de transporte por vans, não um ERP genérico. A interface deve comunicar mobilidade e organização: placa, rota, horário, status da van e andamento da viagem são os objetos centrais, e não métricas financeiras ou dashboards abstratos.

## Operating Context

- Aplicação ASP.NET Core MVC renderizada no servidor; uso em desktop e celular.
- O gestor trabalha em sessões longas e alterna entre listagens, formulários e detalhes.
- O motorista opera em campo, com conectividade variável e alvos de toque grandes.
- O aluno consulta poucas informações e não deve enfrentar menus administrativos.
- O produto usa dados reais do banco SQLite: vans, motoristas, alunos, rotas, viagens e presenças. Não há dados de GPS, pagamento, reserva ou notificação em tempo real implementados.

## Capabilities and Constraints

Implementado e verificável no código:

- Login por cookie com três papéis: Administrador, Motorista e Aluno.
- CRUD de vans, motoristas, alunos e rotas (administração).
- Listagem e filtros de vans por texto, status e capacidade (`spec/filtros-e-busca.md`).
- Viagens com criação, início, fim e lista de presença.
- Painéis distintos por papel.
- Senhas com hash PBKDF2; cadastro público ainda permite escolher perfil (dívida de segurança registrada, não alterada nesta entrega).
- Stack fixa: Razor Views, Bootstrap 5, jQuery e CSS próprio. Sem SPA, sem dependências novas.

Fora de escopo desta entrega: multi-tenancy, notificações, GPS, pagamentos e novas entidades.

## Brand Commitments

- Nome **EasyVan** e logotipo existente em `wwwroot/images/logo.png` (van, rota, azul e laranja).
- Paleta já consolidada em `DESIGN.md`: azul-petróleo `#0F4C5C` como cor de identidade e ação, âmbar `#F5A524` como acento.
- Interface em português do Brasil.
- Status sempre com rótulo textual, nunca apenas cor.

## Evidence on Hand

- Logo raster oficial em `wwwroot/images/logo.png`.
- Três vans, três motoristas, três alunos e três rotas de referência em `Data/DbInitializer.cs`.
- Contas de demonstração apenas em Development: `admin@easyvan.com`, `motorista@easyvan.com`, `aluno@easyvan.com`, senha `123456`.
- Especificação de filtros em `spec/filtros-e-busca.md` e plano de testes em `plan/03-plano-de-testes.md`.
- Não existem depoimentos, clientes, preços, métricas de mercado ou estudos de caso. Nada disso deve ser inventado.

## Product Principles

1. **A operação primeiro.** Placa, rota, horário e status vêm antes de qualquer elemento decorativo.
2. **Uma ação principal por tela.** O usuário deve saber o que fazer em seguida sem procurar.
3. **Densidade com respiro.** O gestor precisa comparar muitos registros; o motorista precisa de alvos grandes. A composição muda de verdade entre desktop e celular.
4. **O estado é informação.** Vazio, erro, manutenção, viagem em andamento e presença precisam ser visíveis e compreensíveis.
5. **Familiaridade sem monotonia.** Padrões conhecidos de produto (tabela, formulário, navegação lateral) executados com precisão, não reinventados por estilo.

## Accessibility & Inclusion

- WCAG 2.2 AA como referência.
- Navegação completa por teclado, foco visível e vínculo entre rótulos, ajuda e erros.
- Alvos de toque de pelo menos 44 px nos fluxos de campo.
- Respeito a `prefers-reduced-motion`.
- Contraste verificado nos tokens de texto, botões, badges e estados.

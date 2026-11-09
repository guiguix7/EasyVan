# Produto EasyVan

## VisÃ£o

O EasyVan Ã© uma ferramenta operacional para organizar o transporte por vans. Seu objetivo Ã© reunir em um Ãºnico fluxo os cadastros, as rotas, as viagens e as presenÃ§as que hoje podem ficar espalhados em planilhas ou mensagens.

## UsuÃ¡rios

### Administrador

Trabalha principalmente no desktop e administra vans, motoristas, alunos, rotas, viagens e usuÃ¡rios. Precisa localizar registros rapidamente, comparar status e corrigir cadastros com seguranÃ§a.

### Motorista

Usa a aplicaÃ§Ã£o principalmente no celular. Precisa consultar suas viagens, iniciar e finalizar percursos e registrar presenÃ§as com poucos toques.

### Aluno

Consulta as informaÃ§Ãµes do prÃ³prio transporte, como van, rota e horÃ¡rio, sem navegar por Ã¡reas administrativas.

## Capacidades atuais

- autenticaÃ§Ã£o por cookie;
- autorizaÃ§Ã£o por Administrador, Motorista e Aluno;
- CRUD de usuÃ¡rios, vans, motoristas, alunos e rotas;
- busca e filtros de vans por texto, status e capacidade;
- criaÃ§Ã£o, inÃ­cio e finalizaÃ§Ã£o de viagens;
- registro e consulta de presenÃ§as;
- painÃ©is especÃ­ficos por papel;
- persistÃªncia em SQLite por Entity Framework Core;
- dados de referÃªncia inicializados pelo `Data/DbInitializer.cs`.
- modelo operacional de Linha recorrente e organizacao com filtro por escopo;
- administracao de Linhas, alunos geocodificados e vans/motoristas vinculados;
- geracao de viagens e presencas por Linha recorrente, janela de 14 dias uteis e prazo configuravel;
- visao do motorista mostra paradas ordenadas, ETA e links Google Maps segmentados;

## Limites atuais

NÃ£o apresentar como existente aquilo que ainda nÃ£o estÃ¡ implementado. Permanecem fora do escopo atual:

- GPS ou localizaÃ§Ã£o em tempo real;
- reservas e pagamentos;
- notificaÃ§Ãµes push ou por e-mail;
- administracao de multiplas organizacoes na interface;
- integraÃ§Ã£o com catracas ou outros serviÃ§os externos;
- mÃ©tricas de mercado, preÃ§os, depoimentos ou estudos de caso.

Entidades operacionais carregam `OrganizacaoId` e consultas usam filtros globais para o escopo autenticado. Ainda nÃ£o hÃ¡ telas para administrar mÃºltiplas organizaÃ§Ãµes.

## PrincÃ­pios de produto

1. **OperaÃ§Ã£o primeiro:** placa, rota, horÃ¡rio, status e viagem tÃªm prioridade visual.
2. **Uma aÃ§Ã£o principal:** cada tela deve deixar claro o prÃ³ximo passo.
3. **Densidade com respiro:** gestores precisam comparar registros; motoristas precisam tocar e ler com rapidez.
4. **Estado Ã© informaÃ§Ã£o:** vazio, erro, manutenÃ§Ã£o, viagem em andamento e presenÃ§a devem ser compreensÃ­veis.
5. **Dados reais:** indicadores e listas devem vir do backend, sem mÃ©tricas fictÃ­cias.
6. **Acesso adequado ao papel:** usuÃ¡rios nÃ£o devem receber menus ou aÃ§Ãµes administrativas sem necessidade.

## EvidÃªncias do projeto

- Logo em `wwwroot/images/logo.png`.
- Dados de referÃªncia em `Data/DbInitializer.cs`.
- Regras de busca em `spec/filtros-e-busca.md`.
- Plano de testes em `plan/03-plano-de-testes.md`.
- Testes automatizados em `tests/EasyVan.Tests`.

## PrÃ³ximas prioridades

- validar a navegacao por trechos em aparelhos moveis e testar PostgreSQL real;
- revisar e atualizar a dependÃªncia SQLite com vulnerabilidade reportada;
- ampliar testes de integraÃ§Ã£o HTTP e fluxos de autorizaÃ§Ã£o;
- validar acessibilidade e comportamento visual em desktop e mobile;
- validar a estratÃ©gia de administraÃ§Ã£o de mÃºltiplas organizaÃ§Ãµes e executar testes em PostgreSQL real.

## Fluxo operacional atualizado â€” 08/11/2026

O conceito de planejamento recorrente Ã© **Linha**. O administrador cadastra origem, destino, dias da semana, horÃ¡rios de ida/volta, passageiros e alocaÃ§Ã£o de vans; o sistema cria viagens diÃ¡rias e deriva as paradas dos alunos confirmados. NÃ£o se cadastra cada viagem do calendÃ¡rio manualmente. O motorista abre cada etapa no Google Maps; o EasyVan fornece sequÃªncia e ETA armazenados, nÃ£o navegaÃ§Ã£o prÃ³pria.

> **AtualizaÃ§Ã£o de 08/11/2026:** o fluxo de Linha recorrente e roteirizaÃ§Ã£o descrito ao final deste documento substitui as referÃªncias anteriores a agenda/paradas manuais e Ã  ausÃªncia de OrganizaÃ§Ã£oId. Consulte `spec/linhas-recorrentes-e-roteirizacao.md`.

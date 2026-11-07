# Produto EasyVan

## Visão

O EasyVan é uma ferramenta operacional para organizar o transporte por vans. Seu objetivo é reunir em um único fluxo os cadastros, as rotas, as viagens e as presenças que hoje podem ficar espalhados em planilhas ou mensagens.

## Usuários

### Administrador

Trabalha principalmente no desktop e administra vans, motoristas, alunos, rotas, viagens e usuários. Precisa localizar registros rapidamente, comparar status e corrigir cadastros com segurança.

### Motorista

Usa a aplicação principalmente no celular. Precisa consultar suas viagens, iniciar e finalizar percursos e registrar presenças com poucos toques.

### Aluno

Consulta as informações do próprio transporte, como van, rota e horário, sem navegar por áreas administrativas.

## Capacidades atuais

- autenticação por cookie;
- autorização por Administrador, Motorista e Aluno;
- CRUD de usuários, vans, motoristas, alunos e rotas;
- busca e filtros de vans por texto, status e capacidade;
- criação, início e finalização de viagens;
- registro e consulta de presenças;
- painéis específicos por papel;
- persistência em SQLite por Entity Framework Core;
- dados de referência inicializados pelo `Data/DbInitializer.cs`.

## Limites atuais

Não apresentar como existente aquilo que ainda não está implementado. Permanecem fora do escopo atual:

- GPS ou localização em tempo real;
- reservas e pagamentos;
- notificações push ou por e-mail;
- multi-tenancy;
- integração com mapas, catracas ou serviços externos;
- métricas de mercado, preços, depoimentos ou estudos de caso.

## Princípios de produto

1. **Operação primeiro:** placa, rota, horário, status e viagem têm prioridade visual.
2. **Uma ação principal:** cada tela deve deixar claro o próximo passo.
3. **Densidade com respiro:** gestores precisam comparar registros; motoristas precisam tocar e ler com rapidez.
4. **Estado é informação:** vazio, erro, manutenção, viagem em andamento e presença devem ser compreensíveis.
5. **Dados reais:** indicadores e listas devem vir do backend, sem métricas fictícias.
6. **Acesso adequado ao papel:** usuários não devem receber menus ou ações administrativas sem necessidade.

## Evidências do projeto

- Logo em `wwwroot/images/logo.png`.
- Dados de referência em `Data/DbInitializer.cs`.
- Regras de busca em `spec/filtros-e-busca.md`.
- Plano de testes em `plan/03-plano-de-testes.md`.
- Testes automatizados em `tests/EasyVan.Tests`.

## Próximas prioridades

- manter o cadastro de contas restrito ao Administrador e revisar contas legadas;
- revisar e atualizar a dependência SQLite com vulnerabilidade reportada;
- ampliar testes de integração HTTP e fluxos de autorização;
- validar acessibilidade e comportamento visual em desktop e mobile;
- avaliar o isolamento de dados antes de qualquer multi-tenancy.

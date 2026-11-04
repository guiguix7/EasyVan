# Segurança

Este documento registra os controles de segurança implementados no EasyVan e os cuidados necessários para executá-lo.

## Controles implementados

- Senhas são transformadas em hash PBKDF2 pelo serviço `IPasswordService`.
- A autenticação usa cookies HttpOnly, SameSite e com expiração configurada.
- O sistema diferencia Administrador, Motorista e Aluno.
- Dados são persistidos por Entity Framework Core, sem SQL cru nos fluxos comuns.
- O banco é atualizado por migrações versionadas na inicialização.
- Em produção, as chaves de Data Protection são persistidas em `/data/keys`.
- O primeiro administrador de produção só é criado com `EASYVAN_ADMIN_EMAIL` e `EASYVAN_ADMIN_PASSWORD`.
- O pipeline de produção usa tratamento de exceções e HSTS.

## Implantação segura

1. Use `ASPNETCORE_ENVIRONMENT=Production`.
2. Defina uma senha forte e única para `EASYVAN_ADMIN_PASSWORD`.
3. Armazene variáveis de ambiente em um cofre ou no mecanismo seguro do provedor.
4. Monte volumes persistentes para o banco e `/data/keys` quando necessário.
5. Restrinja o acesso direto ao arquivo SQLite e aos logs.
6. Publique a aplicação atrás de HTTPS e de uma rede controlada.
7. Faça backup do banco antes de atualizações e migrações.
8. Atualize o .NET e os pacotes NuGet regularmente.

## Limitações conhecidas

- O cadastro público permite escolher o papel conforme a implementação atual; isso precisa ser restringido antes de um uso real, para impedir que um usuário se promova a Administrador ou Motorista.
- Não há limitação de tentativas de login, recuperação de senha ou autenticação multifator implementada.
- O sistema ainda não possui multi-tenancy ou isolamento entre operadores.
- O banco SQLite local não deve ser usado como solução de alta disponibilidade sem uma arquitetura adicional.
- A restauração automática de chaves de Data Protection depende da persistência de `/data/keys` no ambiente de hospedagem.
- O pacote `SQLitePCLRaw.lib.e_sqlite3` reporta uma vulnerabilidade conhecida durante o restore; a atualização ou substituição deve ser avaliada antes da produção.

## Segredos e dados sensíveis

Não versione:

- senhas, tokens, certificados ou chaves;
- `easyvan.db` e cópias de backup;
- conteúdo real de produção ou dados pessoais;
- arquivos de configuração com credenciais.

Ao reportar uma falha, não inclua dados pessoais ou segredos. Descreva o impacto, os passos mínimos para reprodução e a versão afetada. O canal de contato de segurança ainda deve ser definido pelo responsável pelo projeto antes da publicação.

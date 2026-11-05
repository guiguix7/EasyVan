# Segurança

## Controles implementados

- Senhas armazenadas como hash PBKDF2-HMAC-SHA256 com salt aleatório de 16 bytes, chave de 32 bytes e 600.000 iterações.
- Formato de hash versionado; hashes antigos de 100.000 iterações continuam válidos e são atualizados após autenticação bem-sucedida.
- Comparação de hashes em tempo constante.
- Cadastro público sempre cria perfil Aluno. Somente Administradores autenticados podem atribuir perfis de Motorista ou Administrador.
- E-mail normalizado e índice único sem distinção entre maiúsculas/minúsculas.
- Login limitado a cinco tentativas por IP por minuto.
- Cookies HttpOnly, Secure, SameSite=Lax e expiração de oito horas.
- Chaves ASP.NET Core Data Protection persistidas em `/data/keys`.
- Migrações EF Core aplicadas na inicialização.

## Railway — persistência obrigatória

1. No serviço web do EasyVan, adicione um **Volume** com Mount Path exatamente `/data`.
2. A aplicação grava o banco em `/data/easyvan.db` e as chaves de autenticação em `/data/keys`.
3. Em Railway, a aplicação falha ao iniciar se detectar que está na plataforma sem o volume montado em `/data`; isso evita iniciar silenciosamente com banco efêmero.
4. Defina `EASYVAN_ADMIN_EMAIL` e `EASYVAN_ADMIN_PASSWORD` como variáveis privadas. A senha inicial deve ter pelo menos 12 caracteres. O administrador é criado somente quando a tabela de usuários está vazia.
5. Faça backup do volume antes de migrações ou alterações de infraestrutura. Persistência não substitui backup.
6. Antes de ativar o volume, faça cópia do banco atual de produção para o novo volume, caso existam cadastros no banco efêmero atual. Não sobrescreva nem descarte o arquivo antigo antes de validar a cópia.

## Limitações e próximos controles

- O limitador de login é local à instância; não substitui proteção distribuída em cenário com múltiplas réplicas.
- Ainda não há recuperação de senha, MFA, confirmação de e-mail ou trilha de auditoria administrativa.
- SQLite é adequado para uma instância de aplicação com volume persistente. Não execute múltiplas réplicas escrevendo no mesmo arquivo SQLite; para alta disponibilidade ou escala horizontal, migre para PostgreSQL.
- A migração do índice único de e-mail falhará se já existirem e-mails duplicados (ignorando maiúsculas/minúsculas). Resolva duplicidades com revisão dos registros antes de aplicar a migração; nenhum cadastro é apagado automaticamente.
- Revise alertas de vulnerabilidade do NuGet e atualize dependências regularmente. Não ignore alertas de `SQLitePCLRaw`.

## Segredos e dados sensíveis

Nunca versione senhas, tokens, certificados, chaves, banco de produção ou backups. Use as variáveis privadas do Railway para segredos. Não exponha credenciais em logs ou mensagens de erro.

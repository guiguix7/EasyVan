# Supabase PostgreSQL + Railway

## Arquitetura

- **Railway:** hospeda a aplicação ASP.NET Core.
- **Supabase:** PostgreSQL gerenciado usado pelo Entity Framework Core.
- **SQLite:** continua disponível apenas para desenvolvimento local quando nenhuma connection string PostgreSQL é informada.

Em produção, o EasyVan recusa iniciar com SQLite. A conexão PostgreSQL é lida de `ConnectionStrings__DefaultConnection` (variável privada da Railway).

## Connection string

No painel do projeto Supabase **EasyVan** (`xckymdhheslyrnxlqaqw`), abra **Connect** e selecione **Session pooler**. Para um backend ASP.NET persistente na Railway, use o pooler de sessão IPv4, não o endpoint direto IPv6 nem o pooler de transação.

Copie a connection string PostgreSQL exibida pelo Supabase e cadastre-a na Railway como:

- **Variable:** `ConnectionStrings__DefaultConnection`
- **Value:** connection string do Session pooler, com SSL.

O código aceita o formato URI `postgresql://...` e o formato Npgsql `Host=...;Port=...;Database=...;Username=...;Password=...;SSL Mode=Require`.

Nunca coloque essa string em `appsettings.json`, no GitHub, em commits ou em logs.

## Primeira inicialização

O projeto EasyVan já possuía três migrations SQLite, mas elas não representam um banco PostgreSQL vazio: as tabelas `Usuarios` e `Vans` foram criadas originalmente fora do histórico de migrations. Por isso, no primeiro boot PostgreSQL, `PostgresDatabaseBootstrapper`:

1. confirma que o schema `public` está vazio; se encontrar tabelas, interrompe sem tentar sobrescrever dados;
2. cria o schema completo a partir do modelo EF Core atual;
3. registra as três migrations existentes como baseline no `__EFMigrationsHistory`;
4. aplica migrations futuras normalmente com `Database.Migrate()`.

Isso evita executar migrations antigas com tipos e anotações específicos do SQLite contra o PostgreSQL.

## Segurança

- As tabelas operacionais têm RLS habilitado.
- Os privilégios de `anon` e `authenticated` são revogados; o EasyVan acessa o banco pelo backend com a credencial privada.
- O login da aplicação continua usando cookies ASP.NET e hashes PBKDF2 existentes. O Supabase Auth não substitui o login atual.
- A Railway continua precisando de um Volume em `/data` somente para persistir as chaves de Data Protection dos cookies; os cadastros e demais dados ficam no PostgreSQL.
- Se o banco estiver vazio em produção, configure `EASYVAN_ADMIN_EMAIL` e `EASYVAN_ADMIN_PASSWORD` como variáveis privadas na Railway. A senha deve ter de 12 a 128 caracteres. Ela será armazenada como hash pelo serviço atual, nunca em texto puro.

## Observação de rede

O projeto Supabase está na região `us-west-2`. O Session pooler é recomendado para conectividade IPv4 da Railway, mas a distância geográfica pode aumentar a latência. A região não é alterada por esta integração.

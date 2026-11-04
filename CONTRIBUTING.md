# Contribuindo

Obrigado por contribuir com o EasyVan. O projeto é uma aplicação ASP.NET Core MVC; mantenha as alterações pequenas, coerentes com a arquitetura existente e documentadas quando mudarem o comportamento do sistema.

## Antes de alterar

1. Leia `AGENTS.md` e a documentação relacionada à tarefa.
2. Verifique o código vizinho, as Views e as regras em `spec/`.
3. Confirme se a alteração é funcional, de segurança, visual ou documental.
4. Não introduza Node.js ou outra stack para resolver problemas que pertencem ao .NET.

## Desenvolvimento

Na raiz do projeto:

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run
```

Não execute `npm install`: não existe `package.json` neste projeto.

## Padrões

- Use C# moderno, nullable habilitado e `async/await` para acesso ao banco.
- Mantenha nomes de domínio já usados no projeto.
- Coloque acesso a dados no `ApplicationDbContext`.
- Proteja formulários POST com antiforgery e valide dados no servidor.
- Não grave senha em texto simples nem inclua segredos em arquivos versionados.
- Mantenha textos de interface e documentação em português do Brasil.
- Evite comentários óbvios; documente somente decisões não triviais.

## Testes

Toda regra nova deve ter teste do caminho feliz e de pelo menos um caso de erro quando aplicável. Para alterações em C# ou Razor, `dotnet build` é obrigatório. Para alterações de serviços ou regras de negócio, execute também `dotnet test`.

## Pull requests

Descreva:

- o problema resolvido;
- a solução adotada;
- os arquivos ou áreas afetados;
- os comandos de validação executados;
- riscos, limitações ou migrações necessárias.

Não inclua banco local, logs com dados pessoais, credenciais, tokens ou arquivos gerados.

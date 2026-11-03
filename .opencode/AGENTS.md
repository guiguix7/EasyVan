# AGENTS.md para OpenCode

Este repositório usa a fonte de verdade principal em [../AGENTS.md](../AGENTS.md).

## Regras principais
- Siga as convenções do projeto em português do Brasil.
- Faça mudanças pequenas e verificáveis.
- Rode `dotnet build` após qualquer mudança em C# ou Razor.
- Priorize segurança, validação no servidor e uso do `ApplicationDbContext`.
- Nunca altere dados sensíveis, secrets ou o banco de desenvolvimento sem necessidade.
- Registre prompts de IA em [../docs/prompts-ia.md](../docs/prompts-ia.md).

## Fluxo recomendado
1. Leia o AGENTS.md principal, o README e a spec/ relevante.
2. Entenda antes de editar.
3. Faça a correção mínima.
4. Compile com `dotnet build`.
5. Resuma o que mudou em português.

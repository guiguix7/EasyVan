# EasyVan

## Equipe
- Guilherme Pereira Andraz
- Davi Gato Grijó da Silva
- Victor Hugo Volpi Pereira
- Caio Regio Candiani
- Pedro Ailton dos Santos Cavalieri

## Sobre o projeto
EasyVan é um sistema web ASP.NET Core MVC para gerenciar o transporte de vans. O objetivo é centralizar rotas, horários, passageiros e motoristas em uma interface web simples e responsiva.

## Status atual
- Aplicação ASP.NET Core MVC funcionando
- Página de login criada em `Home/Index`
- Página de cadastro criada em `Home/Pages/cadastro`
- Validação de formulário com `LoginViewModel` e `Usuarios` usando data annotations
- Controle de usuários com `UsuariosController` e views CRUD em `Views/Usuarios`
- Layout estilizado com Bootstrap e CSS customizado em `wwwroot/css/site.css`

## Funcionalidades implementadas
- Login visual com campos de usuário, email e senha
- Link para cadastro de novo usuário
- Cadastro de usuário via formulário conectado ao modelo `Usuarios`
- Validação de campos obrigatórios e formato de email
- CRUD básico de usuários em memória (`UsuariosController`)
- Views: `Index`, `Create`, `Edit`, `Details`, `Delete` para usuários

## Funcionalidades planejadas
- Autenticação real com Identity ou banco de dados
- Persistência de dados em banco SQL
- Gerenciamento de rotas e itinerários
- Cadastro de motoristas e passageiros
- Painel administrativo com permissões por perfil

## Tecnologias
- ASP.NET Core MVC (.NET 10)
- Razor Views
- Bootstrap
- jQuery
- CSS customizado em `wwwroot/css/site.css`

## Estrutura do projeto
```
Controllers/        Controladores MVC
Views/              Views Razor
Models/             Modelos e ViewModels
wwwroot/            CSS, JS e bibliotecas
API/                Arquivos placeholder para API
Backend/JS/         Estrutura placeholder de servidor Node.js
DataBase/           Estrutura placeholder de banco de dados
```

## Como executar
1. Abra o terminal na pasta do projeto.
2. Execute `dotnet restore`.
3. Execute `dotnet build`.
4. Execute `dotnet run`.
5. Acesse `https://localhost:7291` ou `http://localhost:5000`.

## Observações
- O projeto ainda usa simulação em memória para usuários; persistência em banco não está implementada.
- O login atual valida campos via data annotations, mas não realiza autenticação de produção.
- Para evitar avisos no `dotnet run`, execute o projeto a partir da raiz do diretório do projeto.

## Tipos de usuário
- `user` - aluno
- `admin` - gerenciador
- `driver` - motorista
 
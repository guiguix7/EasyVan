# EasyVan

## Equipe
- Guilherme Pereira
- Davi Gato
- Victor Volpi
- Caio Regio
- Pedro Ailton

## Sobre o projeto
EasyVan é um sistema web MVC para gerenciar o transporte de vans. A proposta é facilitar o controle de rotas, horários, passageiros e motoristas, reduzindo erros operacionais e melhorando a organização do serviço.

## Problema que o sistema resolve
Muitas empresas e motoristas de vans enfrentam dificuldades para organizar:
- rotas
- horários
- alunos/passageiros
- motoristas
- informações de viagem

Isso pode gerar atrasos, falhas na comunicação e perda de dados importantes. O EasyVan visa centralizar essas informações em um sistema acessível e estruturado.

## Objetivo
Organizar o transporte de vans de forma eficiente para facilitar a administração de:
- rotas e itinerários
- horários de embarque/desembarque
- cadastro de passageiros
- cadastro de motoristas

## Funcionalidades principais
- Cadastro e gerenciamento de motoristas
- Cadastro e gerenciamento de passageiros
- Organização de rotas
- Definição e controle de horários
- Acompanhamento de viagens
- Controle de pagamentos e comunicação entre responsáveis

## Como executar
1. Abra o terminal na pasta do projeto.
2. Execute `dotnet restore` para restaurar dependências.
3. Execute `dotnet build` para compilar o projeto.
4. Execute `dotnet run` para iniciar a aplicação.
5. Abra o navegador em `http://localhost:5256` ou `http://localhost:5000`.

## Observações
O projeto usa o template ASP.NET Core MVC e serve arquivos estáticos a partir da pasta `wwwroot`. Para evitar avisos ao executar diretamente o `.exe`, execute o projeto a partir da raiz do projeto ou publique corretamente.
 
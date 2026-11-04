# Política de Privacidade

**Projeto:** EasyVan  
**Data de vigência:** 3 de novembro de 2026

Este documento descreve, em linguagem objetiva, quais dados o EasyVan pode tratar durante o uso da aplicação. Ele deve ser revisado pelo responsável pelo projeto e por assessoria jurídica antes de uma publicação comercial, especialmente para adequação à LGPD.

## 1. Dados tratados

O sistema pode armazenar:

- nome, e-mail, telefone e endereço informados em cadastros;
- dados operacionais de motoristas, incluindo CNH, quando cadastrados pelo gestor;
- dados de vans, rotas, viagens e presenças;
- credenciais de acesso, armazenadas como hash de senha PBKDF2;
- dados técnicos mínimos necessários ao funcionamento do servidor e da sessão, como cookies de autenticação e registros de aplicação.

O projeto não implementa GPS, pagamentos, publicidade comportamental ou serviço de analytics identificado no código atual.

## 2. Finalidades

Os dados são utilizados para:

- autenticar usuários e aplicar permissões por papel;
- administrar vans, motoristas, alunos, rotas e viagens;
- registrar presença e exibir informações de transporte;
- manter a integridade, a disponibilidade e a segurança da aplicação;
- corrigir falhas e atender solicitações relacionadas ao sistema.

## 3. Compartilhamento

O EasyVan não vende dados pessoais. O compartilhamento deve ficar limitado a:

- provedores de hospedagem ou infraestrutura necessários para operar a aplicação;
- autoridades públicas, quando houver obrigação legal válida;
- responsáveis pelo projeto, para suporte e administração do serviço.

Não há integração de terceiros para pagamentos, e-mail, mapas ou analytics implementada nesta versão.

## 4. Cookies e sessões

O sistema utiliza cookie de autenticação para manter a sessão do usuário. O cookie é configurado como HttpOnly e SameSite e possui expiração. Desabilitar cookies pode impedir o login e o uso de áreas protegidas.

## 5. Armazenamento e retenção

Em desenvolvimento, os dados são armazenados no arquivo SQLite `easyvan.db`. Em produção, o local e o período de retenção dependem da infraestrutura adotada pelo responsável pelo serviço.

Os dados devem ser mantidos somente pelo período necessário à operação, às obrigações legais e à defesa de direitos. O responsável pela implantação deve definir rotinas de backup, exclusão e restauração.

## 6. Direitos dos titulares

Nos termos aplicáveis da LGPD, o titular pode solicitar confirmação de tratamento, acesso, correção, anonimização, eliminação, portabilidade e informações sobre o uso de seus dados, observadas as hipóteses legais e a capacidade operacional do serviço.

O canal de contato oficial ainda não está configurado no repositório. Antes da publicação, substitua este aviso por um e-mail ou formulário mantido pelo controlador do tratamento.

## 7. Menores

O sistema pode conter dados de alunos no contexto do transporte escolar ou universitário. O responsável pela operação deve definir a base legal, os procedimentos de consentimento ou representação e as salvaguardas aplicáveis a menores.

## 8. Segurança

O projeto usa hash PBKDF2 para senhas e autenticação por cookie. Ainda assim, a segurança depende da configuração do ambiente, da proteção dos segredos, do controle de acesso ao banco e da atualização das dependências. Consulte [SECURITY.md](SECURITY.md).

## 9. Alterações

Esta política deve ser atualizada quando o tratamento de dados, a infraestrutura ou as funcionalidades do EasyVan mudarem. A data de vigência deve acompanhar cada revisão publicada.

> Este documento é uma descrição técnica do comportamento atual do projeto e não substitui orientação jurídica.

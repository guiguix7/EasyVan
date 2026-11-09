# Política de Privacidade

**Projeto:** EasyVan  
**Data de vigência:** 8 de novembro de 2026

Este documento descreve, em linguagem objetiva, quais dados o EasyVan pode tratar durante o uso da aplicação. Ele deve ser revisado pelo responsável pelo projeto e por assessoria jurídica antes de uma publicação comercial, especialmente para adequação à LGPD.

## 1. Dados tratados

O sistema pode armazenar:

- nome, e-mail, telefone, endereço e escola informados em cadastros;
- endereço e coordenadas do ponto operacional de embarque, quando localizados;
- dados operacionais de motoristas, incluindo CNH, quando cadastrados pelo gestor;
- dados de vans, rotas, viagens e presenças;
- credenciais de acesso, armazenadas como hash de senha PBKDF2;
- dados técnicos mínimos necessários ao funcionamento do servidor e da sessão, como cookies de autenticação e registros de aplicação.

O projeto não implementa GPS em tempo real, pagamentos, publicidade comportamental ou serviço de analytics identificado no código atual. O ponto de embarque e as coordenadas, quando disponíveis, são usados para calcular a sequência e os horários estimados da viagem.

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
- Google Maps Platform, quando configurado: autocomplete e geocodificação podem enviar texto de endereço ao Google; os serviços de rota recebem coordenadas dos pontos localizados. Consulte a política e os termos do Google para entender o tratamento pelo fornecedor;
- autoridades públicas, quando houver obrigação legal válida;
- responsáveis pelo projeto, para suporte e administração do serviço.

Não há integração de pagamentos, e-mail ou analytics identificada nesta versão. A integração opcional com Google Maps é documentada em [docs/google-maps.md](docs/google-maps.md).

## 4. Cookies e sessões

O sistema utiliza cookie de autenticação para manter a sessão do usuário. O cookie é configurado como HttpOnly e SameSite e possui expiração. Desabilitar cookies pode impedir o login e o uso de áreas protegidas.

## 5. Armazenamento e retenção

Em desenvolvimento, os dados são armazenados no arquivo SQLite `easyvan.db`. Em produção, o local e o período de retenção dependem da infraestrutura adotada pelo responsável pelo serviço.

Os dados devem ser mantidos somente pelo período necessário à operação, às obrigações legais e à defesa de direitos. O responsável pela implantação deve definir prazo de retenção por categoria, rotinas de backup, exclusão e restauração; esses prazos ainda não estão configurados no produto.

## 6. Direitos dos titulares

Nos termos aplicáveis da LGPD, o titular pode solicitar confirmação de tratamento, acesso, correção, anonimização, eliminação, portabilidade e informações sobre o uso de seus dados, observadas as hipóteses legais e a capacidade operacional do serviço.

O canal de contato oficial ainda não está configurado no repositório. Antes da publicação, substitua este aviso por um e-mail ou formulário mantido pelo controlador do tratamento.

## 7. Menores

O sistema pode conter dados de crianças e adolescentes no contexto do transporte escolar. O controlador deve avaliar e documentar a hipótese legal aplicável a cada finalidade e assegurar que o melhor interesse prevaleça. O sistema não oferece perfil de Responsável nem fluxo próprio de consentimento ou representação. Consulte orientação jurídica antes de usar dados reais de menores.

## 8. Segurança

O projeto usa hash PBKDF2 para senhas e autenticação por cookie. Ainda assim, a segurança depende da configuração do ambiente, da proteção dos segredos, do controle de acesso ao banco, da atualização das dependências e da validação do isolamento por organização em produção. O canal de titulares e o procedimento operacional de incidentes ainda precisam ser definidos pelo controlador. Consulte [SECURITY.md](SECURITY.md) e [docs/fase-6-relatorio-final.md](docs/fase-6-relatorio-final.md).

## 9. Alterações

Esta política deve ser atualizada quando o tratamento de dados, a infraestrutura ou as funcionalidades do EasyVan mudarem. A data de vigência deve acompanhar cada revisão publicada.

> Este documento é uma descrição técnica do comportamento atual do projeto e não substitui orientação jurídica.

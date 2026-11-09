# Linhas recorrentes e roteirização

Data: 08/11/2026. Esta especificação substitui, para novas operações, a criação manual de rotas/paradas e viagens.

## Conceitos e fluxo

- O Admin cria uma Linha recorrente com nome, origem, destino geocodificados, dias, sentidos, horários, alunos selecionados, alocação manual por van e motorista e capacidade validada.
- Origem, destino e embarques são informados por endereço ou selecionados no Google Maps; latitude/longitude são dados internos e nunca campos para o administrador preencher. Sem as chaves do Maps, os endereços podem ser salvos, mas a navegação por rota aguarda a localização dos pontos. O cadastro de aluno inclui a escola, exibida junto ao nome na seleção de passageiros da Linha.
- A volta pode terminar na garagem (ponto de partida da van) ou no endereço do último aluno da ordem calculada; a tela explica esse efeito sem exigir que o usuário conheça conceitos de coordenadas ou roteirização.
- Viagens são ocorrências por data, sentido e van, geradas até 14 dias úteis. Índice único + verificação anterior tornam a geração idempotente.
- Presenças são criadas como SemResposta. Só alunos confirmados viram paradas calculadas; hash ordenado de IDs/data/sentido permite reuso da rota.
- A ida calcula uma partida estimada reversa e repete a consulta usando a ordem retornada; o prazo encerra o recálculo automático. A abertura de uma viagem também recalcula se o hash mudou.
- Falhas externas preservam as últimas paradas válidas e apresentam aviso. O roteirizador falso permite executar testes sem rede/chave.

## Persistência e compatibilidade

`Linha` mantém uma referência à `Rota` legada para preservar viagens e relações antigas. Os endpoints antigos de criação manual de rota, parada, agenda, vínculo e viagem redirecionam ao cadastro de Linha; suas views de criação foram removidas. Viagens de Linha não podem ser editadas manualmente. A migration nova mantém registros existentes e atribui Organização 1 ao legado; não recriar o SQLite local.

## Google Maps e privacidade

Origens, destinos e paradas são enviados como coordenadas; nomes não são enviados. A geocodificação roda apenas ao salvar endereço. Link de navegação abre com a localização atual na primeira etapa e divide em trechos de até três paradas intermediárias para compatibilidade móvel. Ver `docs/google-maps.md` para limites, preços e fontes oficiais.

## Suposições e pendências

- A janela configurada como 14 dias úteis exclui sábados e domingos, mas não conhece feriados municipais.
- Volta termina na garagem por padrão; a opção de terminar no último aluno fica configurável na Linha.
- Sem chave, as rotas são aproximadas pelo FakeRoteirizador; não equivale à rota viária do Google.
- Ainda é preciso validar com administradores reais os horários limite e a política de rota da volta.


# Google Maps Platform â€” pesquisa e limites

**Consultado em: 08/11/2026.** Os valores abaixo sÃ£o a tabela global publicada pelo Google nesta data, em USD e por 1.000 eventos apÃ³s a cota grÃ¡tis mensal de cada SKU. O preÃ§o efetivo depende do paÃ­s da conta de faturamento, tributos e uso agregado de todos os projetos da conta.

## Rotas

- Routes API `ComputeRoutes` aceita atÃ© **25 waypoints intermediÃ¡rios** por requisiÃ§Ã£o. Waypoints 1â€“10 usam o nÃ­vel Essentials quando sÃ³ hÃ¡ recursos bÃ¡sicos; 11â€“25 entram em cobranÃ§a superior. Otimizar a ordem exige `optimizeWaypointOrder=true` e pedir `routes.optimizedIntermediateWaypointIndex` no FieldMask; a otimizaÃ§Ã£o Ã© faturada como Routes Compute Routes Pro.
- O FieldMask deve conter somente os campos necessÃ¡rios. Para esta aplicaÃ§Ã£o: `routes.distanceMeters`, `routes.duration`, `routes.legs.duration`, `routes.legs.distanceMeters`, `routes.optimizedIntermediateWaypointIndex` e, se for preciso desenhar geometria, `routes.polyline.encodedPolyline`.
- A referÃªncia atual da Maps JavaScript Routes Library diz que `arrivalTime` sÃ³ Ã© vÃ¡lido com `travelMode=TRANSIT`. Para `DRIVE`, usar `departureTime`; portanto a chegada-alvo da ida Ã© resolvida calculando uma estimativa de partida a partir da duraÃ§Ã£o e folga, e fazendo uma segunda consulta com partida estimada.
- A documentaÃ§Ã£o de preÃ§o lista cota mensal de **10.000** eventos para Compute Routes Essentials e **5.000** para Compute Routes Pro. A primeira faixa acima da cota Pro custa **US$ 10/1.000 chamadas**. A otimizaÃ§Ã£o por waypoint Ã© uma chamada por viagem/van, nÃ£o uma chamada por aluno.
- EndereÃ§os geocodificados uma vez: SKU Geocoding tem cota publicada de **10.000 chamadas/mÃªs** e primeira faixa de US$ 5/1.000. Place Autocomplete tem SKU prÃ³prio, cota publicada de 10.000 chamadas/mÃªs; o nÃºmero de eventos depende das sessÃµes/seleÃ§Ãµes.

## Estimativa para uma linha

Hipóteses: 15 alunos, uma van, ida e volta em cada dia de operação e 30 dias de operação no mês. A ida com chegada-alvo usa duas chamadas Routes Pro (estimativa inicial e novo cálculo com partida sugerida); a volta usa uma. Total: **90 chamadas Routes Pro/mês** (3 por dia × 30). Isso fica abaixo da cota mensal publicada de 5.000 chamadas Routes Pro; assumindo que a cota da conta não seja consumida por outros projetos, o custo marginal estimado é **US$ 0/mês**. Duas vans: 180 chamadas/mês. Recálculos após mudança das confirmações podem adicionar chamadas; o hash/cache evita chamadas repetidas quando o conjunto não mudou. A cota é compartilhada na conta de faturamento e não garante fatura zero.

Geocodificar os endereços dos 15 alunos uma vez equivale a 15 chamadas de Geocoding. Origem e destino da Linha usam autocomplete Places (duas seleções; cobrança depende das sessões e seleções). Atualizações de endereço geram chamadas adicionais. A estimativa não inclui outros projetos/serviços da conta nem tributos.

## Cotas e salvaguardas

- Configurar restriÃ§Ã£o da chave por APIs habilitadas, quota diÃ¡ria de Compute Routes e alertas de orÃ§amento no Google Cloud. Nunca incluir a chave em Git ou logs.
- Para 15 alunos, a requisiÃ§Ã£o fica no limite de 25 waypoints. Acima desse limite, a aplicaÃ§Ã£o deve criar trechos sequenciais e botÃµes de prÃ³ximo trecho; nÃ£o descartar alunos silenciosamente.
- A URL externa Maps suporta atÃ© 3 paradas intermediÃ¡rias em browsers mÃ³veis e atÃ© 9 em outras plataformas; limite geral de 2.048 caracteres. Ao exceder, dividir em trechos mantendo o ponto final de um como origem do seguinte. Links Maps nÃ£o calculam a ordem otimizada: recebem a sequÃªncia jÃ¡ calculada pelo EasyVan.
- NÃ£o enviar nomes ao Google. GeocodificaÃ§Ã£o e rota usam coordenadas; nÃ£o registrar endereÃ§os ou coordenadas em logs.

## Fontes oficiais

- [Compute Routes â€” waypoints](https://developers.google.com/maps/documentation/routes/intermed_waypoints)
- [OtimizaÃ§Ã£o de waypoints e FieldMask](https://developers.google.com/maps/documentation/routes/opt-way)
- [Routes API â€” preÃ§os, cobranÃ§a e limites](https://developers.google.com/maps/documentation/routes/usage-and-billing)
- [Tabela global de preÃ§os](https://developers.google.com/maps/billing-and-pricing/pricing)
- [Maps JavaScript API â€” ComputeRoutesRequest e arrivalTime](https://developers.google.com/maps/documentation/javascript/reference/route)
- [Geocoding API â€” preÃ§os e cotas](https://developers.google.com/maps/documentation/geocoding/usage-and-billing)
- [Maps URLs â€” limites e waypoints](https://developers.google.com/maps/documentation/urls/get-started)


A aplicação usa duas chaves distintas: GoogleMaps__ApiKey para chamadas de servidor (Routes/Geocoding) e GoogleMaps__BrowserApiKey para autocomplete/mapas no navegador. Restrinja cada chave às APIs e à origem/IP apropriadas. A chave de navegador é visível ao cliente por natureza; deve ser limitada por HTTP referrer.





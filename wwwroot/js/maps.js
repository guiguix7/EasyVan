// EasyVan â€” mapa de trajeto com a Google Maps JavaScript API.
// O mapa Ã© opcional: sem chave configurada, o fallback em HTML permanece visÃ­vel.
(function () {
  "use strict";

  var meta = document.querySelector('meta[name="google-maps-key"]');
  var containers = document.querySelectorAll("[data-mapa-rota]");

  if (!meta || !containers.length) {
    return;
  }

  var chave = meta.getAttribute("content");
  if (!chave) {
    return;
  }

  window.easyVanIniciarMapas = function () {
    for (var i = 0; i < containers.length; i += 1) {
      iniciar(containers[i]);
    }
  };

  function iniciar(container) {
    var canvas = container.querySelector("[data-mapa-canvas]");
    var origem = container.getAttribute("data-origem");
    var destino = container.getAttribute("data-destino");
    var paradas = [];
    try { paradas = JSON.parse(container.getAttribute("data-paradas") || "[]"); } catch (_) { paradas = []; }

    if (!canvas || !origem || !destino || !window.google || !google.maps) {
      return;
    }

    var mapa = new google.maps.Map(canvas, {
      center: { lat: -23.5505, lng: -46.6333 },
      zoom: 11,
      mapTypeControl: false,
      streetViewControl: false,
      fullscreenControl: true
    });

    var bounds = new google.maps.LatLngBounds();
    if (paradas.length > 25) { return; }

    google.maps.importLibrary("routes")
      .then(function (biblioteca) {
        return biblioteca.Route.computeRoutes({
          origin: origem,
          destination: destino,
          intermediates: paradas.map(function (endereco) { return { location: endereco }; }),
          travelMode: "DRIVING",
          optimizeWaypointOrder: paradas.length > 1,
          fields: ["path", "optimizedIntermediateWaypointIndices"]
        });
      })
      .then(function (resultado) {
        if (!resultado.routes || !resultado.routes.length) { throw new Error("Rota indisponível"); }
        var rota = resultado.routes[0];
        rota.createPolylines().forEach(function (linha) {
          linha.setOptions({ strokeColor: "#0f4c5c", strokeWeight: 5 });
          linha.setMap(mapa);
          linha.getPath().forEach(function (ponto) { bounds.extend(ponto); });
        });
        mapa.fitBounds(bounds);
        container.classList.add("is-ready");
      })
      .catch(function () {
        container.classList.remove("is-ready");
      });
  }

  var script = document.createElement("script");
  script.src =
    "https://maps.googleapis.com/maps/api/js?key=" +
    encodeURIComponent(chave) +
    "&callback=easyVanIniciarMapas&loading=async&language=pt-BR&region=BR";
  script.async = true;
  script.defer = true;
  script.onerror = function () {
    for (var i = 0; i < containers.length; i += 1) {
      containers[i].classList.remove("is-ready");
    }
  };
  document.head.appendChild(script);
})();

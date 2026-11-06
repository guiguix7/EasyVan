// EasyVan — mapa de trajeto com a Google Maps JavaScript API.
// O mapa é opcional: sem chave configurada, o fallback em HTML permanece visível.
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

    var geocoder = new google.maps.Geocoder();
    var bounds = new google.maps.LatLngBounds();
    var direcoes = new google.maps.DirectionsService();
    var renderer = new google.maps.DirectionsRenderer({
      suppressMarkers: true,
      polylineOptions: { strokeColor: "#0f4c5c", strokeWeight: 5 }
    });
    renderer.setMap(mapa);

    function geocodificar(endereco) {
      return new Promise(function (resolve, reject) {
        geocoder.geocode({ address: endereco, region: "BR" }, function (resultados, status) {
          if (status === "OK" && resultados && resultados[0]) {
            resolve(resultados[0].geometry.location);
          } else {
            reject(status);
          }
        });
      });
    }

    Promise.all([geocodificar(origem), geocodificar(destino)])
      .then(function (pontos) {
        bounds.extend(pontos[0]);
        bounds.extend(pontos[1]);
        mapa.fitBounds(bounds);

        new google.maps.Marker({ position: pontos[0], map: mapa, title: origem, label: "A" });
        new google.maps.Marker({ position: pontos[1], map: mapa, title: destino, label: "B" });

        container.classList.add("is-ready");

        direcoes.route(
          {
            origin: origem,
            destination: destino,
            travelMode: google.maps.TravelMode.DRIVING
          },
          function (resultado, status) {
            if (status === "OK") {
              renderer.setDirections(resultado);
              mapa.fitBounds(bounds);
            }
          }
        );
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

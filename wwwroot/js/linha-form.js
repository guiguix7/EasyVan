(function () {
  "use strict";
  var form = document.querySelector("[data-linha-form]");
  if (!form) return;

  var count = form.querySelector("[data-contador-alunos]");
  var capacityMessage = form.querySelector("[data-resumo-capacidade]");
  var students = Array.prototype.slice.call(form.querySelectorAll("[data-passageiro-check]"));
  var vans = Array.prototype.slice.call(form.querySelectorAll("[data-van-check]"));

  function updateCapacity() {
    var selectedStudents = students.filter(function (item) { return item.checked; });
    var selectedVans = vans.filter(function (item) { return item.checked; });
    var totalCapacity = selectedVans.reduce(function (total, item) {
      var row = item.closest("[data-van-row]");
      return total + Number(row ? row.getAttribute("data-capacidade") : 0);
    }, 0);
    if (count) count.textContent = String(selectedStudents.length);
    if (capacityMessage) {
      capacityMessage.textContent = selectedStudents.length > totalCapacity
        ? selectedStudents.length + " alunos, capacidade total " + totalCapacity + " â€” adicione outra van."
        : selectedStudents.length + " alunos, capacidade total " + totalCapacity + ".";
      capacityMessage.classList.toggle("text-danger", selectedStudents.length > totalCapacity);
    }
    form.querySelectorAll("[data-passageiro-van]").forEach(function (select) {
      Array.prototype.forEach.call(select.options, function (option) {
        if (!option.value) return;
        option.disabled = !selectedVans.some(function (van) { return van.value === option.value; });
      });
      if (select.value && !selectedVans.some(function (van) { return van.value === select.value; })) select.value = "";
    });
  }

  students.concat(vans).forEach(function (input) { input.addEventListener("change", updateCapacity); });
  updateCapacity();

  var keyMeta = document.querySelector('meta[name="google-maps-key"]');
  var key = keyMeta ? keyMeta.content : "";
  var addressWidgets = form.querySelectorAll("[data-place-autocomplete]");
  [["Destino", "DestinoLat", "DestinoLng"], ["Origem", "OrigemLat", "OrigemLng"]].forEach(function (field) {
    var address = document.getElementById(field[0]);
    var lat = document.getElementById(field[1]);
    var lng = document.getElementById(field[2]);
    if (!address || !lat || !lng) return;
    address.addEventListener("input", function () {
      lat.value = "";
      lng.value = "";
    });
  });
  form.querySelectorAll("[data-ponto-endereco]").forEach(function (address) {
    var id = address.getAttribute("data-ponto-endereco");
    var lat = document.getElementById("embarque-lat-" + id);
    var lng = document.getElementById("embarque-lng-" + id);
    address.addEventListener("input", function () {
      if (lat) lat.value = "";
      if (lng) lng.value = "";
    });
  });
  if (!key || !addressWidgets.length) return;

  function initialize() {
    google.maps.importLibrary("places").then(function (places) {
      addressWidgets.forEach(function (container) {
        var address = document.getElementById(container.getAttribute("data-address-target"));
        var lat = document.getElementById(container.getAttribute("data-lat-target"));
        var lng = document.getElementById(container.getAttribute("data-lng-target"));
        if (!address || !lat || !lng) return;
        var autocomplete = new places.PlaceAutocompleteElement();
        autocomplete.setAttribute("aria-label", "Buscar endereco no Google Maps");
        autocomplete.setAttribute("placeholder", "Digite e selecione no Google Maps");
        autocomplete.style.width = "100%";
        autocomplete.addEventListener("gmp-select", function (event) {
          var place = event.placePrediction.toPlace();
          place.fetchFields({ fields: ["formattedAddress", "location"] }).then(function () {
            if (!place.location || !place.formattedAddress) return;
            address.value = place.formattedAddress;
            lat.value = String(place.location.lat());
            lng.value = String(place.location.lng());
            address.dispatchEvent(new Event("change", { bubbles: true }));
          });
        });
        container.appendChild(autocomplete);
      });
    }).catch(function () { /* Os campos de endereÃ§o e coordenadas continuam editÃ¡veis. */ });
  }

  if (window.google && google.maps && google.maps.importLibrary) {
    initialize();
  } else {
    window.easyVanInicializarAutocomplete = initialize;
    var script = document.createElement("script");
    script.src = "https://maps.googleapis.com/maps/api/js?key=" + encodeURIComponent(key)
      + "&callback=easyVanInicializarAutocomplete&loading=async&language=pt-BR&region=BR";
    script.async = true;
    script.defer = true;
    document.head.appendChild(script);
  }
})();


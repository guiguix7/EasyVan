// EasyVan — comportamentos de interface sem dependências novas.
// 1) Alternar visibilidade da senha (mantém o valor e o autocomplete).
// 2) Refletir o estado de validação do jQuery Validation em aria-invalid.

(function () {
  "use strict";

  function alternarSenha(botao) {
    var grupo = botao.closest(".ev-password");
    var campo = grupo ? grupo.querySelector("input") : null;
    if (!campo) {
      return;
    }

    var visivel = campo.type === "text";
    campo.type = visivel ? "password" : "text";
    botao.setAttribute("aria-pressed", String(!visivel));
    botao.setAttribute("aria-label", visivel ? "Mostrar senha" : "Ocultar senha");

    var uso = botao.querySelector("use");
    if (uso) {
      uso.setAttribute("href", visivel ? "#i-eye" : "#i-eye-off");
    }

    campo.focus({ preventScroll: true });
  }

  document.addEventListener("click", function (evento) {
    var botao = evento.target.closest("[data-password-toggle]");
    if (botao) {
      evento.preventDefault();
      alternarSenha(botao);
    }
  });

  function sincronizarEstadoInvalido(raiz) {
    var escopo = raiz && raiz.querySelectorAll ? raiz : document;
    var campos = escopo.querySelectorAll(".input-validation-error");
    for (var i = 0; i < campos.length; i += 1) {
      campos[i].setAttribute("aria-invalid", "true");
    }
  }

  function limparEstadoInvalido(evento) {
    var campo = evento.target;
    if (campo && campo.classList) {
      campo.classList.remove("input-validation-error");
      campo.removeAttribute("aria-invalid");
    }
  }

  document.addEventListener("DOMContentLoaded", function () {
    sincronizarEstadoInvalido(document);

    document.addEventListener("input", limparEstadoInvalido);

    var formularios = document.querySelectorAll("form");
    for (var i = 0; i < formularios.length; i += 1) {
      formularios[i].addEventListener("submit", function () {
        window.setTimeout(function () {
          sincronizarEstadoInvalido(document);
        }, 0);
      });
    }
  });
})();

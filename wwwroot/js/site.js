// EasyVan — comportamentos de interface sem dependências novas.
// 1) Alternar visibilidade da senha (mantém o valor e o autocomplete).
// 2) Alternar o tema claro/escuro e persistir a escolha no servidor.
// 3) Máscaras de telefone brasileiro e CNH.
// 4) Refletir o estado de validação do jQuery Validation em aria-invalid.

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

  function aplicarTema(tema, botao) {
    var escuro = tema === "escuro";
    document.documentElement.setAttribute("data-theme", tema);
    botao.setAttribute("data-tema-atual", tema);

    var rotulo = escuro ? "Ativar tema claro" : "Ativar tema escuro";
    botao.setAttribute("aria-label", rotulo);
    botao.setAttribute("title", rotulo);

    var uso = botao.querySelector("use");
    if (uso) {
      uso.setAttribute("href", escuro ? "#i-sun" : "#i-moon");
    }
  }

  function alternarTema(botao) {
    var formulario = botao.closest("[data-tema-form]");
    if (!formulario) {
      return;
    }

    var temaAtual = document.documentElement.getAttribute("data-theme") === "escuro" ? "escuro" : "claro";
    var temaNovo = temaAtual === "escuro" ? "claro" : "escuro";

    aplicarTema(temaNovo, botao);

    fetch(formulario.action, {
      method: "POST",
      body: new FormData(formulario),
      credentials: "same-origin",
      headers: { "X-Requested-With": "XMLHttpRequest" }
    }).then(function (resposta) {
      if (!resposta.ok) {
        throw new Error("Não foi possível salvar o tema.");
      }
    }).catch(function () {
      aplicarTema(temaAtual, botao);
      formulario.submit();
    });
  }

  function somenteDigitos(valor, limite) {
    return (valor || "").replace(/\D/g, "").slice(0, limite);
  }

  function formatarTelefone(valor) {
    var digitos = somenteDigitos(valor, 11);
    if (!digitos) {
      return "";
    }

    if (digitos.length < 3) {
      return "(" + digitos;
    }

    if (digitos.length < 7) {
      return "(" + digitos.slice(0, 2) + ") " + digitos.slice(2);
    }

    if (digitos.length < 11) {
      return "(" + digitos.slice(0, 2) + ") " + digitos.slice(2, 6) + "-" + digitos.slice(6);
    }

    return "(" + digitos.slice(0, 2) + ") " + digitos.slice(2, 7) + "-" + digitos.slice(7);
  }

  document.addEventListener("click", function (evento) {
    var botao = evento.target.closest("[data-password-toggle]");
    if (botao) {
      evento.preventDefault();
      alternarSenha(botao);
      return;
    }

    var botaoTema = evento.target.closest("[data-tema-toggle]");
    if (botaoTema) {
      evento.preventDefault();
      alternarTema(botaoTema);
      return;
    }

    var adicionarHorario = evento.target.closest("[data-horario-add]");
    if (adicionarHorario) {
      var lista = adicionarHorario.closest("form").querySelector("[data-horarios]");
      if (lista) {
        var total = lista.querySelectorAll(".ev-schedule__row").length;
        if (total >= 10) {
          return;
        }

        var linha = document.createElement("div");
        linha.className = "ev-schedule__row";

        var campo = document.createElement("input");
        campo.type = "time";
        campo.className = "form-control";
        campo.name = "Horarios";
        campo.setAttribute("aria-label", "Horário " + (total + 1));

        var remover = document.createElement("button");
        remover.type = "button";
        remover.className = "btn btn-outline-secondary";
        remover.setAttribute("data-horario-remove", "");
        remover.setAttribute("aria-label", "Remover horário " + (total + 1));
        remover.innerHTML = '<svg class="ev-icon" aria-hidden="true" focusable="false"><use href="#i-trash"></use></svg>';

        linha.appendChild(campo);
        linha.appendChild(remover);
        lista.appendChild(linha);
        campo.focus();
      }
      return;
    }

    var removerHorario = evento.target.closest("[data-horario-remove]");
    if (removerHorario) {
      var linhaRemover = removerHorario.closest(".ev-schedule__row");
      if (linhaRemover) {
        linhaRemover.remove();
      }
    }
  });

  document.addEventListener("input", function (evento) {
    var campo = evento.target;
    if (!campo || !campo.matches) {
      return;
    }

    if (campo.matches("[data-telefone-brasil]")) {
      var telefone = formatarTelefone(campo.value);
      if (campo.value !== telefone) {
        campo.value = telefone;
      }
      return;
    }

    if (campo.matches("[data-cnh-brasil]")) {
      var cnh = somenteDigitos(campo.value, 11);
      if (campo.value !== cnh) {
        campo.value = cnh;
      }
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

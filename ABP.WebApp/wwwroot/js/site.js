(function () {
  "use strict";

  var STORAGE_KEY = "abp-sidebar";

  function isCollapsed() {
    return document.documentElement.dataset.sidebar === "collapsed";
  }

  function apply(collapsed) {
    if (collapsed) {
      document.documentElement.dataset.sidebar = "collapsed";
    } else {
      delete document.documentElement.dataset.sidebar;
    }

    document.querySelectorAll("[data-sidebar-toggle]").forEach(function (btn) {
      btn.setAttribute("aria-expanded", String(!collapsed));
      btn.title = collapsed ? "Expandir menú" : "Contraer menú";
    });
  }

  document.addEventListener("click", function (event) {
    var toggle = event.target.closest("[data-sidebar-toggle]");
    if (!toggle) return;

    var collapsed = !isCollapsed();
    apply(collapsed);
    localStorage.setItem(STORAGE_KEY, collapsed ? "collapsed" : "expanded");
  });

  // Al escribir en un campo, limpia el mensaje de validación asociado para que
  // desaparezca de inmediato.
  document.addEventListener("input", function (event) {
    var field = event.target;
    if (!(field instanceof HTMLInputElement) && !(field instanceof HTMLSelectElement) && !(field instanceof HTMLTextAreaElement)) {
      return;
    }

    if (!field.name) {
      return;
    }

    var form = field.closest("form");
    if (!form) {
      return;
    }

    var message = form.querySelector('span[data-valmsg-for="' + field.name + '"]');
    if (message) {
      message.textContent = "";
    }

    field.classList.remove("input-validation-error");
    field.setAttribute("aria-invalid", "false");
  });

  // Sincroniza el estado persistido (el atributo ya se restauró inline en <head>).
  apply(isCollapsed());
})();

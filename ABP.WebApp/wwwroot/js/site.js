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

  // Sincroniza el estado persistido (el atributo ya se restauró inline en <head>).
  apply(isCollapsed());
})();

(function () {
    "use strict";

    var STORAGE_KEY = "zeamistrap-theme";
    var root = document.documentElement;

    function applyTheme(theme) {
        root.dataset.theme = theme;
    }

    (function initTheme() {
        var saved = null;
        try { saved = localStorage.getItem(STORAGE_KEY); } catch (e) { /* ignore */ }

        if (saved === "dark" || saved === "light") {
            applyTheme(saved);
        } else {
            var prefersDark = window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches;
            applyTheme(prefersDark ? "dark" : "light");
        }
    })();

    var toggle = document.getElementById("theme-toggle");
    if (toggle) {
        toggle.addEventListener("click", function () {
            var next = root.dataset.theme === "dark" ? "light" : "dark";
            applyTheme(next);
            try { localStorage.setItem(STORAGE_KEY, next); } catch (e) { /* ignore */ }
        });
    }

    if (window.matchMedia) {
        window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", function (e) {
            var saved = null;
            try { saved = localStorage.getItem(STORAGE_KEY); } catch (err) { /* ignore */ }
            if (saved !== "dark" && saved !== "light") {
                applyTheme(e.matches ? "dark" : "light");
            }
        });
    }

    var yearEl = document.getElementById("year");
    if (yearEl) {
        yearEl.textContent = String(new Date().getFullYear());
    }
})();

/* admin.js — vanilla helpers wired by the shared layout.
 * Idempotent: safe to load on every page. */
(function () {
    'use strict';

    // ── User-chip popover toggle ──────────────────────────────────────────
    document.addEventListener('click', function (e) {
        var trigger = e.target.closest('[data-action="toggle-user-menu"]');
        var menus   = document.querySelectorAll('.user-menu');
        if (trigger) {
            var menu = trigger.closest('.user-menu');
            menus.forEach(function (m) { if (m !== menu) m.classList.remove('open'); });
            menu.classList.toggle('open');
            trigger.setAttribute('aria-expanded', menu.classList.contains('open') ? 'true' : 'false');
            return;
        }
        // click anywhere else closes any open menu
        if (!e.target.closest('.user-menu-popover')) {
            menus.forEach(function (m) { m.classList.remove('open'); });
        }
    });

    // ── Flash toast auto-dismiss ───────────────────────────────────────────
    document.querySelectorAll('.flash').forEach(function (el) {
        setTimeout(function () {
            el.style.opacity = '0';
            el.style.transform = 'translateY(8px)';
            setTimeout(function () { el.remove(); }, 320);
        }, 4500);
    });

    // ── ⌘K / Ctrl-K focuses the header search ─────────────────────────────
    document.addEventListener('keydown', function (e) {
        var isMod = e.metaKey || e.ctrlKey;
        if (isMod && (e.key === 'k' || e.key === 'K')) {
            var input = document.querySelector('.header .search input');
            if (input) {
                e.preventDefault();
                input.focus();
                input.select();
            }
        }
    });
})();

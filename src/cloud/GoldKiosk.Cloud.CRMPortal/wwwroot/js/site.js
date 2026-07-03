// Site-wide bound handlers. No global onclick attributes — keeps server-rendered
// HTML from interpolating user data into a JS context.

(function () {
    // -------------------------------------------------------------------------
    // Copy-to-clipboard for activation key reveal cards.
    // Trigger: <button data-copy-key data-key="…">…</button>
    // -------------------------------------------------------------------------
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-copy-key]');
        if (!btn) return;
        var key = btn.dataset.key || '';
        if (!key) return;
        var done = function () {
            var original = btn.textContent;
            btn.textContent = 'Copied ✓';
            setTimeout(function () { btn.textContent = original; }, 2000);
        };
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(key).then(done, done);
        } else {
            // Fallback for older browsers.
            var ta = document.createElement('textarea');
            ta.value = key;
            ta.setAttribute('readonly', '');
            ta.style.position = 'absolute';
            ta.style.left = '-9999px';
            document.body.appendChild(ta);
            ta.select();
            try { document.execCommand('copy'); } catch (_) { /* ignore */ }
            document.body.removeChild(ta);
            done();
        }
    });

    // =========================================================================
    // Generic popover helper
    // Opens/closes an element with [hidden]; wires Escape + click-outside.
    // Returns { open, close, toggle }.
    // =========================================================================
    function makePopover(triggerEl, panelEl) {
        if (!triggerEl || !panelEl) return null;

        function isOpen() { return !panelEl.hasAttribute('hidden'); }

        function open() {
            panelEl.removeAttribute('hidden');
            triggerEl.setAttribute('aria-expanded', 'true');
        }

        function close() {
            panelEl.setAttribute('hidden', '');
            triggerEl.setAttribute('aria-expanded', 'false');
        }

        function toggle() { isOpen() ? close() : open(); }

        // Click outside
        document.addEventListener('click', function (e) {
            if (isOpen() && !triggerEl.contains(e.target) && !panelEl.contains(e.target)) {
                close();
            }
        });

        // Escape key
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && isOpen()) {
                close();
                triggerEl.focus();
            }
        });

        triggerEl.addEventListener('click', toggle);

        return { open: open, close: close, toggle: toggle };
    }

    // =========================================================================
    // User-menu dropdown (A) — DEDUPLICATED: the click/Escape/outside-click
    // wiring now lives inline in _Header.cshtml so it runs synchronously and
    // doesn't race with this defer-loaded script. We only keep the arrow-key
    // navigation enhancement here; the popover toggle is no longer wired from
    // site.js. Skip if the inline handler already wired things.
    // =========================================================================
    (function () {
        var trigger = document.querySelector('[data-action="toggle-user-menu"]');
        var panel   = document.querySelector('.user-menu-panel');
        if (!trigger || !panel) return;
        // Inline handler is the source of truth — bail early before makePopover.
        return;

        var popover = makePopover(trigger, panel);

        // Arrow-key navigation among [role="menuitem"] elements.
        panel.addEventListener('keydown', function (e) {
            var items = Array.from(panel.querySelectorAll('[role="menuitem"]'));
            if (!items.length) return;
            var idx = items.indexOf(document.activeElement);

            if (e.key === 'ArrowDown') {
                e.preventDefault();
                items[(idx + 1) % items.length].focus();
            } else if (e.key === 'ArrowUp') {
                e.preventDefault();
                items[(idx - 1 + items.length) % items.length].focus();
            } else if (e.key === 'Tab') {
                // Wrap Tab inside the panel.
                var first = items[0];
                var last  = items[items.length - 1];
                if (e.shiftKey && document.activeElement === first) {
                    e.preventDefault();
                    last.focus();
                } else if (!e.shiftKey && document.activeElement === last) {
                    e.preventDefault();
                    first.focus();
                }
            }
        });
    }());

    // =========================================================================
    // Notifications popover (B)
    // =========================================================================
    (function () {
        var bellBtn = document.querySelector('[data-action="toggle-notif-panel"]');
        var panel   = document.querySelector('.notif-panel');
        if (!bellBtn || !panel) return;

        // Inline handler in _Header.cshtml owns the toggle / outside-click /
        // Escape behaviour. We only keep the data-fetch + rendering below.

        // Icon SVG lookup by action kind (matches AuditAction constants).
        function iconSvgForKind(kind) {
            // Default: info circle
            var shapes = {
                // Key lifecycle
                'provision':   '<path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>',
                'revoke_key':  '<circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="1.5"/><line x1="15" y1="9" x2="9" y2="15" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/><line x1="9" y1="9" x2="15" y2="15" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>',
                'reissue_key': '<rect x="3" y="11" width="18" height="11" rx="2" ry="2" stroke="currentColor" stroke-width="1.5"/><path d="M7 11V7a5 5 0 0 1 10 0v4" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>',
                // Lead actions
                'create':      '<line x1="12" y1="5" x2="12" y2="19" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/><line x1="5" y1="12" x2="19" y2="12" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>',
                'update':      '<path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>',
                'delete':      '<polyline points="3 6 5 6 21 6" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/><path d="M19 6l-1 14H6L5 6" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/><path d="M10 11v6M14 11v6" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>',
                'sign_in':     '<path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/><polyline points="10 17 15 12 10 7" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/><line x1="15" y1="12" x2="3" y2="12" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>',
                'export':      '<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/><polyline points="17 8 12 3 7 8" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/><line x1="12" y1="3" x2="12" y2="15" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>'
            };
            var path = shapes[kind] || '<circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="1.5"/><line x1="12" y1="8" x2="12" y2="12" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/><circle cx="12" cy="16" r="0.5" fill="currentColor" stroke="currentColor" stroke-width="1.5"/>';
            return '<svg width="14" height="14" viewBox="0 0 24 24" fill="none">' + path + '</svg>';
        }

        function kindCssClass(kind) {
            if (kind === 'revoke_key' || kind === 'delete') return 'kind-danger';
            if (kind === 'provision' || kind === 'create')  return 'kind-success';
            return 'kind-info';
        }

        function relativeTime(isoString) {
            var then = new Date(isoString);
            var diffMs = Date.now() - then.getTime();
            var diffMins = Math.floor(diffMs / 60000);
            if (diffMins < 1)  return 'Just now';
            if (diffMins < 60) return diffMins + 'm ago';
            var diffHrs = Math.floor(diffMins / 60);
            if (diffHrs < 24)  return diffHrs + 'h ago';
            var diffDays = Math.floor(diffHrs / 24);
            return diffDays + 'd ago';
        }

        function escapeHtml(s) {
            return String(s)
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;');
        }

        function renderItems(items) {
            var list = panel.querySelector('.notif-list');
            var empty = panel.querySelector('.notif-empty');
            if (!list) return;

            if (!items || items.length === 0) {
                if (empty) empty.style.display = '';
                return;
            }

            if (empty) empty.style.display = 'none';

            var html = '';
            items.forEach(function (item) {
                html +=
                    '<a class="notif-item unread" href="#" role="menuitem">' +
                      '<div class="notif-icon ' + kindCssClass(item.kind) + '">' +
                        iconSvgForKind(item.kind) +
                      '</div>' +
                      '<div class="notif-item-body">' +
                        '<div class="notif-title">' + escapeHtml(item.title) + '</div>' +
                        '<div class="notif-body">'  + escapeHtml(item.body)  + '</div>' +
                        '<time class="notif-time" datetime="' + escapeHtml(item.when) + '">' +
                          relativeTime(item.when) +
                        '</time>' +
                      '</div>' +
                    '</a>';
            });
            list.insertAdjacentHTML('beforeend', html);
        }

        // Fetch on page load (once per page — no polling).
        fetch('/api/notifications', { credentials: 'same-origin' })
            .then(function (res) { return res.ok ? res.json() : { count: 0, items: [] }; })
            .then(function (data) {
                var count = (data && typeof data.count === 'number') ? data.count : 0;
                // Update badge visibility.
                bellBtn.setAttribute('data-count', String(count));
                // Render items into panel.
                renderItems(data.items || []);
            })
            .catch(function () {
                // Network failure — silent, badge stays hidden (data-count default is not set).
                bellBtn.setAttribute('data-count', '0');
            });
    }());

    // =========================================================================
    // Global search shortcut — Ctrl+K / Cmd+K focuses the header search input.
    // The ⌘K <kbd> chip in the header is the visual hint for this shortcut.
    // =========================================================================
    (function () {
        document.addEventListener('keydown', function (e) {
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                var input = document.getElementById('global-search');
                if (!input) return;
                e.preventDefault();
                input.focus();
                input.select();
            }
        });
    }());

}());

// leads-kanban.js
// Native HTML5 drag-and-drop for the Kanban board + right-side detail drawer.
//
// Design principles:
//   - All event listeners are attached to the .kanban container (event delegation),
//     so dynamically-added cards work without re-binding.
//   - Optimistic UI: card moves immediately on drop; reverts on server failure.
//   - Drawer opens on click (not drag); Escape or backdrop click closes it.
//   - AbortController cancels in-flight drawer fetches on close.
//   - No third-party libraries.

(function () {
    'use strict';

    // -----------------------------------------------------------------------
    // State
    // -----------------------------------------------------------------------
    var dragging      = null;   // the .kb-card element being dragged
    var originalCol   = null;   // the .kb-col it came from (for revert)
    var originalNext  = null;   // sibling it was before (preserves position on revert)
    var dragDidMove   = false;  // true once the card is dropped in a new column
    var clickBlocked  = false;  // true during/after a drag so click doesn't fire

    var drawerAbort   = null;   // AbortController for the in-flight drawer fetch
    var drawerLeadId  = null;   // currently displayed lead id (avoid duplicate fetches)

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    function getCsrfToken() {
        var form = document.getElementById('kanban-csrf');
        if (!form) return '';
        var input = form.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    function showToast(msg) {
        var t = document.createElement('div');
        t.className = 'kanban-toast';
        t.textContent = msg;
        document.body.appendChild(t);
        setTimeout(function () { document.body.removeChild(t); }, 3500);
    }

    function openDrawer() {
        var drawer  = document.getElementById('leadDrawer');
        var backdrop = document.getElementById('drawerBackdrop');
        if (!drawer || !backdrop) return;
        drawer.hidden   = false;
        backdrop.hidden = false;
        drawer.focus();
    }

    function closeDrawer() {
        var drawer   = document.getElementById('leadDrawer');
        var backdrop = document.getElementById('drawerBackdrop');
        if (!drawer || !backdrop) return;
        drawer.hidden   = true;
        backdrop.hidden = true;
        // Cancel any in-flight fetch
        if (drawerAbort) { drawerAbort.abort(); drawerAbort = null; }
        drawerLeadId = null;
        var body = document.getElementById('leadDrawerBody');
        if (body) body.innerHTML = '<div class="drawer-loading">Loading…</div>';
    }

    function fetchDrawer(leadId) {
        if (leadId === drawerLeadId) { openDrawer(); return; }

        // Abort any previous in-flight request.
        if (drawerAbort) drawerAbort.abort();
        drawerAbort = new AbortController();

        var body = document.getElementById('leadDrawerBody');
        if (body) body.innerHTML = '<div class="drawer-loading">Loading…</div>';

        openDrawer();

        fetch('/Leads/DetailsPartial/' + encodeURIComponent(leadId), {
            signal: drawerAbort.signal,
            headers: { 'X-Requested-With': 'fetch' }
        })
        .then(function (res) {
            if (!res.ok) throw new Error('Server returned ' + res.status);
            return res.text();
        })
        .then(function (html) {
            if (body) body.innerHTML = html;
            drawerLeadId = leadId;
        })
        .catch(function (err) {
            if (err.name === 'AbortError') return;
            if (body) body.innerHTML = '<div class="drawer-loading" style="color:var(--danger);">Failed to load. Try again.</div>';
            console.warn('[gk kanban] drawer fetch failed', err);
        });
    }

    // -----------------------------------------------------------------------
    // Find the closest ancestor matching a selector
    // (polyfill for IE11-style closest; modern browsers have it natively)
    // -----------------------------------------------------------------------
    function closest(el, sel) {
        if (el && el.closest) return el.closest(sel);
        while (el) {
            if (el.matches && el.matches(sel)) return el;
            el = el.parentElement;
        }
        return null;
    }

    // -----------------------------------------------------------------------
    // Drag events — delegated on the .kanban container
    // -----------------------------------------------------------------------

    function onDragStart(e) {
        var card = closest(e.target, '.kb-card');
        if (!card) return;

        dragging     = card;
        originalCol  = card.parentElement;
        originalNext = card.nextElementSibling;
        dragDidMove  = false;
        clickBlocked = true;

        card.classList.add('dragging');
        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('text/plain', card.dataset.leadId || '');
    }

    function onDragEnd(e) {
        if (dragging) dragging.classList.remove('dragging');
        dragging    = null;
        originalCol = null;
        originalNext = null;

        // Remove all drop-target highlights
        document.querySelectorAll('.kb-col.drop-target').forEach(function (col) {
            col.classList.remove('drop-target');
        });

        // Unblock click after a short delay so the dragend-then-click sequence
        // (which browsers sometimes fire) doesn't open the drawer.
        setTimeout(function () { clickBlocked = false; }, 100);
    }

    function onDragOver(e) {
        var col = closest(e.target, '.kb-col');
        if (!col || !dragging) return;
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';

        // Highlight only this column
        document.querySelectorAll('.kb-col.drop-target').forEach(function (c) {
            if (c !== col) c.classList.remove('drop-target');
        });
        col.classList.add('drop-target');
    }

    function onDragLeave(e) {
        var col = closest(e.target, '.kb-col');
        if (!col) return;
        // Only remove if we've actually left the column (not just moved between children)
        var related = e.relatedTarget;
        if (!related || !col.contains(related)) {
            col.classList.remove('drop-target');
        }
    }

    function onDrop(e) {
        var col = closest(e.target, '.kb-col');
        if (!col || !dragging) return;
        e.preventDefault();
        col.classList.remove('drop-target');

        var leadId   = dragging.dataset.leadId;
        var newStage = col.dataset.stage;

        if (!leadId || !newStage) return;

        // Same column — no-op
        if (col === originalCol) return;

        // Optimistic: move the card into the new column
        var savedDragging     = dragging;
        var savedOriginalCol  = originalCol;
        var savedOriginalNext = originalNext;

        col.appendChild(savedDragging);
        updateColCounts();

        dragDidMove = true;

        // POST to MoveStage
        var csrf = getCsrfToken();
        var body = '__RequestVerificationToken=' + encodeURIComponent(csrf)
                 + '&id=' + encodeURIComponent(leadId)
                 + '&newStage=' + encodeURIComponent(newStage)
                 + '&note=';

        fetch('/Leads/MoveStage', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'X-Requested-With': 'fetch'
            },
            body: body
        })
        .then(function (res) {
            if (!res.ok) throw new Error('Server returned ' + res.status);
            // Success: update the card's internal href so a subsequent click
            // opens the correct details page.
            if (savedDragging.dataset.href) {
                // href is already correct (points to Details by id, stage is display-only)
            }
        })
        .catch(function (err) {
            console.warn('[gk kanban] MoveStage failed — reverting', err);
            // Revert: move card back to original position
            if (savedOriginalNext) {
                savedOriginalCol.insertBefore(savedDragging, savedOriginalNext);
            } else {
                savedOriginalCol.appendChild(savedDragging);
            }
            updateColCounts();
            showToast('Could not move lead. Please try again.');
        });
    }

    // Recalculate the .kb-count badge for each column after a move.
    function updateColCounts() {
        document.querySelectorAll('.kb-col').forEach(function (col) {
            var count = col.querySelectorAll('.kb-card').length;
            var badge = col.querySelector('.kb-count');
            if (badge) badge.textContent = String(count);
        });
    }

    // -----------------------------------------------------------------------
    // Click — delegated on the .kanban container
    // -----------------------------------------------------------------------

    function onKanbanClick(e) {
        if (clickBlocked) return;

        var card = closest(e.target, '.kb-card');
        if (!card) return;

        var leadId = card.dataset.leadId;
        if (!leadId) return;

        fetchDrawer(leadId);
    }

    // -----------------------------------------------------------------------
    // Keyboard — Enter/Space on .kb-card opens drawer (a11y)
    // -----------------------------------------------------------------------

    function onKanbanKeyDown(e) {
        if (e.key !== 'Enter' && e.key !== ' ') return;
        var card = closest(e.target, '.kb-card');
        if (!card) return;
        e.preventDefault();
        var leadId = card.dataset.leadId;
        if (leadId) fetchDrawer(leadId);
    }

    // -----------------------------------------------------------------------
    // Close drawer on backdrop / button click, or Escape
    // -----------------------------------------------------------------------

    function onCloseDrawerClick(e) {
        var trigger = closest(e.target, '[data-action="close-drawer"]');
        if (!trigger) return;
        closeDrawer();
    }

    function onDocumentKeyDown(e) {
        if (e.key === 'Escape') closeDrawer();
    }

    // -----------------------------------------------------------------------
    // Wire up — once the DOM is ready
    // -----------------------------------------------------------------------

    function init() {
        var kanban = document.querySelector('.kanban');
        if (!kanban) return;  // not on the kanban page

        // Drag events
        kanban.addEventListener('dragstart', onDragStart);
        kanban.addEventListener('dragend',   onDragEnd);
        kanban.addEventListener('dragover',  onDragOver);
        kanban.addEventListener('dragleave', onDragLeave);
        kanban.addEventListener('drop',      onDrop);

        // Click / keyboard for drawer
        kanban.addEventListener('click',   onKanbanClick);
        kanban.addEventListener('keydown', onKanbanKeyDown);

        // Close drawer triggers (backdrop + close button are outside .kanban)
        document.addEventListener('click',   onCloseDrawerClick);
        document.addEventListener('keydown', onDocumentKeyDown);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();

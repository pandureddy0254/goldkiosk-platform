/* ============================================================================
   GoldKiosk — Theme toggle with localStorage persistence.
   Default = system preference. Manual choice overrides.
   ============================================================================ */
(function () {
  const KEY = 'goldkiosk:theme';

  function getStored()      { try { return localStorage.getItem(KEY); } catch { return null; } }
  function setStored(value) { try { localStorage.setItem(KEY, value); } catch {} }
  function systemPref()     { return matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'; }

  function apply(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    document.querySelectorAll('[data-theme-label]').forEach(el => {
      el.textContent = theme === 'dark' ? 'Light' : 'Dark';
    });
  }

  // Apply at parse time so there's no flash.
  const initial = getStored() || systemPref();
  apply(initial);

  document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[data-action="toggle-theme"]').forEach(el => {
      el.addEventListener('click', () => {
        const next = document.documentElement.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
        apply(next);
        setStored(next);
        // Persist to server so the next page load renders the correct theme
        // server-side (eliminates FOUC). Fire-and-forget — UI is already updated.
        fetch('/api/preferences/theme', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          credentials: 'same-origin',
          body: JSON.stringify({ theme: next })
        }).catch(function () { /* silent — localStorage is the fallback */ });
      });
    });
  });

  // React to system change only if user hasn't explicitly chosen.
  matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
    if (!getStored()) apply(e.matches ? 'dark' : 'light');
  });
})();

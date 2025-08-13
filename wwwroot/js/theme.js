(function() {
  function applyTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    var icon = document.getElementById('themeToggleIcon');
    if (icon) {
      icon.className = theme === 'dark' ? 'bi bi-sun' : 'bi bi-moon';
    }
    var btn = document.getElementById('themeToggle');
    if (btn) {
      btn.title = theme === 'dark' ? 'تم روشن' : 'تم تیره';
    }
  }

  function getPreferredTheme() {
    try {
      var saved = localStorage.getItem('theme');
      if (saved) return saved;
    } catch (e) {}
    if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
      return 'dark';
    }
    return 'light';
  }

  document.addEventListener('DOMContentLoaded', function() {
    var theme = getPreferredTheme();
    applyTheme(theme);

    var toggle = document.getElementById('themeToggle');
    if (toggle) {
      toggle.addEventListener('click', function () {
        var current = document.documentElement.getAttribute('data-theme') || 'light';
        var next = current === 'light' ? 'dark' : 'light';
        applyTheme(next);
        try { localStorage.setItem('theme', next); } catch (e) {}
      });
    }
  });
})();

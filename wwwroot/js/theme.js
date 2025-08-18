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

    // Sidebar toggles
    function toggleSidebar(open) {
      var sidebar = document.getElementById('sidebar');
      var overlay = document.getElementById('sidebarOverlay');
      if (!sidebar || !overlay) return;
      if (open === true) {
        sidebar.classList.add('open');
        overlay.classList.add('active');
      } else if (open === false) {
        sidebar.classList.remove('open');
        overlay.classList.remove('active');
      } else {
        sidebar.classList.toggle('open');
        overlay.classList.toggle('active');
      }
    }

    var sidebarToggle = document.getElementById('sidebarToggle');
    if (sidebarToggle) sidebarToggle.addEventListener('click', function() { toggleSidebar(false); });

    // Minimalize on double-click of header title or with key 'm'
    var sidebar = document.getElementById('sidebar');
    if (sidebar) {
      sidebar.addEventListener('dblclick', function() {
        sidebar.classList.toggle('minimal');
      });
      document.addEventListener('keydown', function(e) {
        if (e.key.toLowerCase() === 'm' && !e.ctrlKey && !e.metaKey && !e.altKey) {
          sidebar.classList.toggle('minimal');
        }
      });
    }

    var sidebarToggleMobile = document.getElementById('sidebarToggleMobile');
    if (sidebarToggleMobile) sidebarToggleMobile.addEventListener('click', function() { toggleSidebar(); });

    var sidebarOverlay = document.getElementById('sidebarOverlay');
    if (sidebarOverlay) sidebarOverlay.addEventListener('click', function() { toggleSidebar(false); });

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

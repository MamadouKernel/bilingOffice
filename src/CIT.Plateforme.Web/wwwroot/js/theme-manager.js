(function () {
    const savedTheme = localStorage.getItem('cit-theme');
    const prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    
    if (savedTheme) {
        document.documentElement.setAttribute('data-theme', savedTheme);
    } else if (prefersDark) {
        document.documentElement.setAttribute('data-theme', 'dark');
    }
})();

function toggleTheme() {
    const currentTheme = document.documentElement.getAttribute('data-theme');
    const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
    document.documentElement.setAttribute('data-theme', newTheme);
    localStorage.setItem('cit-theme', newTheme);
}

// F2 (audit sécurité) — les gestionnaires d'événements INLINE (onclick="…") sont bloqués par la
// CSP à nonce (script-src sans 'unsafe-inline'). On les remplace par un écouteur DÉLÉGUÉ, unique et
// posé sur le document : il reste valable pour les boutons que la navigation améliorée de Blazor
// fait apparaître ensuite, sans se raccrocher à chaque rendu.
document.addEventListener('click', function (e) {
    if (e.target.closest('[data-theme-toggle]')) {
        toggleTheme();
        return;
    }
    if (e.target.closest('[data-reload]')) {
        window.location.reload();
    }
});

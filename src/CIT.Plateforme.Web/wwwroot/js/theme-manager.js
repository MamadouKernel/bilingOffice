// Thème : CLAIR par défaut, sombre uniquement si l'utilisateur l'a choisi (bouton 🌓, mémorisé).
// DÉFAUT CORRIGÉ : le thème suivait la préférence du système au chargement complet, alors que la
// navigation améliorée de Blazor, en recalquant le <html> rendu par le serveur, effaçait
// l'attribut data-theme. La page alternait donc sombre (après un rechargement, ex. « Recharger »
// sur la barre d'erreur) et clair (après une navigation), sans action de l'utilisateur.
function themeVoulu() {
    try { return localStorage.getItem('cit-theme') === 'dark' ? 'dark' : 'light'; }
    catch { return 'light'; }
}

function appliquerTheme() {
    const voulu = themeVoulu();
    if (document.documentElement.getAttribute('data-theme') !== voulu) {
        document.documentElement.setAttribute('data-theme', voulu);
    }
}

appliquerTheme();

// Réapplique le thème si une mise à jour du DOM (navigation améliorée Blazor) retire l'attribut.
new MutationObserver(appliquerTheme)
    .observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });

function toggleTheme() {
    const nouveau = themeVoulu() === 'dark' ? 'light' : 'dark';
    // Le choix est mémorisé AVANT d'être appliqué : l'observateur relit toujours la préférence.
    try { localStorage.setItem('cit-theme', nouveau); } catch { /* stockage indisponible */ }
    appliquerTheme();
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

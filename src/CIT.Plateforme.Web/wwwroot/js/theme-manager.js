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
    // Sélecteur segmenté Clair/Sombre de l'en-tête : reflète le thème effectif.
    document.querySelectorAll('[data-theme-set]').forEach(function (bouton) {
        bouton.setAttribute('aria-pressed', String(bouton.getAttribute('data-theme-set') === voulu));
    });
}

function definirTheme(theme) {
    try { localStorage.setItem('cit-theme', theme === 'dark' ? 'dark' : 'light'); } catch { /* stockage indisponible */ }
    appliquerTheme();
}

appliquerTheme();

// Réapplique le thème si une mise à jour du DOM (navigation améliorée Blazor) retire l'attribut.
new MutationObserver(appliquerTheme)
    .observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });

// L'en-tête est rendu après ce script (placé dans <head>) : synchroniser le sélecteur une fois le
// document prêt, puis après chaque navigation améliorée de Blazor.
document.addEventListener('DOMContentLoaded', function () {
    appliquerTheme();
    if (window.Blazor && typeof window.Blazor.addEventListener === 'function') {
        window.Blazor.addEventListener('enhancedload', appliquerTheme);
    }
});

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
    const choix = e.target.closest('[data-theme-set]');
    if (choix) {
        definirTheme(choix.getAttribute('data-theme-set'));
        return;
    }
    if (e.target.closest('[data-theme-toggle]')) {
        toggleTheme();
        return;
    }
    if (e.target.closest('[data-reload]')) {
        window.location.reload();
    }
});

// MOTION — entrée du contenu après une navigation « améliorée » de Blazor (même document) :
// l'API View Transitions ne couvre que les navigations complètes. On rejoue donc l'animation
// d'entrée du contenu principal en retirant puis remettant sa classe. Aucun texte n'est modifié
// (Blazor reste maître du DOM), et rien ne se passe si l'utilisateur réduit les animations.
(function () {
    const reduit = window.matchMedia('(prefers-reduced-motion: reduce)');

    function rejouerEntree() {
        if (reduit.matches) { return; }
        const principal = document.getElementById('cit-contenu-principal');
        if (!principal) { return; }
        principal.classList.remove('cit-page-entree');
        void principal.offsetWidth; // force le recalcul pour relancer l'animation
        principal.classList.add('cit-page-entree');
    }

    document.addEventListener('DOMContentLoaded', function () {
        rejouerEntree();
        if (window.Blazor && typeof window.Blazor.addEventListener === 'function') {
            window.Blazor.addEventListener('enhancedload', rejouerEntree);
        }
    });
})();

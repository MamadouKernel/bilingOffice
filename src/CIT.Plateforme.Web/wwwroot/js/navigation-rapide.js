// RECHERCHE D'ÉCRAN (en-tête) — Ctrl+K (ou ⌘K) place le curseur dans la recherche ; choisir un
// écran de la liste y navigue. La liste (<datalist>) est rendue par le serveur à partir du menu du
// compte : on ne peut atteindre que les écrans que ce compte voit déjà. Écouteurs délégués sur le
// document (CSP sans 'unsafe-inline', et valables après la navigation améliorée de Blazor).
(function () {
    function routePour(champ) {
        const liste = champ.list;
        if (!liste) { return null; }
        const saisie = champ.value.trim().toLowerCase();
        const option = Array.from(liste.options).find(function (o) { return o.value.toLowerCase() === saisie; });
        return option ? option.getAttribute('data-route') : null;
    }

    function naviguer(champ) {
        const route = routePour(champ);
        // Chemin interne uniquement : jamais d'URL absolue ni de schéma, même si le DOM était altéré.
        if (route && route.startsWith('/') && !route.startsWith('//')) {
            champ.value = '';
            window.location.assign(route);
        }
    }

    document.addEventListener('change', function (e) {
        if (e.target.matches && e.target.matches('[data-recherche-ecran]')) { naviguer(e.target); }
    });

    document.addEventListener('keydown', function (e) {
        if (e.target.matches && e.target.matches('[data-recherche-ecran]') && e.key === 'Enter') {
            e.preventDefault();
            naviguer(e.target);
            return;
        }
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
            const champ = document.querySelector('[data-recherche-ecran]');
            if (champ) {
                e.preventDefault();
                champ.focus();
            }
        }
    });
})();

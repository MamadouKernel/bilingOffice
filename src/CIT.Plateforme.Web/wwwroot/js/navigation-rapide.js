// RECHERCHE D'ÉCRAN (en-tête) — Ctrl+K (ou ⌘K) place le curseur dans la recherche ; choisir un
// écran de la liste y navigue. La liste (<datalist>) est rendue par le serveur à partir du menu du
// compte : on ne peut atteindre que les écrans que ce compte voit déjà. Écouteurs délégués sur le
// document (CSP sans 'unsafe-inline', et valables après la navigation améliorée de Blazor).
(function () {
    // Comparaison sans casse ni accents : « proforma » trouve « Simulation Proforma ».
    function normaliser(texte) {
        return texte.trim().toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '');
    }

    // Libellé exact d'abord (choix dans la liste) ; sinon, à la validation par Entrée, le premier
    // écran qui contient la saisie — quelques lettres puis Entrée suffisent.
    function routePour(champ, partielle) {
        const liste = champ.list;
        if (!liste) { return null; }
        const saisie = normaliser(champ.value);
        if (!saisie) { return null; }
        const options = Array.from(liste.options);
        const option = options.find(function (o) { return normaliser(o.value) === saisie; })
            || (partielle ? options.find(function (o) { return normaliser(o.value).indexOf(saisie) !== -1; }) : null);
        return option ? option.getAttribute('data-route') : null;
    }

    function naviguer(champ, partielle) {
        const route = routePour(champ, partielle);
        // Chemin interne uniquement : jamais d'URL absolue ni de schéma, même si le DOM était altéré.
        if (route && route.startsWith('/') && !route.startsWith('//')) {
            champ.value = '';
            window.location.assign(route);
        }
    }

    document.addEventListener('change', function (e) {
        if (e.target.matches && e.target.matches('[data-recherche-ecran]')) { naviguer(e.target); }
    });

    // Menus déroulants de la barre (<details class="cit-user-menu"> : espace, profil) : un clic
    // ailleurs ou Échap les referme, et en ouvrir un referme l'autre.
    function fermerMenus(sauf) {
        document.querySelectorAll('details.cit-user-menu[open]').forEach(function (d) {
            if (d !== sauf) { d.removeAttribute('open'); }
        });
    }

    document.addEventListener('click', function (e) {
        fermerMenus(e.target.closest ? e.target.closest('details.cit-user-menu') : null);
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') { fermerMenus(null); }
    });

    document.addEventListener('keydown', function (e) {
        if (e.target.matches && e.target.matches('[data-recherche-ecran]') && e.key === 'Enter') {
            e.preventDefault();
            naviguer(e.target, true);
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

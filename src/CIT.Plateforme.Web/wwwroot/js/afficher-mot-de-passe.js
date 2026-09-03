// Bascule « afficher / masquer » sur les champs de mot de passe.
//
// POURQUOI DU JAVASCRIPT ET NON UN @onclick : Routes.razor ne déclare aucun
// @rendermode, la plateforme est donc rendue en Static SSR. Un gestionnaire
// @onclick ne se déclencherait jamais.
//
// POURQUOI DE LA DÉLÉGATION ET NON UN ÉCOUTEUR PAR BOUTON : la navigation
// améliorée de Blazor remplace des portions du DOM. Un écouteur attaché à un
// bouton donné disparaît avec lui. L'écouteur est donc posé une seule fois sur
// le document, et fonctionne pour tout bouton présent ou futur — y compris ceux
// qu'un rendu ultérieur fera apparaître.
//
// Le balisage du bouton vient de Components/Shared/ChampMotDePasse.razor :
// rendu côté serveur, il fait partie du HTML de référence et survit à la
// synchronisation du DOM.
(function () {
    "use strict";

    // Révèle les boutons : sans JavaScript, la règle CSS les laisse masqués,
    // car un bouton inerte vaut moins que pas de bouton du tout. La classe est
    // posée sur <html>, hors de la portée de la synchronisation du DOM.
    document.documentElement.classList.add("js-actif");

    document.addEventListener("click", function (evenement) {
        var bouton = evenement.target.closest(".cit-bascule-mot-de-passe");
        if (!bouton) {
            return;
        }

        var enveloppe = bouton.closest(".cit-champ-mot-de-passe");
        var champ = enveloppe && enveloppe.querySelector("input");
        if (!champ) {
            return;
        }

        var visible = champ.type === "password";
        champ.type = visible ? "text" : "password";

        // L'icône découle du type du champ via CSS : rien à basculer ici.
        // Seule l'annonce aux technologies d'assistance est mise à jour.
        bouton.setAttribute("aria-pressed", visible ? "true" : "false");
        var libelle = visible
            ? bouton.getAttribute("data-libelle-masquer")
            : bouton.getAttribute("data-libelle-afficher");
        if (libelle) {
            bouton.setAttribute("aria-label", libelle);
            bouton.setAttribute("title", libelle);
        }

        // Le curseur revient dans le champ, à la fin de la saisie : sans cela,
        // l'utilisateur doit recliquer pour continuer à taper.
        var position = champ.value.length;
        champ.focus();
        try {
            champ.setSelectionRange(position, position);
        } catch (e) {
            // setSelectionRange n'est pas supporté sur tous les types de champ.
        }
    });

    // À la soumission, le champ redevient masqué : le mot de passe ne doit pas
    // rester lisible à l'écran une fois le formulaire parti, ni réapparaître en
    // clair si la page est restaurée depuis le cache de navigation.
    document.addEventListener("submit", function (evenement) {
        var formulaire = evenement.target;
        if (!formulaire || typeof formulaire.querySelectorAll !== "function") {
            return;
        }
        var enveloppes = formulaire.querySelectorAll(".cit-champ-mot-de-passe");
        for (var i = 0; i < enveloppes.length; i++) {
            var champ = enveloppes[i].querySelector("input");
            var bouton = enveloppes[i].querySelector(".cit-bascule-mot-de-passe");
            if (champ && champ.type === "text") {
                champ.type = "password";
            }
            if (bouton) {
                bouton.setAttribute("aria-pressed", "false");
                var libelle = bouton.getAttribute("data-libelle-afficher");
                if (libelle) {
                    bouton.setAttribute("aria-label", libelle);
                    bouton.setAttribute("title", libelle);
                }
            }
        }
    }, true);
})();

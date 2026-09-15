// REMÉDIATION UI/UX — accessibilité (WCAG 2.4.3 « Focus Order »).
//
// Une modale de confirmation place le focus sur son bouton d'annulation à l'ouverture. À la
// fermeture, le focus doit RETOURNER sur l'élément qui l'a ouverte — sinon l'utilisateur au
// clavier ou au lecteur d'écran est renvoyé en haut du document, perdant sa position.
//
// `memoriser` capture l'élément actif juste avant que la modale ne prenne le focus ; `restaurer`
// le lui rend, à condition qu'il soit toujours présent dans le document.
window.citFocus = {
    _precedent: null,
    memoriser: function () {
        this._precedent = document.activeElement;
    },
    restaurer: function () {
        const cible = this._precedent;
        this._precedent = null;
        if (cible && typeof cible.focus === "function" && document.contains(cible)) {
            cible.focus();
        }
    }
};

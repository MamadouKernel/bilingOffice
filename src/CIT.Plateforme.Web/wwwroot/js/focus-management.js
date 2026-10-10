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
    _piege: null,
    memoriser: function () {
        this._precedent = document.activeElement;
    },
    restaurer: function () {
        const cible = this._precedent;
        this._precedent = null;
        if (cible && typeof cible.focus === "function" && document.contains(cible)) {
            cible.focus();
        }
    },
    // WCAG 2.4.3 / 2.1.2 inversé : tant qu'une modale est ouverte, le focus clavier doit RESTER
    // dans le dialogue. Sans ce piège, Tab s'échappait vers la page derrière (et Échap, câblé sur
    // le voile, cessait alors de fonctionner). Le gestionnaire est posé sur le document en capture :
    // il agit même si le focus a déjà quitté le conteneur, et le ramène.
    _selecteurFocusable: 'a[href],button:not([disabled]),input:not([disabled]),select:not([disabled]),textarea:not([disabled]),[tabindex]:not([tabindex="-1"])',
    piegeDans: function (conteneur) {
        this.libererPiege();
        if (!conteneur) {
            return;
        }
        const self = this;
        const handler = function (e) {
            if (e.key !== "Tab") {
                return;
            }
            const focusables = Array.prototype.slice
                .call(conteneur.querySelectorAll(self._selecteurFocusable))
                .filter(function (el) { return el.offsetParent !== null; });
            if (focusables.length === 0) {
                return;
            }
            const premier = focusables[0];
            const dernier = focusables[focusables.length - 1];
            const actif = document.activeElement;
            if (!conteneur.contains(actif)) {
                e.preventDefault();
                premier.focus();
            } else if (e.shiftKey && actif === premier) {
                e.preventDefault();
                dernier.focus();
            } else if (!e.shiftKey && actif === dernier) {
                e.preventDefault();
                premier.focus();
            }
        };
        this._piege = handler;
        document.addEventListener("keydown", handler, true);
    },
    libererPiege: function () {
        if (this._piege) {
            document.removeEventListener("keydown", this._piege, true);
            this._piege = null;
        }
    }
};

// Fil de discussion : amène le dernier message en vue (assistant virtuel).
window.citDefilerEnBas = function (el) {
    if (el) el.scrollTop = el.scrollHeight;
};

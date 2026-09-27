// VISITE GUIDÉE — présente la barre et le menu à la première connexion (voir
// Components/Layout/VisiteGuidee.razor, qui rend les étapes traduites dans <template id="cit-visite">).
// Aucun HTML interprété : tout le texte passe par textContent. « Déjà vue » est mémorisé par
// utilisateur dans le navigateur ; [data-visite-lancer] la relance (panneau « ? »).
(function () {
    'use strict';

    const PREFIXE_CLE = 'cit-visite-vue:';
    let enCours = null;

    function lireStockage(cle) {
        try { return localStorage.getItem(cle); } catch { return null; }
    }

    function ecrireStockage(cle) {
        try { localStorage.setItem(cle, new Date().toISOString()); } catch { /* stockage indisponible : la visite sera simplement reproposée */ }
    }

    function estVisible(el) {
        if (!el || el.getClientRects().length === 0) { return false; }
        const style = getComputedStyle(el);
        return style.visibility !== 'hidden' && style.display !== 'none';
    }

    function premiereCibleVisible(selecteurs) {
        if (!selecteurs) { return null; }
        return Array.from(document.querySelectorAll(selecteurs)).find(estVisible) || null;
    }

    function lireEtapes(modele) {
        return Array.from(modele.content.querySelectorAll('[data-etape]')).map(function (e) {
            return { cible: e.getAttribute('data-cible'), titre: e.getAttribute('data-titre'), texte: e.textContent.trim() };
        });
    }

    function creer(tag, classe, texte) {
        const el = document.createElement(tag);
        if (classe) { el.className = classe; }
        if (texte !== undefined) { el.textContent = texte; }
        return el;
    }

    function demarrer(modele) {
        if (enCours) { return; }
        // Étapes dont la cible n'existe pas pour ce compte ou cet écran : écartées d'emblée.
        const etapes = lireEtapes(modele).filter(function (e) { return !e.cible || premiereCibleVisible(e.cible); });
        if (etapes.length === 0) { return; }

        const libelles = modele.dataset;
        const cle = PREFIXE_CLE + libelles.cle;
        const focusInitial = document.activeElement;

        const racine = creer('div', 'cit-visite');
        const halo = creer('div', 'cit-visite-halo');
        const carte = creer('div', 'cit-visite-carte');
        carte.setAttribute('role', 'dialog');
        carte.setAttribute('aria-modal', 'true');
        carte.setAttribute('aria-labelledby', 'cit-visite-titre');
        const compteur = creer('div', 'cit-visite-compteur');
        const titre = creer('h2', 'cit-visite-titre');
        titre.id = 'cit-visite-titre';
        const texte = creer('p', 'cit-visite-texte');
        const actions = creer('div', 'cit-visite-actions');
        const passer = creer('button', 'btn btn-link btn-sm cit-visite-passer', libelles.passer);
        const precedent = creer('button', 'btn btn-outline-secondary btn-sm', libelles.precedent);
        const suivant = creer('button', 'btn btn-primary btn-sm', libelles.suivant);
        [passer, precedent, suivant].forEach(function (b) { b.type = 'button'; });
        actions.append(passer, precedent, suivant);
        carte.append(compteur, titre, texte, actions);
        racine.append(halo, carte);
        document.body.appendChild(racine);

        let index = 0;

        function positionner() {
            const etape = etapes[index];
            const cible = premiereCibleVisible(etape.cible);
            racine.classList.toggle('centree', !cible);
            if (!cible) { return; }

            const r = cible.getBoundingClientRect();
            const marge = 6;
            Object.assign(halo.style, {
                top: (r.top - marge) + 'px', left: (r.left - marge) + 'px',
                width: (r.width + 2 * marge) + 'px', height: (r.height + 2 * marge) + 'px'
            });

            // Carte sous la cible si la place le permet, sinon au-dessus ; toujours dans l'écran.
            const largeur = carte.offsetWidth;
            const hauteur = carte.offsetHeight;
            const bas = r.bottom + 14;
            const top = bas + hauteur <= innerHeight - 8 ? bas : Math.max(8, r.top - hauteur - 14);
            const left = Math.min(Math.max(8, r.left), innerWidth - largeur - 8);
            carte.style.top = top + 'px';
            carte.style.left = Math.max(8, left) + 'px';
        }

        function afficher() {
            const etape = etapes[index];
            compteur.textContent = libelles.compteur.replace('{0}', index + 1).replace('{1}', etapes.length);
            titre.textContent = etape.titre;
            texte.textContent = etape.texte;
            precedent.hidden = index === 0;
            suivant.textContent = index === etapes.length - 1 ? libelles.terminer : libelles.suivant;
            positionner();
            suivant.focus();
        }

        function fermer() {
            ecrireStockage(cle);
            removeEventListener('resize', positionner);
            removeEventListener('scroll', positionner, true);
            document.removeEventListener('keydown', clavier, true);
            racine.remove();
            enCours = null;
            // Retour du focus là où il était ; si c'était dans le panneau « ? », désormais refermé,
            // on le rend au bouton « ? » lui-même.
            const retour = estVisible(focusInitial) ? focusInitial : document.querySelector('.cit-aide-bouton');
            if (retour && typeof retour.focus === 'function') { retour.focus(); }
        }

        function avancer() {
            if (index < etapes.length - 1) { index++; afficher(); } else { fermer(); }
        }

        function reculer() {
            if (index > 0) { index--; afficher(); }
        }

        function clavier(e) {
            if (e.key === 'Escape') { e.preventDefault(); fermer(); return; }
            if (e.key === 'ArrowRight') { e.preventDefault(); avancer(); return; }
            if (e.key === 'ArrowLeft') { e.preventDefault(); reculer(); return; }
            if (e.key === 'Tab') {
                // Le focus reste dans la carte tant que la visite est ouverte.
                const boutons = [passer, precedent, suivant].filter(function (b) { return !b.hidden; });
                const position = boutons.indexOf(document.activeElement);
                e.preventDefault();
                const prochain = e.shiftKey ? position - 1 : position + 1;
                boutons[(prochain + boutons.length) % boutons.length].focus();
            }
        }

        passer.addEventListener('click', fermer);
        precedent.addEventListener('click', reculer);
        suivant.addEventListener('click', avancer);
        document.addEventListener('keydown', clavier, true);
        addEventListener('resize', positionner);
        addEventListener('scroll', positionner, true);

        enCours = { fermer: fermer };
        afficher();
    }

    function demarrageAutomatique() {
        const modele = document.getElementById('cit-visite');
        if (!modele || enCours) { return; }
        // Pas pendant les parcours de compte (activation 2FA, mot de passe…) : l'utilisateur y a
        // déjà une tâche à terminer.
        if (location.pathname.toLowerCase().startsWith('/comptes/')) { return; }
        if (lireStockage(PREFIXE_CLE + modele.dataset.cle)) { return; }
        setTimeout(function () { demarrer(modele); }, 600);
    }

    document.addEventListener('click', function (e) {
        const lanceur = e.target.closest ? e.target.closest('[data-visite-lancer]') : null;
        const modele = document.getElementById('cit-visite');
        if (!lanceur || !modele) { return; }
        e.preventDefault();
        const panneau = lanceur.closest('details');
        if (panneau) { panneau.removeAttribute('open'); }
        demarrer(modele);
    });

    document.addEventListener('DOMContentLoaded', function () {
        demarrageAutomatique();
        if (window.Blazor && typeof window.Blazor.addEventListener === 'function') {
            window.Blazor.addEventListener('enhancedload', demarrageAutomatique);
        }
    });
})();

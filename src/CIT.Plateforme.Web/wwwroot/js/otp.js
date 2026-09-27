// SAISIE OTP À CASES (VerifierCode.razor) — amélioration progressive d'un champ unique.
// Le <input> réel reste la seule source de la valeur envoyée : il devient transparent et couvre les
// cases, qui reflètent sa valeur (curseur sur la case active). Code complet : les cases se
// rassemblent en anneau et tournent, puis le formulaire est soumis. Écouteurs délégués (CSP à
// nonce, navigation améliorée de Blazor).
(function () {
    const DUREE_ANNEAU_MS = 900;

    function activer(racine) {
        if (racine.classList.contains('cit-otp--actif')) { return; }
        racine.classList.add('cit-otp--actif');
        refleter(racine);
    }

    function refleter(racine) {
        const champ = racine.querySelector('input');
        const cases = racine.querySelectorAll('.cit-otp-case');
        const nettoye = champ.value.replace(/\D/g, '').slice(0, cases.length);
        if (champ.value !== nettoye) { champ.value = nettoye; }

        cases.forEach(function (c, i) {
            c.textContent = nettoye[i] || '';
            c.classList.toggle('rempli', i < nettoye.length);
            c.classList.toggle('courant', document.activeElement === champ && i === Math.min(nettoye.length, cases.length - 1));
        });

        if (nettoye.length === cases.length) { terminer(racine, champ); }
        else { racine.classList.remove('cit-otp--anneau'); }
    }

    function terminer(racine, champ) {
        if (racine.classList.contains('cit-otp--anneau')) { return; }
        racine.classList.add('cit-otp--anneau');
        const reduit = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        window.setTimeout(function () {
            const formulaire = champ.form;
            if (formulaire && racine.classList.contains('cit-otp--anneau')) { formulaire.requestSubmit(); }
        }, reduit ? 0 : DUREE_ANNEAU_MS);
    }

    function racineDe(cible) { return cible && cible.closest ? cible.closest('[data-otp]') : null; }

    ['input', 'focusin', 'focusout', 'keyup', 'click'].forEach(function (type) {
        document.addEventListener(type, function (e) {
            const racine = racineDe(e.target);
            if (racine) { activer(racine); refleter(racine); }
        });
    });

    function initialiser() { document.querySelectorAll('[data-otp]').forEach(activer); }
    document.addEventListener('DOMContentLoaded', function () {
        initialiser();
        if (window.Blazor && typeof window.Blazor.addEventListener === 'function') {
            window.Blazor.addEventListener('enhancedload', initialiser);
        }
    });
})();

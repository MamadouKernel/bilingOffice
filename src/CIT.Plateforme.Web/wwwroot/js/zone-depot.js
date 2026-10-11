// Zone de dépôt (Shared/ZoneDepot.razor) : survol de glisser-déposer + déclenchement du « tir ».
// Écouteurs délégués sur document : survivent aux rendus Blazor, aucun interop nécessaire.
(() => {
    const zone = e => e.target.closest && e.target.closest('.zone-depot');
    const survol = e => { const z = zone(e); if (z) z.classList.add('zone-depot--over'); };
    const tir = e => {
        const z = zone(e);
        if (!z) return;
        z.classList.remove('zone-depot--over', 'zone-depot--tir');
        void z.offsetWidth; // relance l'animation si on enchaîne deux dépôts
        z.classList.add('zone-depot--tir');
        setTimeout(() => z.classList.remove('zone-depot--tir'), 1500);
    };
    document.addEventListener('dragenter', survol, true);
    document.addEventListener('dragover', survol, true);
    document.addEventListener('dragleave', e => {
        const z = zone(e);
        if (z && !z.contains(e.relatedTarget)) z.classList.remove('zone-depot--over');
    }, true);
    document.addEventListener('drop', tir, true);
    document.addEventListener('change', tir, true);
})();

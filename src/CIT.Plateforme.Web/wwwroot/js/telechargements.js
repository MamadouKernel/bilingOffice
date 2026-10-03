// FEATURE 005 (C6-05) : déclenche le téléchargement navigateur d'un fichier généré côté serveur
// (document PDF de simulation proforma) à partir de son contenu encodé en base64.
// APERÇU DANS L'APPLICATION : affiche une pièce jointe (PDF, image) dans l'élément indiqué
// (<iframe> ou <img>) à partir de son contenu base64, déjà contrôlé côté serveur. L'URL blob est
// propre à la page et libérée à la fermeture de l'aperçu.
window.apercuDocument = (() => {
    const urls = new Map();
    const versBlob = (typeContenu, base64) => {
        const octets = atob(base64);
        const tampon = new Uint8Array(octets.length);
        for (let i = 0; i < octets.length; i++) {
            tampon[i] = octets.charCodeAt(i);
        }
        return new Blob([tampon], { type: typeContenu });
    };
    return {
        // Ouvre la fenêtre d'aperçu en modal natif (couche supérieure) ; Échap reste géré par le
        // composant Blazor, qui retire la fenêtre — le navigateur ne la ferme pas de lui-même.
        afficher(dialogueId) {
            const dialogue = document.getElementById(dialogueId);
            if (!dialogue || dialogue.open) { return; }
            dialogue.addEventListener('cancel', (e) => e.preventDefault());
            dialogue.showModal();
        },
        ouvrir(elementId, typeContenu, base64) {
            const element = document.getElementById(elementId);
            if (!element) { return; }
            this.fermer(elementId);
            const url = URL.createObjectURL(versBlob(typeContenu, base64));
            urls.set(elementId, url);
            element.src = url;
        },
        fermer(elementId) {
            const url = urls.get(elementId);
            if (url) {
                URL.revokeObjectURL(url);
                urls.delete(elementId);
            }
        }
    };
})();

window.telechargerFichier = (nomFichier, typeContenu, base64) => {
    const octets = atob(base64);
    const tampon = new Uint8Array(octets.length);
    for (let i = 0; i < octets.length; i++) {
        tampon[i] = octets.charCodeAt(i);
    }
    const blob = new Blob([tampon], { type: typeContenu });
    const url = URL.createObjectURL(blob);
    const lien = document.createElement("a");
    lien.href = url;
    lien.download = nomFichier;
    document.body.appendChild(lien);
    lien.click();
    document.body.removeChild(lien);
    URL.revokeObjectURL(url);
};

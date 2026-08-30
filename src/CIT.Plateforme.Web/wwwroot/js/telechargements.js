// FEATURE 005 (C6-05) : déclenche le téléchargement navigateur d'un fichier généré côté serveur
// (document PDF de simulation proforma) à partir de son contenu encodé en base64.
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

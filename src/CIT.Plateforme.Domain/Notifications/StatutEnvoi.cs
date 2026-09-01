namespace CIT.Plateforme.Domain.Notifications;

/// <summary>
/// FEATURE 009 (T004) — « statut d'envoi », attribut exigé par le CDC §20 (« Modèle de données
/// minimal » : <c>Notification (canal, modèle, destinataire, statut d'envoi)</c>). Il est porté
/// par canal, ce qui rend l'isolation d'ERR-02 démontrable (spec.md §15).
///
/// AUCUN statut de terminaison (« abandonné », « épuisé ») n'est défini : le CDC exige une
/// nouvelle tentative sans fixer de condition d'arrêt — CLAR-NOT-02, à ne pas inventer.
/// </summary>
public enum StatutEnvoi
{
    EnAttente,

    Envoye,

    /// <summary>Le transport a échoué. L'envoi reste éligible à la reprise tant que le plafond
    /// configuré n'est pas atteint ; au-delà, il n'est plus sélectionné et conserve ce statut.</summary>
    Echec,

    /// <summary>La coordonnée requise est absente pour ce destinataire, ou le canal n'est pas
    /// servable. Distinct de <see cref="Echec"/> : rien n'a été tenté. La sémantique définitive
    /// reste à confirmer avant implémentation métier (CLAR-COM-04).</summary>
    NonDelivrable,
}

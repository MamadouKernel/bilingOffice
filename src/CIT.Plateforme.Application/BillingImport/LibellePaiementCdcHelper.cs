namespace CIT.Plateforme.Application.BillingImport;

/// <summary>
/// IMP-13 : le CDC nomme littéralement 3 statuts de paiement — « En attente paiement »,
/// « En attente validation paiement », « Payé confirmé » — alors que l'implémentation actuelle
/// s'appuie sur des noms de file (Facturation / Validation Paiement / Contrôle Documentaire).
/// Cet utilitaire ne fait qu'AFFICHER le libellé littéral attendu par le CDC, dérivé de la file
/// courante du ticket : il ne renomme ni l'énumération <c>StatutTicket</c>, ni le schéma, ni les
/// noms de file existants (<see cref="NomsFiles"/>) — purement un affichage de confort.
/// </summary>
public static class LibellePaiementCdcHelper
{
    public const string EnAttentePaiement = "En attente paiement";
    public const string EnAttenteValidationPaiement = "En attente validation paiement";
    public const string PayeConfirme = "Payé confirmé";

    /// <summary>Écart 06/10 #2 (mode déclaratif) : le client a déclaré le règlement, en attente de
    /// confirmation du facturier (ticket revenu en Facturation).</summary>
    public const string EnAttenteConfirmationReglement = "En attente de confirmation du règlement";

    /// <summary>
    /// Retourne le libellé littéral CDC (IMP-13) correspondant à la file courante d'un ticket
    /// Billing IMPORT, ou <c>null</c> si le ticket n'est pas dans une étape du cycle paiement
    /// (ex. Pilote Manifeste, ou tout autre module).
    /// </summary>
    public static string? PourFile(string module, string fileNom) => PourFile(module, fileNom, reglementDeclare: false);

    /// <summary>
    /// <paramref name="reglementDeclare"/> : en mode déclaratif, un ticket revenu en Facturation
    /// avec un règlement déclaré affiche « En attente de confirmation du règlement » plutôt que le
    /// générique « En attente paiement ».
    /// </summary>
    public static string? PourFile(string module, string fileNom, bool reglementDeclare)
    {
        if (module != SoumettreDemandeImportCommand.Module)
        {
            return null;
        }

        if (fileNom == NomsFiles.Facturation && reglementDeclare)
        {
            return EnAttenteConfirmationReglement;
        }

        return fileNom switch
        {
            NomsFiles.Facturation => EnAttentePaiement,
            NomsFiles.ValidationPaiement => EnAttenteValidationPaiement,
            NomsFiles.ControleDocumentaire => PayeConfirme,
            _ => null,
        };
    }
}

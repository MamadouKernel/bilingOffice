using CIT.Plateforme.Application.BillingExport;
using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>
/// Étape métier d'un ticket Billing telle que le client la comprend (parcours TO BE, documents
/// métier du 05/10/2026 : Soumis → Contrôle BL → BL validé → Proforma à valider → Facturé / en
/// attente de paiement → Paiement à valider → Paiement confirmé → Clôturé).
///
/// Elle se DÉDUIT de la file courante, du statut générique et de l'état de la proforma : aucun
/// second état n'est stocké, donc aucun risque de désynchronisation avec le workflow réel.
/// Retourne une clé de traduction (« Etape.* »), ou <c>null</c> hors parcours Billing.
/// </summary>
public static class EtapeParcoursClient
{
    public static string? Determiner(
        string module, string fileNom, StatutTicket statut, EtatProforma proforma, bool estRejete, bool preuvePaiementDeposee)
    {
        var import = module == SoumettreDemandeImportCommand.Module;
        if (!import && module != SoumettreDemandeExportCommand.Module)
        {
            return null;
        }

        if (statut == StatutTicket.Annule)
        {
            return estRejete ? "Etape.Rejete" : "Etape.Annule";
        }

        if (statut == StatutTicket.Cloture)
        {
            return "Etape.Cloture";
        }

        return fileNom switch
        {
            SoumettreDemandeImportCommand.FilePiloteManifeste or SoumettreDemandeExportCommand.FileBackOfficeExport => statut switch
            {
                StatutTicket.EnAttenteClient => "Etape.ComplementDemande",
                StatutTicket.EnAttenteDePriseEnCharge => "Etape.Soumis",
                _ => import ? "Etape.ControleBl" : "Etape.Codification",
            },
            NomsFiles.FileIes => "Etape.TraitementIes",
            NomsFiles.Facturation => proforma switch
            {
                EtatProforma.EnAttenteValidation => "Etape.ProformaAValider",
                EtatProforma.CorrectionDemandee => "Etape.ProformaEnCorrection",
                EtatProforma.Validee => "Etape.FactureEnPreparation",
                _ => import ? "Etape.BlValide" : "Etape.Codifie",
            },
            NomsFiles.ValidationPaiement => statut == StatutTicket.EnAttenteClient
                ? "Etape.NouvellePreuveAttendue"
                : preuvePaiementDeposee ? "Etape.PaiementAValider" : "Etape.EnAttentePaiement",
            NomsFiles.ComptabiliteClient => "Etape.LettrageEnCours",
            NomsFiles.ControleDocumentaire => "Etape.ControleDocumentaire",
            _ => null,
        };
    }
}

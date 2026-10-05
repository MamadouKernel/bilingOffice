using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Securite;

public record ResultatEligibiliteRetention(bool EstEligible, ActionRetention Action, string Motif);

/// <summary>
/// Moteur pur d'évaluation de l'éligibilité à la rétention et à la purge réglementaire ARTCI / OHADA (SEC-06).
/// Zéro dépendance directe sur DateTime.Now (invariants temporels testables).
/// </summary>
public sealed class MoteurRetentionService
{
    public ResultatEligibiliteRetention EvaluerEligibilite(
        CategorieRetention categorie,
        DateTime dateReferenceUtc,
        bool estSousGelLegal,
        DateTime dateCouranteUtc)
    {
        if (estSousGelLegal)
        {
            return new ResultatEligibiliteRetention(
                false,
                ActionRetention.Aucune,
                "Élément exempté de purge / anonymisation car sous gel légal (contentieux actif).");
        }

        return categorie switch
        {
            CategorieRetention.Brouillon => dateCouranteUtc >= dateReferenceUtc.AddDays(Exploitation.ParametresCourants.ConservationBrouillonJours)
                ? new ResultatEligibiliteRetention(true, ActionRetention.Purge, $"Brouillon inactif depuis plus de {Exploitation.ParametresCourants.ConservationBrouillonJours} jours.")
                : new ResultatEligibiliteRetention(false, ActionRetention.Aucune, $"Brouillon actif ou récent (< {Exploitation.ParametresCourants.ConservationBrouillonJours} jours)."),

            CategorieRetention.Dossier => dateCouranteUtc >= dateReferenceUtc.AddYears(Exploitation.ParametresCourants.ConservationDossierAnnees)
                ? new ResultatEligibiliteRetention(true, ActionRetention.Anonymisation, $"Dossier opérationnel clôturé depuis plus de {Exploitation.ParametresCourants.ConservationDossierAnnees} ans.")
                : new ResultatEligibiliteRetention(false, ActionRetention.Aucune, $"Dossier en cours ou clôturé depuis moins de {Exploitation.ParametresCourants.ConservationDossierAnnees} ans."),

            CategorieRetention.Comptable => dateCouranteUtc >= dateReferenceUtc.AddYears(Exploitation.ParametresCourants.ConservationComptableAnnees)
                ? new ResultatEligibiliteRetention(true, ActionRetention.ArchivageInalterable, "Pièce comptable décennale échue.")
                : new ResultatEligibiliteRetention(false, ActionRetention.ScellementArchive, "Pièce comptable sous conservation obligatoire décennale (scellée)."),

            CategorieRetention.Audit => dateCouranteUtc >= dateReferenceUtc.AddYears(Exploitation.ParametresCourants.ConservationAuditAnnees)
                ? new ResultatEligibiliteRetention(true, ActionRetention.ArchivageInalterable, "Événement d'audit supérieur au délai légal triennal.")
                : new ResultatEligibiliteRetention(false, ActionRetention.Aucune, "Événement d'audit récent sous conservation active."),

            _ => new ResultatEligibiliteRetention(false, ActionRetention.Aucune, "Catégorie inconnue.")
        };
    }
}

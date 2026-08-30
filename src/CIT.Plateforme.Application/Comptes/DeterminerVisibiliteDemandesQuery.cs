using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>
/// Filtre de visibilité applicable aux demandes (dossiers/tickets, Feature 001) d'un utilisateur
/// rattaché — contrat C2-11 (research.md D5, CLAR-01, CLAR-02). Le cloisonnement inter-société dur
/// (CPT-04) reste garanti séparément, tel quel, par
/// <see cref="CIT.Plateforme.Application.Securite.CloisonnementService.ExigerProprieteClientAsync"/>
/// (Feature 001, INCHANGÉ) — ce filtre porte uniquement la portée intra-société (ENT-03).
/// </summary>
public sealed class VisibiliteDemandesFiltre(Guid societeId, Guid? auteurIdRestriction)
{
    public Guid SocieteId { get; } = societeId;

    /// <summary>Non nul uniquement si l'appelant est restreint à « ses propres demandes »
    /// (ENT-03) — jamais pour un administrateur de société (CLAR-02) ni un droit total.</summary>
    public Guid? AuteurIdRestriction { get; } = auteurIdRestriction;

    public bool DemandeEstVisible(Dossier dossier) =>
        dossier.ClientId == SocieteId && (AuteurIdRestriction is null || dossier.AuteurId == AuteurIdRestriction);
}

/// <summary>Contrat C2-11 — Déterminer la visibilité applicable à une demande. CDC : ENT-03.</summary>
public sealed class DeterminerVisibiliteDemandesQuery(IUtilisateurSocieteRepository rattachements)
{
    public async Task<VisibiliteDemandesFiltre> ExecuterAsync(Guid utilisateurId, CancellationToken ct = default)
    {
        var rattachement = await rattachements.ObtenirParUtilisateurAsync(utilisateurId, ct).ConfigureAwait(false);
        if (rattachement is null || !rattachement.EstActif)
        {
            throw new InvalidOperationException($"L'utilisateur {utilisateurId} n'a aucun rattachement société actif (ENT-03).");
        }

        // CLAR-02 : un administrateur de société voit toujours l'ensemble des demandes de sa
        // société, indépendamment de son propre droit de consultation individuel.
        var visibiliteTotale = rattachement.EstAdministrateur || rattachement.DroitConsultation == DroitConsultation.ToutesDemandesSociete;

        return new VisibiliteDemandesFiltre(rattachement.SocieteId, visibiliteTotale ? null : utilisateurId);
    }
}

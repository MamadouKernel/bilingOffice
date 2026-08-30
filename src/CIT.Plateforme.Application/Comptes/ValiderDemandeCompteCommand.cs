using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>
/// Contrat C2-02 — Valider ou rejeter une demande de compte société. CDC : CPT-01, CPT-02.
/// Le demandeur (<see cref="SocieteCliente.DemandeurId"/>) devient automatiquement le premier
/// administrateur de société à la validation (spec.md § Assumptions).
/// </summary>
public sealed class ValiderOuRejeterDemandeCompteCommand(
    ISocieteClienteRepository societes,
    IUtilisateurSocieteRepository rattachements,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ValiderAsync(Guid societeId, Guid appelantId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.CompteValider, ct: ct).ConfigureAwait(false);

        var societe = await societes.ObtenirParIdAsync(societeId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Société {societeId} introuvable.");

        var ancienEtat = societe.Etat.ToString();
        societe.Valider();

        // Droit de consultation individuel laissé à sa valeur par défaut (PropresDemandesUniquement,
        // invariant §5.4 de data-model.md) : CLAR-02 garantit déjà une visibilité totale à tout
        // administrateur, indépendamment de ce paramètre individuel — pas d'exception nécessaire ici.
        var premierAdministrateur = UtilisateurSociete.Rattacher(
            societe.DemandeurId, societe.Id, horloge.MaintenantUtc, estAdministrateur: true, droitSoumission: true);
        societe.EnregistrerAdministrateurActif();
        await rattachements.AjouterAsync(premierAdministrateur, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(appelantId, "CompteValide", nameof(SocieteCliente), societe.Id, ancienEtat, societe.Etat.ToString(), ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    public async Task RejeterAsync(Guid societeId, Guid appelantId, string motif, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new ArgumentException("Le motif de rejet est obligatoire (traçabilité, Principe IX).", nameof(motif));
        }

        await autorisation.ExigerAsync(appelantId, Permission.CompteValider, ct: ct).ConfigureAwait(false);

        var societe = await societes.ObtenirParIdAsync(societeId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Société {societeId} introuvable.");

        var ancienEtat = societe.Etat.ToString();
        societe.Rejeter();

        await journalisation.ConsignerAuditAsync(appelantId, "CompteRejete", nameof(SocieteCliente), societe.Id, ancienEtat, motif, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

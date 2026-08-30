using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Comptes;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>
/// Contrat C2-01 — Soumettre une demande de compte société. CDC : CPT-01, CPT-02.
/// </summary>
/// <summary>
/// <paramref name="DemandeurId"/> : identifiant du compte Identity du demandeur (créé côté Web au
/// moment de la soumission — D1, aucune modification d'Identity) ; conservé pour désigner
/// automatiquement le premier administrateur de société à la validation (spec.md § Assumptions,
/// contrat C2-02).
/// </summary>
public sealed record SoumettreDemandeCompteRequete(Guid DemandeurId, string InformationsDeclarees, TypeClient ProfilDeclare);

public sealed class SoumettreDemandeCompteCommand(
    ISocieteClienteRepository societes,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<SocieteCliente> ExecuterAsync(SoumettreDemandeCompteRequete requete, CancellationToken ct = default)
    {
        var societe = SocieteCliente.Soumettre(requete.DemandeurId, requete.InformationsDeclarees, requete.ProfilDeclare, horloge.MaintenantUtc);
        await societes.AjouterAsync(societe, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(requete.DemandeurId, "DemandeCompteSoumise", nameof(SocieteCliente), societe.Id, null, requete.ProfilDeclare.ToString(), ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return societe;
    }
}

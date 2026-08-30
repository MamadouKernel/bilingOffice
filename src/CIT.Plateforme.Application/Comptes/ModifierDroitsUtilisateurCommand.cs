using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Comptes;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>Contrat C2-08 — Modifier les droits de consultation/soumission d'un utilisateur. CDC :
/// ENT-02 (journalisé, ENT-04).</summary>
public sealed class ModifierDroitsUtilisateurCommand(
    IUtilisateurSocieteRepository rattachements,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid appelantId, Guid cibleId, DroitConsultation droitConsultation, bool droitSoumission, CancellationToken ct = default)
    {
        var cible = await rattachements.ObtenirParIdAsync(cibleId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Rattachement {cibleId} introuvable.");

        await RattacherUtilisateurCommand.ExigerAdministrateurDeLaSocieteAsync(rattachements, appelantId, cible.SocieteId, ct).ConfigureAwait(false);

        var avant = $"{cible.DroitConsultation}/{cible.DroitSoumission}";
        cible.ModifierDroits(droitConsultation, droitSoumission);
        var apres = $"{cible.DroitConsultation}/{cible.DroitSoumission}";

        await journalisation.ConsignerAuditAsync(appelantId, "DroitsUtilisateurModifies", nameof(UtilisateurSociete), cible.Id, avant, apres, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

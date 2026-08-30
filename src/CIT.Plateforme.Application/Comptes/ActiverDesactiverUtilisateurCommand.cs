using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Comptes;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>
/// Contrat C2-07 — Activer ou désactiver un utilisateur rattaché. CDC : ENT-02 (journalisé, ENT-04).
/// Si la cible est
/// administratrice, l'invariant du dernier administrateur actif (CLAR-03,
/// <see cref="SocieteCliente.DecrementerAdministrateursActifs"/>) est appliqué avant toute
/// désactivation.
/// </summary>
public sealed class ActiverDesactiverUtilisateurCommand(
    IUtilisateurSocieteRepository rattachements,
    ISocieteClienteRepository societes,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ActiverAsync(Guid appelantId, Guid cibleId, CancellationToken ct = default)
    {
        var cible = await rattachements.ObtenirParIdAsync(cibleId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Rattachement {cibleId} introuvable.");

        await RattacherUtilisateurCommand.ExigerAdministrateurDeLaSocieteAsync(rattachements, appelantId, cible.SocieteId, ct).ConfigureAwait(false);

        if (cible.EstAdministrateur)
        {
            var societe = await societes.ObtenirParIdAsync(cible.SocieteId, ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Société {cible.SocieteId} introuvable.");
            societe.EnregistrerAdministrateurActif();
        }

        cible.Activer();

        await journalisation.ConsignerAuditAsync(appelantId, "UtilisateurActive", nameof(UtilisateurSociete), cible.Id, "Désactivé", "Actif", ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    public async Task DesactiverAsync(Guid appelantId, Guid cibleId, CancellationToken ct = default)
    {
        var cible = await rattachements.ObtenirParIdAsync(cibleId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Rattachement {cibleId} introuvable.");

        await RattacherUtilisateurCommand.ExigerAdministrateurDeLaSocieteAsync(rattachements, appelantId, cible.SocieteId, ct).ConfigureAwait(false);

        if (cible.EstAdministrateur)
        {
            var societe = await societes.ObtenirParIdAsync(cible.SocieteId, ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Société {cible.SocieteId} introuvable.");
            // CLAR-03 : lève DernierAdministrateurActifException si c'est le dernier — la
            // désactivation n'est appliquée qu'après ce contrôle, jamais avant.
            societe.DecrementerAdministrateursActifs();
        }

        cible.Desactiver();

        await journalisation.ConsignerAuditAsync(appelantId, "UtilisateurDesactive", nameof(UtilisateurSociete), cible.Id, "Actif", "Désactivé", ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

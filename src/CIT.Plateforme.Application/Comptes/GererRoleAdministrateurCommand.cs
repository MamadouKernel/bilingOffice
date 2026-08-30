using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Comptes;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>
/// Contrat C2-09 — Attribuer ou retirer le rôle Administrateur de société. CDC : ENT-02
/// (journalisé, ENT-04). Le
/// retrait du rôle du dernier administrateur actif est refusé (CLAR-03), y compris auto-retrait.
/// </summary>
public sealed class GererRoleAdministrateurCommand(
    IUtilisateurSocieteRepository rattachements,
    ISocieteClienteRepository societes,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task AttribuerRoleAsync(Guid appelantId, Guid cibleId, CancellationToken ct = default)
    {
        var cible = await rattachements.ObtenirParIdAsync(cibleId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Rattachement {cibleId} introuvable.");

        await RattacherUtilisateurCommand.ExigerAdministrateurDeLaSocieteAsync(rattachements, appelantId, cible.SocieteId, ct).ConfigureAwait(false);

        cible.AttribuerRoleAdministrateur();

        // REMÉDIATION POST-AUDIT (post-implementation-audit.md §6, dérive du compteur) :
        // NombreAdministrateursActifs ne compte que les administrateurs RÉELLEMENT actifs
        // (EstAdministrateur ET EstActif). Attribuer le rôle à un utilisateur désactivé ne doit
        // jamais incrémenter le compteur — il ne sera compté que s'il est (ré)activé ensuite
        // (ActiverDesactiverUtilisateurCommand.ActiverAsync gère alors l'incrément).
        if (cible.EstActif)
        {
            var societe = await societes.ObtenirParIdAsync(cible.SocieteId, ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Société {cible.SocieteId} introuvable.");
            societe.EnregistrerAdministrateurActif();
        }

        await journalisation.ConsignerAuditAsync(appelantId, "RoleAdministrateurAttribue", nameof(UtilisateurSociete), cible.Id, "Utilisateur", "Administrateur", ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    public async Task RetirerRoleAsync(Guid appelantId, Guid cibleId, CancellationToken ct = default)
    {
        var cible = await rattachements.ObtenirParIdAsync(cibleId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Rattachement {cibleId} introuvable.");

        await RattacherUtilisateurCommand.ExigerAdministrateurDeLaSocieteAsync(rattachements, appelantId, cible.SocieteId, ct).ConfigureAwait(false);

        // REMÉDIATION POST-AUDIT (§6) : symétrique de AttribuerRoleAsync — ne décrémente que si la
        // cible était réellement comptée (EstActif) ; CLAR-03 lève DernierAdministrateurActifException
        // si c'est le dernier, le retrait de rôle n'est appliqué qu'après ce contrôle.
        if (cible.EstActif)
        {
            var societe = await societes.ObtenirParIdAsync(cible.SocieteId, ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Société {cible.SocieteId} introuvable.");
            societe.DecrementerAdministrateursActifs();
        }

        cible.RetirerRoleAdministrateur();

        await journalisation.ConsignerAuditAsync(appelantId, "RoleAdministrateurRetire", nameof(UtilisateurSociete), cible.Id, "Administrateur", "Utilisateur", ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>Opérations d'identité nécessaires à la réinitialisation du second facteur.</summary>
public interface IDoubleAuthentificationAdministration
{
    /// <summary>
    /// Désactive la 2FA, invalide la clé d'authentification et les codes de secours, et renouvelle
    /// le jeton de sécurité (les sessions ouvertes sont révoquées). Renvoie faux si le compte
    /// est introuvable.
    /// </summary>
    Task<bool> ReinitialiserAsync(Guid utilisateurId, CancellationToken ct = default);
}

/// <summary>Un administrateur ne peut pas réinitialiser son propre second facteur.</summary>
public sealed class AutoReinitialisation2FaInterditeException()
    : InvalidOperationException("Un administrateur ne peut pas réinitialiser sa propre double authentification.");

/// <summary>
/// RÉINITIALISATION DE LA 2FA PAR UN ADMINISTRATEUR — pour l'utilisateur qui a perdu son
/// téléphone ET ses codes de secours. La 2FA étant obligatoire, l'utilisateur devra la
/// reconfigurer dès sa prochaine connexion (mot de passe inchangé).
///
/// Garde-fous : habilitation <see cref="Permission.AdministrationComptes"/> revérifiée côté
/// serveur ; interdiction de se l'appliquer à soi-même (un compte compromis ne peut pas
/// affaiblir son propre second facteur) ; action consignée à l'audit.
/// </summary>
public sealed class ReinitialiserDoubleAuthentificationCommand(
    IDoubleAuthentificationAdministration identite,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid appelantId, Guid cibleId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationComptes, ct: ct).ConfigureAwait(false);

        if (appelantId == cibleId)
        {
            throw new AutoReinitialisation2FaInterditeException();
        }

        if (!await identite.ReinitialiserAsync(cibleId, ct).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Compte {cibleId} introuvable.");
        }

        await journalisation.ConsignerAuditAsync(
            appelantId, "DoubleAuthentificationReinitialisee", "ApplicationUser", cibleId,
            "2FA active", "2FA à reconfigurer", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

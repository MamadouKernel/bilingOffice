using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Notifications;

namespace CIT.Plateforme.Application.Notifications;

/// <summary>
/// FEATURE 009 — CONTRAT C9-09 : préférences de canaux (COM-05, Should).
///
/// SEULE dimension prévue par le CDC : le canal. Ni fréquence, ni langue, ni type d'événement,
/// ni plage horaire, ni opt-out global (spec.md §21).
///
/// Un utilisateur ne modifie QUE ses propres préférences — vérifié côté serveur.
/// Le canal In-App n'est pas débrayable (invariant porté par le domaine).
/// Une règle obligatoire est diffusée MALGRÉ la désactivation (appliqué par le moteur, C9-01).
/// </summary>
public sealed class GererPreferencesCanauxCommand(
    IPreferenceCanalRepository preferences,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<IReadOnlyList<PreferenceCanalUtilisateur>> ListerMesPreferencesAsync(
        Guid utilisateurAuthentifieId, CancellationToken ct = default)
    {
        if (utilisateurAuthentifieId == Guid.Empty)
        {
            throw new AccesRefuseException("Accès refusé : authentification requise.");
        }

        return await preferences.ListerParUtilisateurAsync(utilisateurAuthentifieId, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Active ou désactive un canal POUR SOI-MÊME. L'identité vient du contexte authentifié :
    /// aucun paramètre ne permet d'agir sur les préférences d'un tiers.
    /// </summary>
    public async Task DefinirAsync(
        Guid utilisateurAuthentifieId, CanalNotification canal, bool actif, CancellationToken ct = default)
    {
        if (utilisateurAuthentifieId == Guid.Empty)
        {
            throw new AccesRefuseException("Accès refusé : authentification requise.");
        }

        if (canal == CanalNotification.InApp)
        {
            throw new ArgumentException(
                "Le canal In-App n'est pas débrayable : le désactiver rendrait NOT-01 et NOT-03 inatteignables.",
                nameof(canal));
        }

        var preference = await preferences.ObtenirAsync(utilisateurAuthentifieId, canal, ct).ConfigureAwait(false);

        if (preference is null)
        {
            preference = PreferenceCanalUtilisateur.Creer(utilisateurAuthentifieId, canal);
            await preferences.AjouterAsync(preference, ct).ConfigureAwait(false);
        }

        var avant = $"actif={preference.EstActif}";
        if (actif) { preference.Activer(); } else { preference.Desactiver(); }

        await journalisation.ConsignerAuditAsync(
            utilisateurAuthentifieId, "PreferenceCanalModifiee", nameof(PreferenceCanalUtilisateur),
            preference.Id, avant, $"canal={canal}; actif={actif}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

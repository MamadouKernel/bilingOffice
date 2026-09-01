using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Notifications;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Notifications;

/// <summary>
/// FEATURE 009 — CONTRAT C9-06 : administrer les modèles de messages (COM-03).
///
/// AUTORISATION VÉRIFIÉE CÔTÉ SERVEUR, indépendamment de ce que l'interface affiche
/// (Constitution Principe VII).
///
/// NON-RÉTROACTIVITÉ : modifier un modèle n'altère jamais une notification déjà émise — son
/// titre et son corps sont figés à la création (cas TEST-NOT-02-3).
///
/// AUCUN contenu de modèle n'est décidé ici : les textes sont une donnée d'administration, à
/// fournir avant recette (CLAR-NOT-05).
/// </summary>
public sealed class AdministrerModelesNotificationCommand(
    IModeleNotificationRepository modeles,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<ModeleNotification> CreerAsync(
        Guid administrateurId, string typeEvenement, CanalNotification canal, string gabaritTitre, string gabaritCorps, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationNotifications, null, ct).ConfigureAwait(false);

        var modele = ModeleNotification.Creer(typeEvenement, canal, gabaritTitre, gabaritCorps);
        await modeles.AjouterAsync(modele, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            administrateurId, "ModeleNotificationCree", nameof(ModeleNotification), modele.Id,
            null, $"evenement={typeEvenement}; canal={canal}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        return modele;
    }

    public async Task ModifierAsync(
        Guid administrateurId, Guid modeleId, string gabaritTitre, string gabaritCorps, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationNotifications, null, ct).ConfigureAwait(false);

        var modele = await modeles.ObtenirParIdAsync(modeleId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Modèle {modeleId} introuvable.");

        var avant = $"titre={modele.GabaritTitre}";
        modele.MettreAJour(gabaritTitre, gabaritCorps);

        await journalisation.ConsignerAuditAsync(
            administrateurId, "ModeleNotificationModifie", nameof(ModeleNotification), modele.Id,
            avant, $"titre={gabaritTitre}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Activation/désactivation — JAMAIS de suppression physique (patron F006).</summary>
    public async Task BasculerActivationAsync(Guid administrateurId, Guid modeleId, bool actif, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationNotifications, null, ct).ConfigureAwait(false);

        var modele = await modeles.ObtenirParIdAsync(modeleId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Modèle {modeleId} introuvable.");

        var avant = $"actif={modele.EstActif}";
        if (actif) { modele.Activer(); } else { modele.Desactiver(); }

        await journalisation.ConsignerAuditAsync(
            administrateurId, "ModeleNotificationModifie", nameof(ModeleNotification), modele.Id,
            avant, $"actif={actif}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<ModeleNotification>> ListerAsync(Guid administrateurId, CancellationToken ct = default) =>
        ListerApresAutorisationAsync(administrateurId, ct);

    private async Task<IReadOnlyList<ModeleNotification>> ListerApresAutorisationAsync(Guid administrateurId, CancellationToken ct)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationNotifications, null, ct).ConfigureAwait(false);
        return await modeles.ListerToutesAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>
/// FEATURE 009 — CONTRAT C9-07 : administrer les règles de diffusion (NOT-02).
///
/// GRANULARITÉ : modifier la règle d'un événement n'affecte JAMAIS un autre événement.
/// EFFET DANS LE TEMPS : le paramétrage s'applique aux événements survenant ensuite, sans
/// redéploiement ; les notifications déjà émises ne sont jamais modifiées.
///
/// La LISTE INITIALE des règles n'est pas décidée ici : seules les règles de niveau
/// CDC-EXIGENCE sont semées, et uniquement en développement (PC-17, CLAR-COM-02).
/// </summary>
public sealed class AdministrerReglesDiffusionCommand(
    IRegleDiffusionRepository regles,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<RegleDiffusion> CreerAsync(
        Guid administrateurId, string typeEvenement, CanalNotification canal, bool estObligatoire, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationNotifications, null, ct).ConfigureAwait(false);

        var regle = RegleDiffusion.Creer(typeEvenement, canal, estObligatoire);
        await regles.AjouterAsync(regle, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            administrateurId, "RegleDiffusionCreee", nameof(RegleDiffusion), regle.Id,
            null, $"evenement={typeEvenement}; canal={canal}; obligatoire={estObligatoire}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        return regle;
    }

    public async Task BasculerActivationAsync(Guid administrateurId, Guid regleId, bool actif, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationNotifications, null, ct).ConfigureAwait(false);

        var regle = await regles.ObtenirParIdAsync(regleId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Règle {regleId} introuvable.");

        var avant = $"actif={regle.EstActif}";
        if (actif) { regle.Activer(); } else { regle.Desactiver(); }

        await journalisation.ConsignerAuditAsync(
            administrateurId, "RegleDiffusionModifiee", nameof(RegleDiffusion), regle.Id,
            avant, $"actif={actif}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    public async Task DefinirCaractereObligatoireAsync(Guid administrateurId, Guid regleId, bool estObligatoire, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationNotifications, null, ct).ConfigureAwait(false);

        var regle = await regles.ObtenirParIdAsync(regleId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Règle {regleId} introuvable.");

        var avant = $"obligatoire={regle.EstObligatoire}";
        regle.MettreAJour(estObligatoire);

        await journalisation.ConsignerAuditAsync(
            administrateurId, "RegleDiffusionModifiee", nameof(RegleDiffusion), regle.Id,
            avant, $"obligatoire={estObligatoire}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RegleDiffusion>> ListerAsync(Guid administrateurId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationNotifications, null, ct).ConfigureAwait(false);
        return await regles.ListerToutesAsync(ct).ConfigureAwait(false);
    }
}

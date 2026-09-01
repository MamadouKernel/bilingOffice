using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Audit;
using CIT.Plateforme.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>
/// FEATURE 009 (T040) — accès aux données du moteur de notification et du centre in-app.
/// Aucun repository n'expose de suppression : le CDC ne prévoit ni purge, ni archivage
/// (CLAR-NOT-03, research.md D13).
/// </summary>
public sealed class NotificationRepository(PlateformeDbContext dbContext) : INotificationRepository
{
    public async Task AjouterAsync(Notification notification, CancellationToken ct = default) =>
        await dbContext.Notifications.AddAsync(notification, ct).ConfigureAwait(false);

    public async Task<Notification?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct).ConfigureAwait(false);

    /// <summary>Cloisonnement (spec.md §14, règle 1) : l'égalité stricte sur le destinataire est
    /// appliquée DANS la requête SQL, jamais après chargement en mémoire.</summary>
    public async Task<IReadOnlyList<Notification>> ListerParDestinataireAsync(Guid destinataireId, CancellationToken ct = default) =>
        await dbContext.Notifications
            .Where(n => n.DestinataireId == destinataireId)
            .OrderByDescending(n => n.CreeeLeUtc)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<int> CompterNonLuesAsync(Guid destinataireId, CancellationToken ct = default) =>
        await dbContext.Notifications
            .CountAsync(n => n.DestinataireId == destinataireId && !n.EstLue, ct)
            .ConfigureAwait(false);

    public async Task<bool> ExisteDejaAsync(Guid evenementSourceId, Guid destinataireId, CancellationToken ct = default) =>
        await dbContext.Notifications
            .AnyAsync(n => n.EvenementSourceId == evenementSourceId && n.DestinataireId == destinataireId, ct)
            .ConfigureAwait(false);
}

public sealed class EnvoiNotificationRepository(PlateformeDbContext dbContext) : IEnvoiNotificationRepository
{
    public async Task AjouterAsync(EnvoiNotification envoi, CancellationToken ct = default) =>
        await dbContext.EnvoisNotification.AddAsync(envoi, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<EnvoiNotification>> ListerParNotificationAsync(Guid notificationId, CancellationToken ct = default) =>
        await dbContext.EnvoisNotification
            .Where(e => e.NotificationId == notificationId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <summary>
    /// ERR-02 — sélection des envois éligibles à une nouvelle tentative. Le plafond
    /// <paramref name="nombreTentativesMax"/> et la borne <paramref name="pasTenteApresUtc"/>
    /// proviennent EXCLUSIVEMENT de la configuration : aucune valeur par défaut n'est écrite ici
    /// (CLAR-NOT-02, tasks.md invariant I2).
    ///
    /// Au plafond, l'envoi cesse d'être sélectionné et CONSERVE son statut Echec tracé — aucun
    /// statut « abandonné » n'est inventé.
    /// </summary>
    public async Task<IReadOnlyList<EnvoiNotification>> ListerAReessayerAsync(
        int nombreTentativesMax, DateTime pasTenteApresUtc, CancellationToken ct = default) =>
        await dbContext.EnvoisNotification
            .Where(e => (e.Statut == StatutEnvoi.EnAttente || e.Statut == StatutEnvoi.Echec)
                        && e.NombreTentatives < nombreTentativesMax
                        && (e.DerniereTentativeUtc == null || e.DerniereTentativeUtc <= pasTenteApresUtc))
            .OrderBy(e => e.DerniereTentativeUtc)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}

public sealed class ModeleNotificationRepository(PlateformeDbContext dbContext) : IModeleNotificationRepository
{
    public async Task AjouterAsync(ModeleNotification modele, CancellationToken ct = default) =>
        await dbContext.ModelesNotification.AddAsync(modele, ct).ConfigureAwait(false);

    public async Task<ModeleNotification?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.ModelesNotification.FirstOrDefaultAsync(m => m.Id == id, ct).ConfigureAwait(false);

    public async Task<ModeleNotification?> ObtenirActifAsync(string typeEvenement, CanalNotification canal, CancellationToken ct = default) =>
        await dbContext.ModelesNotification
            .FirstOrDefaultAsync(m => m.TypeEvenement == typeEvenement && m.Canal == canal && m.EstActif, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<ModeleNotification>> ListerToutesAsync(CancellationToken ct = default) =>
        await dbContext.ModelesNotification
            .OrderBy(m => m.TypeEvenement).ThenBy(m => m.Canal)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}

public sealed class RegleDiffusionRepository(PlateformeDbContext dbContext) : IRegleDiffusionRepository
{
    public async Task AjouterAsync(RegleDiffusion regle, CancellationToken ct = default) =>
        await dbContext.ReglesDiffusion.AddAsync(regle, ct).ConfigureAwait(false);

    public async Task<RegleDiffusion?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.ReglesDiffusion.FirstOrDefaultAsync(r => r.Id == id, ct).ConfigureAwait(false);

    public async Task<RegleDiffusion?> ObtenirAsync(string typeEvenement, CanalNotification canal, CancellationToken ct = default) =>
        await dbContext.ReglesDiffusion
            .FirstOrDefaultAsync(r => r.TypeEvenement == typeEvenement && r.Canal == canal, ct)
            .ConfigureAwait(false);

    /// <summary>Une liste vide signifie « ne pas notifier » : le silence du paramétrage ne
    /// produit aucune notification (spec.md §10).</summary>
    public async Task<IReadOnlyList<RegleDiffusion>> ListerActivesParEvenementAsync(string typeEvenement, CancellationToken ct = default) =>
        await dbContext.ReglesDiffusion
            .Where(r => r.TypeEvenement == typeEvenement && r.EstActif)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<RegleDiffusion>> ListerToutesAsync(CancellationToken ct = default) =>
        await dbContext.ReglesDiffusion
            .OrderBy(r => r.TypeEvenement).ThenBy(r => r.Canal)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}

public sealed class PreferenceCanalRepository(PlateformeDbContext dbContext) : IPreferenceCanalRepository
{
    public async Task AjouterAsync(PreferenceCanalUtilisateur preference, CancellationToken ct = default) =>
        await dbContext.PreferencesCanalUtilisateur.AddAsync(preference, ct).ConfigureAwait(false);

    public async Task<PreferenceCanalUtilisateur?> ObtenirAsync(Guid utilisateurId, CanalNotification canal, CancellationToken ct = default) =>
        await dbContext.PreferencesCanalUtilisateur
            .FirstOrDefaultAsync(p => p.UtilisateurId == utilisateurId && p.Canal == canal, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<PreferenceCanalUtilisateur>> ListerParUtilisateurAsync(Guid utilisateurId, CancellationToken ct = default) =>
        await dbContext.PreferencesCanalUtilisateur
            .Where(p => p.UtilisateurId == utilisateurId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}

/// <summary>
/// FEATURE 009 — lecture et prise du journal d'événements par le moteur (research.md D1/D2).
/// </summary>
public sealed class EvenementSocleRepository(PlateformeDbContext dbContext) : IEvenementSocleRepository
{
    public async Task<IReadOnlyList<EvenementSocle>> ListerNonDistribuesAsync(int limite, CancellationToken ct = default) =>
        await dbContext.JournalEvenements
            .Where(e => e.DistribueLeUtc == null)
            .OrderBy(e => e.HorodatageUtc)
            .Take(limite)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<EvenementSocle?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.JournalEvenements.FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false);

    /// <summary>
    /// PRISE ATOMIQUE (tasks.md T192, garantie I9) — première ligne de défense contre la double
    /// distribution concurrente. L'UPDATE conditionnel sur <c>DistribueLeUtc IS NULL</c> est
    /// exécuté par le serveur SQL en une seule opération : deux workers simultanés ne peuvent pas
    /// tous deux obtenir 1 ligne affectée.
    ///
    /// La seconde ligne de défense reste l'index unique (EvenementSourceId, DestinataireId) sur
    /// Notification, qui rend l'effet observable « effectively-once » même si cette prise était
    /// franchie. Aucune garantie de livraison unique stricte n'est revendiquée.
    /// </summary>
    public async Task<bool> TenterDePrendreAsync(Guid evenementId, DateTime maintenantUtc, CancellationToken ct = default)
    {
        var lignesAffectees = await dbContext.JournalEvenements
            .Where(e => e.Id == evenementId && e.DistribueLeUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.DistribueLeUtc, maintenantUtc), ct)
            .ConfigureAwait(false);

        return lignesAffectees == 1;
    }
}

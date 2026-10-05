using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Securite;

/// <summary>Contrat C-25 — Consulter le journal d'audit. CDC : SOC-13. Réservé à l'administrateur
/// technique (SEC-05 cité) ; distinct de l'historique fonctionnel du ticket.</summary>
public interface IJournalAuditLecture
{
    Task<IReadOnlyList<EntreeJournalAuditVue>> ListerAsync(Guid? entiteId, CancellationToken ct = default);

    /// <summary>
    /// UX-P1 (défaut UI-04) — lecture PAGINÉE. Le journal d'audit croît sans borne : le charger
    /// entièrement dégrade la page et la base à mesure que la plateforme sert.
    /// </summary>
    Task<(IReadOnlyList<EntreeJournalAuditVue> Entrees, int Total)> ListerPageAsync(
        Guid? entiteId, int page, int taillePage, CancellationToken ct = default);
}

/// <summary>
/// Une page du journal d'audit, avec de quoi se situer dans l'ensemble. Les bornes sont calculées
/// ici plutôt qu'à l'écran : deux pages qui les recalculeraient finiraient par diverger.
/// </summary>
public sealed record PageJournalAudit(
    IReadOnlyList<EntreeJournalAuditVue> Entrees,
    int Total,
    int Page,
    int TaillePage)
{
    public int NombrePages => Total <= 0 ? 1 : (int)Math.Ceiling(Total / (double)TaillePage);

    public bool APagePrecedente => Page > 1;

    public bool APageSuivante => Page < NombrePages;

    /// <summary>Rang du premier élément affiché, base 1. <c>0</c> si la page est vide.</summary>
    public int PremierRang => Entrees.Count == 0 ? 0 : ((Page - 1) * TaillePage) + 1;

    public int DernierRang => Entrees.Count == 0 ? 0 : PremierRang + Entrees.Count - 1;
}

/// <summary>
/// Une entrée du journal d'audit, avec l'auteur RÉSOLU.
///
/// REMÉDIATION UI/UX (D-03) : les 111 entrées affichaient toutes le libellé « Utilisateur ». Un
/// registre d'imputabilité qui n'impute personne ne remplit pas sa fonction. La donnée existait —
/// <see cref="AuteurId"/> — mais n'était jamais résolue en identité lisible.
///
/// <para><b>La clé stable reste l'identifiant.</b> Le nom d'un compte peut changer ; l'identifiant,
/// non. L'affichage montre les deux : une identité lisible pour l'humain, l'identifiant pour la
/// traçabilité durable. On ne remplace jamais l'un par l'autre.</para>
/// </summary>
public sealed record EntreeJournalAuditVue(
    Guid? AuteurId,
    string Action,
    string EntiteType,
    Guid EntiteId,
    DateTime HorodatageUtc,
    string? AuteurNomAffiche = null,
    string? AuteurEmail = null,
    NatureUtilisateur? AuteurNature = null)
{
    /// <summary>
    /// Identité lisible de l'auteur. Trois cas, dans cet ordre : le socle (aucun auteur), un compte
    /// résolu (son nom, à défaut son e-mail), ou un auteur dont le compte n'existe plus — l'entrée
    /// reste imputée à son identifiant, jamais effacée.
    /// </summary>
    public string AuteurLibelle => AuteurId is null
        ? LangueCourante.T("Système CIT", "CIT system")
        : !string.IsNullOrWhiteSpace(AuteurNomAffiche) ? AuteurNomAffiche!
        : !string.IsNullOrWhiteSpace(AuteurEmail) ? AuteurEmail!
        : "Compte supprimé";

    /// <summary>Vrai lorsqu'un auteur est référencé mais que son compte est introuvable.</summary>
    public bool AuteurIntrouvable =>
        AuteurId is not null
        && string.IsNullOrWhiteSpace(AuteurNomAffiche)
        && string.IsNullOrWhiteSpace(AuteurEmail);
}

public sealed class ConsulterJournalAuditQuery(IJournalAuditLecture lecture, IAutorisationService autorisation)
{
    /// <summary>
    /// UX-P1 (UI-04) — consultation paginée. La taille de page est bornée côté serveur : une valeur
    /// venue de l'appelant ne doit pas pouvoir redemander la table entière.
    /// </summary>
    public async Task<PageJournalAudit> ExecuterPageAsync(
        Guid administrateurTechniqueId, int page, int taillePage, Guid? entiteId = null, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurTechniqueId, Permission.AuditConsulter, ct: ct).ConfigureAwait(false);

        var pageNormalisee = Math.Max(1, page);
        var tailleNormalisee = Math.Clamp(taillePage, 10, 200);

        var (entrees, total) = await lecture
            .ListerPageAsync(entiteId, pageNormalisee, tailleNormalisee, ct)
            .ConfigureAwait(false);

        return new PageJournalAudit(entrees, total, pageNormalisee, tailleNormalisee);
    }

    public async Task<IReadOnlyList<EntreeJournalAuditVue>> ExecuterAsync(Guid administrateurTechniqueId, Guid? entiteId = null, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurTechniqueId, Permission.AuditConsulter, ct: ct).ConfigureAwait(false);
        return await lecture.ListerAsync(entiteId, ct).ConfigureAwait(false);
    }
}

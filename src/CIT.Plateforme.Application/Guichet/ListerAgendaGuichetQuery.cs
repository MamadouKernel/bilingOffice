using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Guichet;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Guichet;

/// <summary>
/// CON-03 — agenda des rendez-vous du guichet pour une journée.
///
/// DÉFAUT CORRIGÉ (audit du 2026-09-04, <c>docs/89</c>) : l'écran d'agenda injectait
/// <c>IRendezVousRepository</c> directement. Un repository ne porte aucun contrôle
/// d'habilitation — c'est le cas d'usage qui l'applique (Principe VII). La page vérifiait bien
/// la permission pour AFFICHER ses commandes, mais la lecture, elle, n'était gardée par rien.
///
/// Cette requête rétablit l'ordre : l'habilitation est exigée avant toute lecture, du côté
/// serveur, comme pour tout autre cas d'usage.
/// </summary>
public sealed class ListerAgendaGuichetQuery(
    IRendezVousRepository rendezVous,
    IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<RendezVous>> ExecuterAsync(
        Guid appelantId, DateOnly journee, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.RendezVousGerer, ct: ct).ConfigureAwait(false);

        return await rendezVous.ListerParDateAsync(journee, ct).ConfigureAwait(false);
    }
}

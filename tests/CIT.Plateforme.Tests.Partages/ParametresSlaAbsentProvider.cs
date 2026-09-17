using CIT.Plateforme.Application.Supervision;
using CIT.Plateforme.Domain.SuiviTemps;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Tests.Partages;

/// <summary>
/// Fournisseur SLA de test qui ne configure jamais de SLA (PC-12 : rien à surveiller, rien à
/// inventer). Utile pour les tests de supervision qui ne portent pas sur le calcul SLA lui-même
/// et ont seulement besoin de construire <see cref="SurveillerEcheancesUseCase"/> ou
/// <see cref="VueFilesQuery"/> sans configuration SLA réelle.
/// </summary>
public sealed class ParametresSlaAbsentProvider : IParametresSlaProvider
{
    public Task<(ParametresSla Parametres, CalendrierOuvre Calendrier)?> ObtenirPourAsync(Ticket ticket, CancellationToken ct = default) =>
        Task.FromResult<(ParametresSla, CalendrierOuvre)?>(null);
}

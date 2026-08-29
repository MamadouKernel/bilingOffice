using Microsoft.AspNetCore.SignalR;

namespace CIT.Plateforme.Web.Hubs;

/// <summary>
/// Transport temps réel de la supervision (SUP-01..03, decision D2). SignalR ne porte AUCUN état
/// métier — il ne fait que pousser les vues déjà calculées par <c>VueFilesQuery</c>/<c>VueChargeQuery</c>
/// (source de vérité = base de données via les cas d'usage Application, règle 11). Ce hub est un
/// simple canal de diffusion.
/// </summary>
public sealed class SupervisionHub : Hub
{
    public const string CheminHub = "/hubs/supervision";
}

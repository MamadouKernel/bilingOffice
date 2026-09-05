using CIT.Plateforme.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace CIT.Plateforme.Infrastructure.Notifications;

/// <summary>
/// Construit les liens du portail à partir de <c>Portail:UrlBase</c>.
///
/// Aucune valeur par défaut n'est inventée en production : si l'adresse n'est pas configurée, le
/// lien est relatif (« /tickets/… »). C'est exact dans le centre de notifications du portail, et
/// visiblement incomplet dans un e-mail — ce qui se corrige par configuration, alors qu'une
/// adresse devinée aurait produit des liens silencieusement erronés.
/// </summary>
public sealed class LienPortailConfigure(IConfiguration configuration) : ILienPortail
{
    public string VersTicket(Guid ticketId)
    {
        var chemin = $"/tickets/{ticketId}";
        var baseUrl = configuration["Portail:UrlBase"]?.TrimEnd('/');

        return string.IsNullOrWhiteSpace(baseUrl) ? chemin : baseUrl + chemin;
    }
}

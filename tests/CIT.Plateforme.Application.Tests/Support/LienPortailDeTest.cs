using CIT.Plateforme.Application.Abstractions;

namespace CIT.Plateforme.Application.Tests.Support;

/// <summary>
/// Fabrique de liens déterministe pour les tests (RG-COM-01). L'adresse réelle du portail est une
/// donnée de configuration ; les tests n'ont besoin que d'un lien stable et reconnaissable.
/// </summary>
public sealed class LienPortailDeTest : ILienPortail
{
    public const string UrlBase = "https://portail.test.cit";

    public string VersTicket(Guid ticketId) => $"{UrlBase}/tickets/{ticketId}";

    public string VersConnexion() => $"{UrlBase}/comptes/connexion";
}

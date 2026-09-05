namespace CIT.Plateforme.Application.Abstractions;

/// <summary>
/// RG-COM-01 / SEC-12 — fabrique le lien direct vers une ressource du portail, tel qu'il figure
/// dans les notifications.
///
/// PORT PLUTÔT QUE CHAÎNE EN DUR : l'adresse publique du portail diffère entre développement,
/// recette et production, et une notification archivée doit rester lisible. Elle est donc lue de
/// la configuration, jamais écrite dans le code.
///
/// SEC-12 rappelle qu'un tel lien ne donne <b>jamais</b> d'accès direct : il conduit au ticket,
/// derrière l'authentification et le contrôle d'habilitation, comme toute autre route du portail.
/// </summary>
public interface ILienPortail
{
    /// <summary>Lien absolu vers la fiche d'un ticket.</summary>
    string VersTicket(Guid ticketId);
}

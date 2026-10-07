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

    /// <summary>Lien de repli vers l'entrée du portail (connexion) — utilisé comme bouton
    /// d'action quand une notification n'est rattachée à aucun ticket (ex. décision de compte),
    /// afin que TOUT e-mail porte un bouton « aller sur le site ».</summary>
    string VersConnexion();
}

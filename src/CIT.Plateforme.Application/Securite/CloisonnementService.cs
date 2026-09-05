using CIT.Plateforme.Application.Abstractions;

namespace CIT.Plateforme.Application.Securite;

/// <summary>
/// FAÇADE de cloisonnement pour la voie client. <b>Ce service ne décide plus rien lui-même</b> :
/// il résout le ticket et transmet la décision à <see cref="ControleAccesRessourceService"/>.
///
/// POURQUOI IL NE DÉCIDE PLUS — CONSOLIDATION SÉCURITÉ.
/// Il portait sa propre règle de propriété :
/// <code>dossier.ClientId == clientId</code>
/// Seconde implémentation générique, servant douze commandes et requêtes de la voie client, et
/// diverger était inévitable. Elle a divergé sur trois points :
///
/// <list type="number">
///   <item><b>Elle ignorait l'état du compte.</b> Ni existence, ni <c>EstActif</c> : un compte
///   désactivé conservait ses actions clientes.</item>
///
///   <item><b>Elle ignorait le rattachement société et ENT-03.</b> <c>Dossier.ClientId</c> porte la
///   SOCIÉTÉ pour les parcours Billing, Réclamation et Avoir, et l'UTILISATEUR pour l'Enrôlement.
///   Comparer un identifiant d'utilisateur à une société échoue donc toujours sur les parcours
///   société — le contrôle refusait les clients légitimes.</item>
///
///   <item><b>Elle était contournée par l'interface, et le code le documentait.</b> Pour lever ce
///   refus, <c>FicheTicket.razor</c> passait <c>_fiche.SocieteId</c> à la place de l'utilisateur.
///   L'appelant fournissait ainsi la valeur à laquelle le contrôle se compare : celui-ci ne pouvait
///   plus que l'accepter. Effet de bord, l'auteur des messages clients devenait la société, et non
///   la personne — RG-SEC-03 (traçabilité nominative) et ENT-03 devenaient inapplicables.</item>
/// </list>
///
/// Le paramètre s'appelle désormais <c>utilisateurId</c>, et non <c>clientId</c> : le nom disait
/// « client » là où le code recevait tantôt un utilisateur, tantôt une société. C'est cette
/// ambiguïté qui a rendu le contournement possible sans qu'il paraisse anormal.
///
/// CE QUI RESTE ICI : la résolution du ticket, et rien d'autre. Aucune comparaison, aucune règle.
/// </summary>
public sealed class CloisonnementService(
    ITicketRepository tickets,
    ControleAccesRessourceService controleAcces)
{
    /// <summary>
    /// Forme booléenne. La décision appartient entièrement au contrôle central : identité valide,
    /// compte actif, propriété nominative ou rattachement société actif, ENT-03, portée de file.
    /// </summary>
    public async Task<bool> ClientEstProprietaireDuTicketAsync(
        Guid utilisateurId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false);
        if (ticket is null)
        {
            // Refus par défaut : une ressource absente n'autorise pas.
            return false;
        }

        return await controleAcces.PeutConsulterLeTicketAsync(utilisateurId, ticket, ct).ConfigureAwait(false);
    }

    /// <summary>Même décision, sous forme exigeante.</summary>
    public async Task ExigerProprieteClientAsync(
        Guid utilisateurId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new AccesRefuseException(
                $"L'utilisateur {utilisateurId} n'est pas autorisé à agir sur le ticket {ticketId} (SEC-01).");

        await controleAcces.ExigerConsultationTicketAsync(utilisateurId, ticket, ct).ConfigureAwait(false);
    }
}

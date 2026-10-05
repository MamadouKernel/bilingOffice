using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Notifications.Resolveurs;

/// <summary>
/// Demande de réaffectation ou escalade d'un ticket : notifie les superviseurs de la file (habilités à
/// réaffecter, sur cette file ou sur toutes) — et, pour une demande de réaffectation, l'agent qui l'a en
/// charge. Aucun client n'est jamais destinataire.
/// </summary>
public sealed class ResolveurSuperviseursDeLaFile(
    ITicketRepository tickets,
    IHabilitationRepository habilitations) : IResolveurDestinataires
{
    public const string TypeEvenement = "ReaffectationDemandee";

    public const string TypeEscalade = "EscaladeSuperviseur";

    public const string TypeDemandeAnnulation = "DemandeAnnulationSoumise";

    public bool PeutResoudre(string typeEvenement) => typeEvenement is TypeEvenement or TypeEscalade or TypeDemandeAnnulation;

    public async Task<IReadOnlyList<Guid>> ResoudreAsync(string typeEvenement, string chargeUtile, CancellationToken ct = default)
    {
        if (!PeutResoudre(typeEvenement) || ResolveurChargeUtile.LireGuid(chargeUtile, "ticketId") is not Guid ticketId
            || await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false) is not { } ticket)
        {
            return [];
        }

        var destinataires = (await habilitations.ListerUtilisateursAvecPermissionAsync(Permission.TicketReaffecter, ticket.FileCouranteId, ct).ConfigureAwait(false)).ToList();
        if (typeEvenement is TypeEvenement or TypeDemandeAnnulation && ticket.OperateurAssigneId is Guid affecte)
        {
            destinataires.Add(affecte);
        }

        return destinataires.Distinct().ToList();
    }

    public Task<Guid?> ResoudreTicketAsync(string chargeUtile, CancellationToken ct = default) =>
        Task.FromResult(ResolveurChargeUtile.LireGuid(chargeUtile, "ticketId"));
}

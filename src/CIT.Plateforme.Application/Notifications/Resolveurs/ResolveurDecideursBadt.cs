using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Notifications.Resolveurs;

/// <summary>
/// FEATURE 010 (Actualisation BADT) — escalade : lorsqu'une ligne est qualifiée « terminale », la
/// décision revient au Décideur Actualisateur BADT. Ce résolveur cible les utilisateurs détenant la
/// permission <see cref="Permission.ActualisationBadtDecider"/>, afin qu'ils soient informés qu'une
/// demande attend leur décision. La permission est portée globalement (non bornée à une file) :
/// l'habilitation a <c>FileId == null</c>, ce que la requête du dépôt couvre.
/// </summary>
public sealed class ResolveurDecideursBadt(IHabilitationRepository habilitations) : IResolveurDestinataires
{
    public const string TypeEscalade = "BadtLigneEscaladee";

    public bool PeutResoudre(string typeEvenement) => typeEvenement == TypeEscalade;

    public async Task<IReadOnlyList<Guid>> ResoudreAsync(string typeEvenement, string chargeUtile, CancellationToken ct = default)
    {
        if (!PeutResoudre(typeEvenement))
        {
            return [];
        }

        // Permission portée globalement (FileId null) : Guid.Empty ne restreint rien, le dépôt
        // retient les habilitations dont FileId est null.
        return await habilitations.ListerUtilisateursAvecPermissionAsync(Permission.ActualisationBadtDecider, Guid.Empty, ct).ConfigureAwait(false);
    }

    public Task<Guid?> ResoudreTicketAsync(string chargeUtile, CancellationToken ct = default) =>
        Task.FromResult(ResolveurChargeUtile.LireGuid(chargeUtile, "ticketId"));
}

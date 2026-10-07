using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Notifications.Resolveurs;

/// <summary>
/// Notifie les agents habilités à VALIDER les comptes société (<see cref="Permission.CompteValider"/>)
/// lorsqu'une demande de compte est soumise (événement <see cref="TypeDemande"/>). Symétrique de
/// <see cref="ResolveurAgentsDeLaFile"/> pour l'onboarding : la demande arrive dans la file
/// « Demandes de compte » (écran /comptes/valider), on prévient ceux qui la traitent.
/// </summary>
public sealed class ResolveurValideursCompte(IHabilitationRepository habilitations) : IResolveurDestinataires
{
    public const string TypeDemande = "DemandeCompteSoumise";

    public bool PeutResoudre(string typeEvenement) => typeEvenement == TypeDemande;

    public async Task<IReadOnlyList<Guid>> ResoudreAsync(string typeEvenement, string chargeUtile, CancellationToken ct = default)
    {
        if (!PeutResoudre(typeEvenement))
        {
            return [];
        }

        // CompteValider est une permission GLOBALE (non bornée à une file) : la requête, en retenant
        // FileId == null OU == fileId, renvoie les détenteurs globaux pour n'importe quel fileId
        // passé — on passe Guid.Empty.
        return await habilitations.ListerUtilisateursAvecPermissionAsync(Permission.CompteValider, Guid.Empty, ct).ConfigureAwait(false);
    }

    public Task<Guid?> ResoudreTicketAsync(string chargeUtile, CancellationToken ct = default) =>
        Task.FromResult<Guid?>(null);
}

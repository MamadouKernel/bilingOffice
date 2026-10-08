using CIT.Plateforme.Application.Abstractions;

namespace CIT.Plateforme.Application.Notifications.Resolveurs;

/// <summary>
/// Validation interne « 4-yeux » (incrément 3, 08/10/2026) — à la soumission d'un enrôlement parqué,
/// notifie les VALIDATEURS de la société concernée (rattachements actifs portant DroitValidation).
/// Charge utile : <c>{"societeId":"…","dossierId":"…"}</c>.
/// </summary>
public sealed class ResolveurValidateursSociete(IUtilisateurSocieteRepository rattachements) : IResolveurDestinataires
{
    public const string TypeEvenement = "EnrolementAValiderInterne";

    public bool PeutResoudre(string typeEvenement) => typeEvenement == TypeEvenement;

    public async Task<IReadOnlyList<Guid>> ResoudreAsync(string typeEvenement, string chargeUtile, CancellationToken ct = default)
    {
        if (!PeutResoudre(typeEvenement) || ResolveurChargeUtile.LireGuid(chargeUtile, "societeId") is not Guid societeId)
        {
            return [];
        }

        var membres = await rattachements.ListerParSocieteAsync(societeId, ct).ConfigureAwait(false);
        return [.. membres.Where(r => r.EstActif && r.DroitValidation).Select(r => r.UtilisateurId).Distinct()];
    }

    public Task<Guid?> ResoudreTicketAsync(string chargeUtile, CancellationToken ct = default) => Task.FromResult<Guid?>(null);
}

/// <summary>
/// Validation interne « 4-yeux » (incrément 3) — notifie le PRÉPARATEUR de l'enrôlement de la
/// décision du validateur (validé en interne / rejeté en interne). Charge utile :
/// <c>{"preparateurId":"…","dossierId":"…"}</c>.
/// </summary>
public sealed class ResolveurPreparateurEnrolement : IResolveurDestinataires
{
    public const string TypeValide = "EnrolementValideInterne";
    public const string TypeRejete = "EnrolementRejeteInterne";

    public bool PeutResoudre(string typeEvenement) => typeEvenement is TypeValide or TypeRejete;

    public Task<IReadOnlyList<Guid>> ResoudreAsync(string typeEvenement, string chargeUtile, CancellationToken ct = default)
    {
        if (!PeutResoudre(typeEvenement) || ResolveurChargeUtile.LireGuid(chargeUtile, "preparateurId") is not Guid preparateurId)
        {
            return Task.FromResult<IReadOnlyList<Guid>>([]);
        }

        return Task.FromResult<IReadOnlyList<Guid>>([preparateurId]);
    }

    public Task<Guid?> ResoudreTicketAsync(string chargeUtile, CancellationToken ct = default) => Task.FromResult<Guid?>(null);
}

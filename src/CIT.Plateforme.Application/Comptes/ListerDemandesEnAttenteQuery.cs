using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>
/// REMÉDIATION POST-AUDIT (`post-implementation-audit.md` §19, IDOR) — Contrat C2-02 (lecture). La
/// file des demandes en attente n'est jamais lue directement depuis l'UI (`ISocieteClienteRepository`
/// en accès direct, sans autorisation) : même permission que la validation elle-même
/// (<c>Permission.CompteValider</c>), pour qu'un visiteur non habilité ne puisse jamais découvrir
/// des demandes de compte en attente (raison sociale, profil déclaré).
/// </summary>
public sealed class ListerDemandesEnAttenteQuery(
    ISocieteClienteRepository societes,
    IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<SocieteCliente>> ExecuterAsync(Guid appelantId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.CompteValider, ct: ct).ConfigureAwait(false);

        return await societes.ListerParEtatAsync(EtatSociete.EnAttenteValidation, ct).ConfigureAwait(false);
    }
}

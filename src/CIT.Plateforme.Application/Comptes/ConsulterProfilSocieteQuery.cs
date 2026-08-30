using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>Contrat C2-10 — Consulter le profil d'une société et ses utilisateurs rattachés. CDC :
/// CPT-04, ENT-01, ENT-03 (cas d'usage #7 déduit de spec.md).</summary>
public sealed record ProfilSocieteResultat(SocieteCliente Societe, IReadOnlyList<UtilisateurSociete> UtilisateursRattaches);

public sealed class ConsulterProfilSocieteQuery(
    ISocieteClienteRepository societes,
    IUtilisateurSocieteRepository rattachements,
    IAutorisationService autorisation)
{
    public async Task<ProfilSocieteResultat> ExecuterAsync(Guid appelantId, Guid societeId, CancellationToken ct = default)
    {
        var societe = await societes.ObtenirParIdAsync(societeId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Société {societeId} introuvable.");

        var rattachementAppelant = await rattachements.ObtenirParUtilisateurAsync(appelantId, ct).ConfigureAwait(false);
        var membreDeLaSociete = rattachementAppelant is { EstActif: true } && rattachementAppelant.SocieteId == societeId;

        if (!membreDeLaSociete && !await autorisation.EstAutoriseAsync(appelantId, Permission.CompteValider, ct: ct).ConfigureAwait(false))
        {
            throw new AccesRefuseException(
                $"L'utilisateur {appelantId} n'est pas rattaché à la société {societeId} et n'est pas un administrateur fonctionnel habilité (CPT-04).");
        }

        var utilisateurs = await rattachements.ListerParSocieteAsync(societeId, ct).ConfigureAwait(false);
        return new ProfilSocieteResultat(societe, utilisateurs);
    }
}

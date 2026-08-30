using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Comptes;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>
/// Contrat C2-03 — Vérifier l'accès au compte après authentification Identity réussie. CDC :
/// CPT-03. Complète l'authentification technique (D1, inchangée) d'une règle métier propre au
/// compte société : société active + rattachement actif (research.md D10).
/// </summary>
public sealed class VerifierAccesCompteQuery(
    IUtilisateurSocieteRepository rattachements,
    ISocieteClienteRepository societes)
{
    public async Task<bool> AccesAutoriseAsync(Guid utilisateurId, CancellationToken ct = default)
    {
        var rattachement = await rattachements.ObtenirParUtilisateurAsync(utilisateurId, ct).ConfigureAwait(false);
        if (rattachement is null || !rattachement.EstActif)
        {
            return false;
        }

        var societe = await societes.ObtenirParIdAsync(rattachement.SocieteId, ct).ConfigureAwait(false);
        return societe is not null && societe.Etat == EtatSociete.Active;
    }
}

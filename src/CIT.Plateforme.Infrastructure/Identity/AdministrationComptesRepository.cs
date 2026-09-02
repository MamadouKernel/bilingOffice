using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Securite;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Infrastructure.Identity;

/// <summary>
/// CDC § 17.3 — administration du cycle de vie des comptes agents, adossée au fournisseur
/// d'identité.
///
/// La politique de mot de passe et l'unicité de l'adresse sont laissées à Identity plutôt que
/// réimplémentées : ce sont les mêmes règles que celles appliquées à l'auto-inscription cliente,
/// et les dupliquer ici les ferait diverger tôt ou tard. Les motifs de refus sont remontés tels
/// quels, sans reformulation qui les rendrait moins actionnables.
/// </summary>
public sealed class AdministrationComptesRepository(UserManager<ApplicationUser> userManager)
    : IAdministrationComptesRepository
{
    public async Task<IReadOnlyList<UtilisateurInfo>> ListerAgentsAsync(CancellationToken ct = default) =>
        await userManager.Users
            .Where(u => u.Nature == NatureUtilisateur.Agent)
            .OrderBy(u => u.NomAffiche)
            .Select(u => new UtilisateurInfo(
                u.Id,
                u.UserName ?? string.Empty,
                u.Email ?? string.Empty,
                u.NomAffiche,
                u.EstActif,
                u.Nature))
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<ResultatCreationCompte> CreerAgentAsync(
        string email, string nomAffiche, string motDePasseInitial, CancellationToken ct = default)
    {
        var compte = new ApplicationUser
        {
            UserName = email,
            Email = email,
            NomAffiche = nomAffiche,
            Nature = NatureUtilisateur.Agent,

            // Le compte est créé par un administrateur identifié, sur un poste interne : la
            // possession de l'adresse est établie par le processus d'entreprise, pas par un
            // courriel de confirmation — d'autant qu'aucun fournisseur d'envoi n'existe.
            EmailConfirmed = true,
            EstActif = true,
        };

        // Identity écrit immédiatement : la création n'est pas portée par l'unité de travail
        // applicative. La journalisation qui suit, elle, l'est.
        var resultat = await userManager.CreateAsync(compte, motDePasseInitial).ConfigureAwait(false);

        return resultat.Succeeded
            ? new ResultatCreationCompte(true, compte.Id, Array.Empty<string>())
            : new ResultatCreationCompte(false, Guid.Empty, resultat.Errors.Select(e => e.Description).ToList());
    }

    /// <summary>
    /// SEC-11 — bascule le seul indicateur d'activité. Aucune suppression, aucune anonymisation :
    /// l'historique produit par le compte doit rester nominativement attribuable.
    /// </summary>
    public async Task DefinirActivationAsync(Guid utilisateurId, bool actif, CancellationToken ct = default)
    {
        var compte = await userManager.FindByIdAsync(utilisateurId.ToString()).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Compte {utilisateurId} introuvable.");

        compte.EstActif = actif;
        await userManager.UpdateAsync(compte).ConfigureAwait(false);
    }
}

using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Infrastructure.Identity;

/// <summary>
/// Annuaire de tous les comptes. Quatre lectures non suivies (comptes, rattachements, sociétés,
/// droits d'enrôlement) assemblées en mémoire : pas de requête par compte (N+1).
/// </summary>
public sealed class AnnuaireComptes(PlateformeDbContext contexte) : IAnnuaireComptes
{
    public async Task<IReadOnlyList<CompteUtilisateurVue>> ListerAsync(CancellationToken ct = default)
    {
        var comptes = await contexte.Users.AsNoTracking()
            .OrderBy(u => u.NomAffiche)
            .Select(u => new { u.Id, u.Email, u.NomAffiche, u.Nature, u.EstActif, u.TwoFactorEnabled, SansMotDePasse = u.PasswordHash == null })
            .ToListAsync(ct).ConfigureAwait(false);

        var rattachements = await contexte.UtilisateursSociete.AsNoTracking()
            .Select(r => new { r.UtilisateurId, r.SocieteId })
            .ToListAsync(ct).ConfigureAwait(false);
        var societeParUtilisateur = rattachements
            .GroupBy(r => r.UtilisateurId)
            .ToDictionary(g => g.Key, g => g.First().SocieteId);

        var nomsSocietes = (await contexte.Societes.AsNoTracking().ToListAsync(ct).ConfigureAwait(false))
            .ToDictionary(s => s.Id, s => s.NomAffichable);

        var transporteurs = (await contexte.Habilitations.AsNoTracking()
                .Where(h => h.PermissionCode == Permission.EnrolementSoumettre)
                .Select(h => h.UtilisateurId)
                .ToListAsync(ct).ConfigureAwait(false))
            .ToHashSet();

        return comptes.Select(c =>
        {
            string? societe = societeParUtilisateur.TryGetValue(c.Id, out var sid) && nomsSocietes.TryGetValue(sid, out var nom)
                ? nom
                : null;

            var categorie = c.Nature == NatureUtilisateur.Agent ? CategorieCompte.AgentCit
                : societe is not null ? CategorieCompte.ClientSociete
                : transporteurs.Contains(c.Id) ? CategorieCompte.Transporteur
                : CategorieCompte.ClientSansSociete;

            return new CompteUtilisateurVue(
                c.Id, c.Email ?? string.Empty, c.NomAffiche, categorie, societe,
                c.EstActif, c.TwoFactorEnabled, c.SansMotDePasse);
        }).ToList();
    }
}

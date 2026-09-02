using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>Contrat C2-10 — Consulter le profil d'une société et ses utilisateurs rattachés. CDC :
/// CPT-04, ENT-01, ENT-03 (cas d'usage #7 déduit de spec.md).</summary>
/// <summary><paramref name="NomsUtilisateurs"/> permet d'afficher des personnes plutôt que des
/// identifiants techniques ; il reste facultatif pour ne pas altérer le contrat existant.</summary>
public sealed record ProfilSocieteResultat(
    SocieteCliente Societe,
    IReadOnlyList<UtilisateurSociete> UtilisateursRattaches,
    IReadOnlyDictionary<Guid, string>? NomsUtilisateurs = null);

public sealed class ConsulterProfilSocieteQuery(
    ISocieteClienteRepository societes,
    IUtilisateurSocieteRepository rattachements,
    IAutorisationService autorisation,
    IUtilisateurInfoRepository? utilisateurs = null)
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

        var membres = await rattachements.ListerParSocieteAsync(societeId, ct).ConfigureAwait(false);

        // Résolution des noms affichés : la liste des collaborateurs présente des personnes,
        // l'identifiant technique n'ayant aucun sens pour un utilisateur métier.
        IReadOnlyDictionary<Guid, string>? noms = null;
        if (utilisateurs is not null && membres.Count > 0)
        {
            var infos = await utilisateurs
                .ObtenirInfosAsync(membres.Select(m => m.UtilisateurId), ct)
                .ConfigureAwait(false);

            noms = infos.ToDictionary(
                paire => paire.Key,
                paire => string.IsNullOrWhiteSpace(paire.Value.NomAffiche) ? paire.Value.Email : paire.Value.NomAffiche!);
        }

        return new ProfilSocieteResultat(societe, membres, noms);
    }
}

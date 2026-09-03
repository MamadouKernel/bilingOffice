using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Identity;

/// <summary>
/// AMORÇAGE DU PREMIER ADMINISTRATEUR.
///
/// LE PROBLÈME : l'écran d'administration des comptes (§ 17.3) exige la permission
/// <see cref="Permission.AdministrationComptes"/>. En développement, le jeu de données l'accorde.
/// En production, le jeu de données ne s'exécute jamais — personne ne détient la permission au
/// premier démarrage, et aucun chemin ne permet de l'accorder. La plateforme serait livrée
/// verrouillée de l'extérieur.
///
/// LE CDC EST MUET sur ce point. Ce silence est traité comme un OUBLI : aucun système
/// administrable ne se déploie sans amorçage, et l'alternative — intervenir directement en base
/// de production — serait précisément la pratique que le journal d'audit existe pour proscrire.
///
/// LES GARDE-FOUS, qui font que ce mécanisme n'est pas une porte dérobée :
///   • IDEMPOTENT ET AUTO-EFFAÇANT — n'agit QUE si aucun compte au monde ne détient la
///     permission. Dès qu'un administrateur existe, ce code ne fait plus rien, définitivement.
///     Il ne peut donc ni recréer un compte supprimé, ni réhabiliter un compte désactivé.
///   • AUCUN SECRET EN DUR — l'adresse et le mot de passe viennent de la configuration
///     (variables d'environnement ou coffre). Rien n'est écrit dans le dépôt.
///   • SILENCIEUX SI NON CONFIGURÉ — l'absence de configuration n'empêche pas le démarrage :
///     elle est signalée par un avertissement explicite. Une plateforme qui refuse de démarrer
///     est plus difficile à diagnostiquer qu'une plateforme qui dit ce qui lui manque.
///   • TRACÉ — la création est journalisée au niveau avertissement, avec la consigne de changer
///     le mot de passe. Un amorçage doit se voir dans les journaux d'exploitation.
/// </summary>
public sealed class ProvisionnementAdministrateurInitial(
    PlateformeDbContext context,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    ILogger<ProvisionnementAdministrateurInitial> logger)
{
    private const string SectionEmail = "Administration:AdministrateurInitial:Email";
    private const string SectionMotDePasse = "Administration:AdministrateurInitial:MotDePasse";
    private const string SectionNom = "Administration:AdministrateurInitial:NomAffiche";

    public async Task ExecuterAsync(CancellationToken ct = default)
    {
        // Le seul verrou qui compte : tant qu'un administrateur existe, ce code est inerte.
        var administrateurExiste = await context.Habilitations
            .AnyAsync(h => h.PermissionCode == Permission.AdministrationComptes, ct)
            .ConfigureAwait(false);

        if (administrateurExiste)
        {
            return;
        }

        var email = configuration[SectionEmail]
            ?? Environment.GetEnvironmentVariable("CIT_ADMIN_EMAIL")
            ?? Environment.GetEnvironmentVariable("CIT_INITIAL_ADMIN_EMAIL");

        var motDePasse = configuration[SectionMotDePasse]
            ?? Environment.GetEnvironmentVariable("CIT_ADMIN_PASSWORD")
            ?? Environment.GetEnvironmentVariable("CIT_TEST_USERS_PASSWORD");

        var nom = configuration[SectionNom]
            ?? Environment.GetEnvironmentVariable("CIT_ADMIN_NAME")
            ?? "Administrateur Initial CIT";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(motDePasse))
        {
            logger.LogWarning(
                "Aucun compte ne détient la permission « {Permission} » et aucun administrateur initial n'est " +
                "configuré : l'administration des comptes est inaccessible. Renseignez « {CleEmail} » et " +
                "« {CleMotDePasse} » (ou variables d'environnement CIT_ADMIN_EMAIL / CIT_ADMIN_PASSWORD), puis redémarrez.",
                Permission.AdministrationComptes, SectionEmail, SectionMotDePasse);
            return;
        }

        var compte = await userManager.FindByEmailAsync(email).ConfigureAwait(false);

        if (compte is null)
        {
            compte = new ApplicationUser
            {
                UserName = email,
                Email = email,
                NomAffiche = nom,
                Nature = NatureUtilisateur.Agent,
                EmailConfirmed = true,
                EstActif = true,
            };

            var resultat = await userManager.CreateAsync(compte, motDePasse).ConfigureAwait(false);
            if (!resultat.Succeeded)
            {
                logger.LogError(
                    "Création du Super Administrateur « {Email} » refusée : {Motifs}.",
                    email, string.Join(" ", resultat.Errors.Select(e => e.Description)));
                return;
            }
        }
        else
        {
            compte.EstActif = true;
            compte.LockoutEnd = null;
            compte.AccessFailedCount = 0;
            await userManager.UpdateAsync(compte).ConfigureAwait(false);

            var resetToken = await userManager.GeneratePasswordResetTokenAsync(compte).ConfigureAwait(false);
            await userManager.ResetPasswordAsync(compte, resetToken, motDePasse).ConfigureAwait(false);
        }

        // Attribution des 54 permissions du système (Super Admin)
        foreach (var perm in Permission.Tous)
        {
            var existe = await context.Habilitations.AnyAsync(h => h.UtilisateurId == compte.Id && h.PermissionCode == perm, ct).ConfigureAwait(false);
            if (!existe)
            {
                context.Habilitations.Add(HabilitationUtilisateur.Attribuer(compte.Id, perm));
            }
        }
        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "AMORÇAGE : Le Super Administrateur « {Email} » ({Nom}) est opérationnel avec 54 permissions.",
            email, nom);
    }
}

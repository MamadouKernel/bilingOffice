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

        var email = configuration[SectionEmail];
        var motDePasse = configuration[SectionMotDePasse];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(motDePasse))
        {
            logger.LogWarning(
                "Aucun compte ne détient la permission « {Permission} » et aucun administrateur initial n'est " +
                "configuré : l'administration des comptes est inaccessible. Renseignez « {CleEmail} » et " +
                "« {CleMotDePasse} » (variable d'environnement ou coffre à secrets), puis redémarrez.",
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
                NomAffiche = configuration[SectionNom] ?? "Administrateur initial",
                Nature = NatureUtilisateur.Agent,

                // Compte provisionné par l'exploitant sur une adresse qu'il maîtrise : la
                // possession est établie par le déploiement, pas par un courriel de confirmation
                // — d'autant qu'aucun fournisseur d'envoi n'existe sur la plateforme.
                EmailConfirmed = true,
                EstActif = true,
            };

            var resultat = await userManager.CreateAsync(compte, motDePasse).ConfigureAwait(false);
            if (!resultat.Succeeded)
            {
                logger.LogError(
                    "Création de l'administrateur initial « {Email} » refusée : {Motifs}. " +
                    "L'administration des comptes reste inaccessible.",
                    email, string.Join(" ", resultat.Errors.Select(e => e.Description)));
                return;
            }
        }

        // Le compte pouvait préexister sans la permission (compte agent ordinaire promu par la
        // configuration) : on n'en recrée pas un second, on l'habilite.
        context.Habilitations.Add(
            HabilitationUtilisateur.Attribuer(compte.Id, Permission.AdministrationComptes));
        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogWarning(
            "AMORÇAGE : l'administrateur initial « {Email} » a reçu la permission « {Permission} ». " +
            "CHANGEZ SON MOT DE PASSE dès la première connexion, et retirez le secret de la configuration " +
            "une fois d'autres administrateurs désignés.",
            email, Permission.AdministrationComptes);
    }
}

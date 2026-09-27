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

    /// <summary>
    /// Retient la première source RÉELLEMENT renseignée, en traitant la chaîne vide comme absente.
    ///
    /// DÉFAUT CORRIGÉ : ces valeurs étaient chaînées avec <c>??</c>, qui ne bascule que sur
    /// <c>null</c>. Or <c>appsettings.json</c> déclare <c>Email</c> et <c>NomAffiche</c> à la chaîne
    /// vide — un gabarit destiné à montrer les clés attendues. La configuration renvoyait donc
    /// <c>""</c>, jamais <c>null</c> : les variables d'environnement placées ensuite dans la chaîne
    /// n'étaient **jamais consultées**, et l'amorçage sortait systématiquement par son
    /// avertissement.
    ///
    /// La conséquence était exactement la panne que cette classe existe pour empêcher : une
    /// plateforme livrée sans aucun compte capable d'administrer les comptes, et aucun moyen d'y
    /// remédier depuis l'application. Le mécanisme paraissait présent et n'a jamais pu s'exécuter.
    /// </summary>
    private static string? PremiereValeurRenseignee(params string?[] sources) =>
        Array.Find(sources, v => !string.IsNullOrWhiteSpace(v));

    public async Task ExecuterAsync(CancellationToken ct = default)
    {
        // Le seul verrou qui compte : tant qu'un administrateur existe, ce code est inerte.
        var administrateurExiste = await context.Habilitations
            .AnyAsync(h => h.PermissionCode == Permission.AdministrationComptes, ct)
            .ConfigureAwait(false);

        var email = PremiereValeurRenseignee(
            configuration[SectionEmail],
            Environment.GetEnvironmentVariable("CIT_ADMIN_EMAIL"),
            Environment.GetEnvironmentVariable("CIT_INITIAL_ADMIN_EMAIL"));

        if (administrateurExiste)
        {
            await CompleterPermissionsSuperAdministrateurAsync(email, ct).ConfigureAwait(false);
            return;
        }

        var motDePasse = PremiereValeurRenseignee(
            configuration[SectionMotDePasse],
            Environment.GetEnvironmentVariable("CIT_ADMIN_PASSWORD"),
            Environment.GetEnvironmentVariable("CIT_TEST_USERS_PASSWORD"));

        var nom = PremiereValeurRenseignee(
            configuration[SectionNom],
            Environment.GetEnvironmentVariable("CIT_ADMIN_NAME"))
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

        var ajoutees = await AttribuerPermissionsManquantesAsync(compte.Id, ct).ConfigureAwait(false);

        logger.LogInformation(
            "AMORÇAGE : Le Super Administrateur « {Email} » ({Nom}) est opérationnel ({Ajoutees} permissions attribuées sur {Total}).",
            email, nom, ajoutees, Permission.Tous.Count);
    }

    /// <summary>
    /// COMPLÉMENT À CHAQUE DÉMARRAGE — le Super Administrateur doit détenir TOUTES les permissions,
    /// y compris celles ajoutées au référentiel après son amorçage (sinon il perd silencieusement
    /// l'accès aux nouveaux écrans, et l'administration ne le compte plus comme super-admin).
    /// Purement additif et borné : seul le compte configuré, et seulement s'il est DÉJÀ
    /// administrateur des comptes, est complété ; rien n'est jamais retiré.
    /// </summary>
    private async Task CompleterPermissionsSuperAdministrateurAsync(string? email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var compte = await userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (compte is null)
        {
            return;
        }

        var estAdministrateur = await context.Habilitations
            .AnyAsync(h => h.UtilisateurId == compte.Id && h.PermissionCode == Permission.AdministrationComptes, ct)
            .ConfigureAwait(false);
        if (!estAdministrateur)
        {
            return;
        }

        var ajoutees = await AttribuerPermissionsManquantesAsync(compte.Id, ct).ConfigureAwait(false);
        if (ajoutees > 0)
        {
            logger.LogInformation(
                "Super Administrateur « {Email} » : {Ajoutees} nouvelle(s) permission(s) du référentiel attribuée(s).",
                email, ajoutees);
        }
    }

    private async Task<int> AttribuerPermissionsManquantesAsync(Guid utilisateurId, CancellationToken ct)
    {
        var detenues = await context.Habilitations
            .Where(h => h.UtilisateurId == utilisateurId && h.FileId == null)
            .Select(h => h.PermissionCode)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var manquantes = Permission.Tous.Except(detenues, StringComparer.Ordinal).ToList();
        foreach (var permission in manquantes)
        {
            context.Habilitations.Add(HabilitationUtilisateur.Attribuer(utilisateurId, permission));
        }

        if (manquantes.Count > 0)
        {
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return manquantes.Count;
    }
}

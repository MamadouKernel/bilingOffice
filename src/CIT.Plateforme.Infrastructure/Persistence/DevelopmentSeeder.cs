using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Persistence;

public sealed partial class DevelopmentSeeder(
    PlateformeDbContext context,
    UserManager<ApplicationUser> userManager,
    IHostEnvironment environnement,
    ILogger<DevelopmentSeeder> logger)
{
    /// <summary>
    /// Amorçage du jeu de démonstration, sous quatre barrières cumulatives évaluées par
    /// <see cref="PolitiqueAmorcageDemo"/> : environnement de développement, secret fourni,
    /// consentement explicite aux opérations destructives, et identité de la base confirmée.
    ///
    /// INCIDENT À L'ORIGINE DE CES BARRIÈRES : cette méthode repliait sur un mot de passe écrit en
    /// dur lorsque la variable attendue était absente. La purge devenait donc inconditionnelle, et
    /// s'est appliquée à la base de développement réelle. Une absence de configuration autorisait
    /// la destruction ; elle l'empêche désormais.
    /// </summary>
    public async Task SeedAsync()
    {
        var motDePasseComptesDemo = Environment.GetEnvironmentVariable(PolitiqueAmorcageDemo.CleMotDePasse);

        var decision = PolitiqueAmorcageDemo.Decider(
            environnement.EnvironmentName,
            motDePasseComptesDemo,
            Environment.GetEnvironmentVariable(PolitiqueAmorcageDemo.CleOptInPurge),
            Environment.GetEnvironmentVariable(PolitiqueAmorcageDemo.CleBaseDemo),
            context.Database.GetDbConnection().Database);

        // Le motif décrit la décision, jamais la valeur d'un secret.
        logger.LogInformation("Amorçage de démonstration : {Motif}", decision.Motif);

        if (!decision.AmorcageAutorise)
        {
            return;
        }

        if (decision.PurgeAutorisee)
        {
            await PurgerDonneesTestAsync();
        }

        // Données de référence : additives et idempotentes, sans effet destructif.
        // Paramétrage métier commun à tous les environnements (files, catégories, pièces).
        await new ProvisionnementParametrageMetier(
            context, Microsoft.Extensions.Logging.Abstractions.NullLogger<ProvisionnementParametrageMetier>.Instance).ExecuterAsync();
        // Les sujets de l'assistant sont amorcés (FR+EN) par ProvisionnementSujetsAssistant, seule
        // source pour tous les environnements — plus de doublon FR-only ici.

        await SeedAdminCitAsync(motDePasseComptesDemo!);
        await SeedClientDemoAsync(motDePasseComptesDemo!);

        await context.SaveChangesAsync();

        logger.LogInformation(
            "Amorçage de démonstration terminé (purge {EtatPurge}).",
            decision.PurgeAutorisee ? "effectuée" : "non effectuée");
    }

    /// <summary>
    /// Purge tous les comptes utilisateurs, sociétés de test, tickets et historiques
    /// en conservant STRICTEMENT ET UNIQUEMENT le compte Super Administrateur M. KONATE.
    /// </summary>
    private async Task PurgerDonneesTestAsync()
    {
        const string konateEmail = "m.konate@cotedivoireterminal.com";

        try
        {
            // A. Identifier les utilisateurs autres que M. KONATE
            var usersToPurge = await userManager.Users
                .Where(u => u.Email == null || u.Email != konateEmail)
                .ToListAsync();

            var userIdsToPurge = usersToPurge.Select(u => u.Id).ToHashSet();

            if (userIdsToPurge.Count > 0)
            {
                // Supprimer les habilitations des utilisateurs purgés
                var habs = await context.Habilitations
                    .Where(h => userIdsToPurge.Contains(h.UtilisateurId))
                    .ToListAsync();
                context.Habilitations.RemoveRange(habs);

                // Supprimer les rattachements aux sociétés
                var rattachs = await context.UtilisateursSociete
                    .Where(u => userIdsToPurge.Contains(u.UtilisateurId))
                    .ToListAsync();
                context.UtilisateursSociete.RemoveRange(rattachs);

                // Supprimer les notifications des utilisateurs purgés
                var notifs = await context.Notifications
                    .Where(n => userIdsToPurge.Contains(n.DestinataireId))
                    .ToListAsync();
                context.Notifications.RemoveRange(notifs);

                // Supprimer les invitations
                var invites = await context.InvitationsCollaborateur.ToListAsync();
                context.InvitationsCollaborateur.RemoveRange(invites);

                // Supprimer les utilisateurs Identity
                foreach (var user in usersToPurge)
                {
                    await userManager.DeleteAsync(user);
                }
            }

            // B. Supprimer toutes les sociétés de test existantes
            var societes = await context.Societes.ToListAsync();
            context.Societes.RemoveRange(societes);

            // C. Supprimer les tickets, dossiers et historiques de test
            var tickets = await context.Tickets.ToListAsync();
            context.Tickets.RemoveRange(tickets);

            var dossiers = await context.Dossiers.ToListAsync();
            context.Dossiers.RemoveRange(dossiers);

            var historiques = await context.HistoriqueTickets.ToListAsync();
            context.HistoriqueTickets.RemoveRange(historiques);

            var periodes = await context.PeriodesAttenteClient.ToListAsync();
            context.PeriodesAttenteClient.RemoveRange(periodes);

            var rdv = await context.RendezVous.ToListAsync();
            context.RendezVous.RemoveRange(rdv);

            var brouillons = await context.BrouillonsSaisie.ToListAsync();
            context.BrouillonsSaisie.RemoveRange(brouillons);

            var suivisIes = await context.SuivisBranchesIes.ToListAsync();
            context.SuivisBranchesIes.RemoveRange(suivisIes);

            await context.SaveChangesAsync();
            logger.LogInformation("Données de test purgées avec succès.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Erreur lors de la purge des données de test (non bloquante) : {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Compte Super Administrateur CIT (M. KONATE) :
    /// - Email : m.konate@cotedivoireterminal.com
    /// - Nom : Mamadou KONATE
    /// - Mot de passe : fourni par <c>CIT_TEST_USERS_PASSWORD</c>, **jamais écrit dans le code**
    /// - Habilitations : intégralité des permissions du système (<c>Permission.Tous</c>)
    /// - Idempotence : ne recrée pas le compte s'il existe déjà, et ne duplique pas ses habilitations.
    /// </summary>
    private async Task SeedAdminCitAsync(string motDePasseComptesDemo)
    {
        const string konateEmail = "m.konate@cotedivoireterminal.com";
        const string konateNom = "Mamadou KONATE";

        var permissionsSuperAdmin = Permission.Tous
            .Select(p => (Permission: p, NomFile: (string?)null))
            .ToArray();

        await CreerAgentAsync(konateEmail, konateNom, motDePasseComptesDemo, permissionsSuperAdmin);
    }

    private async Task CreerAgentAsync(string email, string nomAffiche, string password, (string Permission, string? NomFile)[] permissions)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                NomAffiche = nomAffiche,
                Nature = NatureUtilisateur.Agent,
                EmailConfirmed = true,
                EstActif = true
            };

            await userManager.CreateAsync(user, password);
        }
        else
        {
            // Le mot de passe d'un compte existant n'est JAMAIS réécrit ici.
            //
            // Auparavant, l'amorçage réinitialisait le secret à chaque démarrage : toute rotation
            // effectuée par un administrateur était défaite au lancement suivant, sans que rien ne
            // le signale. Un compte dont le secret a été compromis serait ainsi revenu à sa valeur
            // compromise. L'amorçage crée un compte absent ; il ne reprend pas la main sur un
            // compte existant.
            logger.LogInformation(
                "Compte de démonstration déjà présent : identifiants laissés intacts, seules les habilitations sont complétées.");
        }

        foreach (var (perm, nomFile) in permissions)
        {
            Guid? fileId = null;
            if (nomFile != null)
            {
                fileId = await context.Files.Where(f => f.Nom == nomFile).Select(f => f.Id).FirstOrDefaultAsync();
            }

            var habExiste = await context.Habilitations.AnyAsync(h => h.UtilisateurId == user.Id && h.PermissionCode == perm && h.FileId == fileId);
            if (!habExiste)
            {
                context.Habilitations.Add(HabilitationUtilisateur.Attribuer(user.Id, perm, fileId));
            }
        }
        await context.SaveChangesAsync();
    }
}

using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CIT.Plateforme.Web.Tests.Support;

/// <summary>
/// Fixtures de comptes pour les scénarios de recette, créées explicitement par les tests.
///
/// POURQUOI CETTE CLASSE EXISTE : ces comptes étaient auparavant produits par
/// <c>DevelopmentSeeder</c>, que les tests appelaient pour s'en servir de jeu de données. Le
/// seeder a ensuite été réécrit pour ne plus créer que le compte d'administration — et cinq
/// scénarios se sont mis à échouer, sans que le défaut soit visible dans leur code.
///
/// Un amorçage applicatif n'est pas une fixture de test : il répond à un besoin de démonstration,
/// il change quand ce besoin change, et il emporte les tests avec lui. Chaque scénario crée
/// désormais ce dont il a besoin, et ne dépend plus de ce que l'application veut bien semer.
///
/// IDENTITÉS : uniquement <c>@test.local</c>, jamais une identité réelle. La création est
/// idempotente : la base de test persiste d'une exécution à l'autre.
/// </summary>
public static class FixtureComptesDeTest
{
    public const string EmailClientNormal = "client.normal.user@test.local";
    public const string EmailAgentSupport = "agent.support@test.local";
    public const string EmailAgentGate = "agent.gate@test.local";
    public const string EmailAgentFacturation = "agent.facturation@test.local";
    public const string EmailTransporteur = "transporteur.chauffeur@test.local";
    public const string EmailTarification = "tarification@test.local";

    /// <summary>Secret synthétique, réservé aux comptes de test. Aucun lien avec un compte réel.</summary>
    private const string MotDePasseSynthetique = "MotDePasseDeTest123!";

    private const string NomSocieteNormale = "NORMAL TEST SA";

    /// <summary>
    /// Client rattaché à une société validée de profil <see cref="TypeClient.Normal"/>, habilité à
    /// soumettre et répondre à une réclamation. Crée la société si elle n'existe pas.
    /// </summary>
    public static async Task<ApplicationUser> CreerClientNormalRattacheAsync(IServiceProvider sp)
    {
        var context = sp.GetRequiredService<PlateformeDbContext>();
        var utilisateur = await CreerUtilisateurAsync(sp, EmailClientNormal, "Client Normal (test)", NatureUtilisateur.Client);

        var societe = await context.Societes.FirstOrDefaultAsync(s => s.Profil == TypeClient.Normal);
        if (societe is null)
        {
            societe = SocieteCliente.Soumettre(
                utilisateur.Id,
                $"{{\"RaisonSociale\": \"{NomSocieteNormale}\", \"RCCM\": \"CI-ABJ-2026-B-0000\"}}",
                TypeClient.Normal,
                DateTime.UtcNow);
            societe.Valider();
            context.Societes.Add(societe);
            await context.SaveChangesAsync();
        }

        // Les scénarios exigent EXACTEMENT un rattachement pour ce client : ils l'obtiennent par
        // SingleAsync. En créer un second au deuxième passage casserait le test sans rapport avec
        // ce qu'il vérifie.
        if (!await context.UtilisateursSociete.AnyAsync(u => u.UtilisateurId == utilisateur.Id))
        {
            var rattachement = UtilisateurSociete.Rattacher(
                utilisateur.Id, societe.Id, DateTime.UtcNow, estAdministrateur: false, droitSoumission: true);
            rattachement.ModifierDroits(DroitConsultation.PropresDemandesUniquement, droitSoumission: true);
            context.UtilisateursSociete.Add(rattachement);
            await context.SaveChangesAsync();
        }

        if (societe.NombreAdministrateursActifs == 0)
        {
            societe.EnregistrerAdministrateurActif();
            await context.SaveChangesAsync();
        }

        await AttribuerAsync(context, utilisateur.Id, Permission.ComplaintSoumettre, null);
        await AttribuerAsync(context, utilisateur.Id, Permission.ComplaintRepondre, null);
        await context.SaveChangesAsync();

        return utilisateur;
    }

    /// <summary>
    /// Transporteur : personne physique, <see cref="NatureUtilisateur.Client"/>, **sans société
    /// rattachée** — c'est la caractéristique que le parcours d'enrôlement ENR-01 exerce.
    /// </summary>
    public static async Task<ApplicationUser> CreerTransporteurAsync(IServiceProvider sp)
    {
        var context = sp.GetRequiredService<PlateformeDbContext>();
        var utilisateur = await CreerUtilisateurAsync(sp, EmailTransporteur, "Transporteur Chauffeur (test)", NatureUtilisateur.Client);

        await AttribuerAsync(context, utilisateur.Id, Permission.EnrolementSoumettre, null);
        await context.SaveChangesAsync();

        return utilisateur;
    }

    public static Task<ApplicationUser> CreerAgentSupportAsync(IServiceProvider sp) =>
        CreerAgentAsync(sp, EmailAgentSupport, "Agent Support (test)",
        [
            (Permission.ComplaintInstruire, "Support Opérationnel"),
            (Permission.AdministrationCategoriesReclamation, null),
        ]);

    public static Task<ApplicationUser> CreerAgentGateAsync(IServiceProvider sp) =>
        CreerAgentAsync(sp, EmailAgentGate, "Agent Gate (test)",
        [
            (Permission.EnrolementInstruire, "Agent Gate"),
            (Permission.GuichetRecherche, null),
            (Permission.RendezVousGerer, null),
        ]);

    public static Task<ApplicationUser> CreerAgentFacturationAsync(IServiceProvider sp) =>
        CreerAgentAsync(sp, EmailAgentFacturation, "Agent Facturation (test)",
        [
            (Permission.BillingImportFacturationEditer, "Facturation"),
            (Permission.BillingExportFacturationEditer, "Facturation"),
            (Permission.GuichetRecherche, null),
            (Permission.RendezVousGerer, null),
            (Permission.AvoirInstruire, "Avoir & Exonération"),
            (Permission.AvoirDecider, null),
            (Permission.ExonerationInstruire, "Avoir & Exonération"),
            (Permission.ExonerationDecider, null),
        ]);

    public static Task<ApplicationUser> CreerAdministrateurTarificationAsync(IServiceProvider sp) =>
        CreerAgentAsync(sp, EmailTarification, "Admin Tarification (test)",
        [
            (Permission.TarificationAdministrer, null),
        ]);

    private static async Task<ApplicationUser> CreerAgentAsync(
        IServiceProvider sp, string email, string nomAffiche, (string Permission, string? NomFile)[] permissions)
    {
        var context = sp.GetRequiredService<PlateformeDbContext>();
        var utilisateur = await CreerUtilisateurAsync(sp, email, nomAffiche, NatureUtilisateur.Agent);

        foreach (var (permission, nomFile) in permissions)
        {
            Guid? fileId = null;
            if (nomFile is not null)
            {
                fileId = await context.Files
                    .Where(f => f.Nom == nomFile)
                    .Select(f => (Guid?)f.Id)
                    .FirstOrDefaultAsync();

                if (fileId is null)
                {
                    var file = FileDeTraitement.Creer("Billing Office", nomFile);
                    context.Files.Add(file);
                    await context.SaveChangesAsync();
                    fileId = file.Id;
                }
            }

            await AttribuerAsync(context, utilisateur.Id, permission, fileId);
        }

        await context.SaveChangesAsync();
        return utilisateur;
    }

    private static async Task<ApplicationUser> CreerUtilisateurAsync(
        IServiceProvider sp, string email, string nomAffiche, NatureUtilisateur nature)
    {
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        var existant = await userManager.FindByEmailAsync(email);
        if (existant is not null)
        {
            // Le compte est réutilisé tel quel : son secret n'est jamais réécrit. Un amorçage qui
            // reprend la main sur un compte existant est précisément ce qui a défait des rotations
            // de mot de passe en silence.
            return existant;
        }

        var utilisateur = new ApplicationUser
        {
            UserName = email,
            Email = email,
            NomAffiche = nomAffiche,
            Nature = nature,
            EmailConfirmed = true,
            EstActif = true,
        };

        var resultat = await userManager.CreateAsync(utilisateur, MotDePasseSynthetique);
        if (!resultat.Succeeded)
        {
            throw new InvalidOperationException(
                $"Création du compte de test « {email} » impossible : "
                + string.Join(" | ", resultat.Errors.Select(e => $"{e.Code}: {e.Description}")));
        }

        return utilisateur;
    }

    private static async Task AttribuerAsync(PlateformeDbContext context, Guid utilisateurId, string permission, Guid? fileId)
    {
        var deja = await context.Habilitations
            .AnyAsync(h => h.UtilisateurId == utilisateurId && h.PermissionCode == permission && h.FileId == fileId);

        if (!deja)
        {
            context.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(context, utilisateurId), permission, fileId));
        }
    }
}

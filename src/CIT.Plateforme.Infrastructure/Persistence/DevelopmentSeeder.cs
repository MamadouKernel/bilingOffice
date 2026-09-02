using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Persistence;

public sealed partial class DevelopmentSeeder(
    PlateformeDbContext context,
    UserManager<ApplicationUser> userManager,
    ILogger<DevelopmentSeeder> logger)
{
    public async Task SeedAsync()
    {
        var password = Environment.GetEnvironmentVariable("CIT_TEST_USERS_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
        {
            password = "Bonjour@2026#";
        }

        logger.LogInformation("Nettoyage et initialisation épurée : Conservation exclusive de M. KONATE et du socle métier indispensable.");

        // 1. Purge intégrale de toutes les données de test antérieures
        await PurgerDonneesTestAsync();

        // 2. Initialisation du socle de base (Files, Catégories, Pièces réglementaires)
        await SeedFilesAsync();
        await SeedCategoriesReclamationAsync();
        await SeedPiecesExigeesAsync();

        // 3. Initialisation exclusive du Super Administrateur M. KONATE (54 permissions)
        await SeedAdminCitAsync(password);

        await context.SaveChangesAsync();

        logger.LogInformation("Initialisation épurée terminée avec succès. Seul le compte M. KONATE est présent.");
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
    /// Initialise les 10 files de traitement opérationnelles standard.
    /// </summary>
    private async Task SeedFilesAsync()
    {
        var files = new[]
        {
            (Nom: "Pilote Manifeste", Module: "Billing Office"),
            (Nom: "Facturation", Module: "Billing Office"),
            (Nom: "Validation Paiement", Module: "Billing Office"),
            (Nom: "Contrôle Documentaire", Module: "Billing Office"),
            (Nom: "Comptabilité Client", Module: "Billing Office"),
            (Nom: "Back Office Export", Module: "Billing Office"),
            (Nom: "IES", Module: "Billing Office"),
            (Nom: "Support Opérationnel", Module: "Billing Office"),
            (Nom: "Avoir & Exonération", Module: "AvoirExoneration"),
            (Nom: "Agent Gate", Module: "Enrolement")
        };

        foreach (var f in files)
        {
            if (!await context.Files.AnyAsync(x => x.Nom == f.Nom))
            {
                context.Files.Add(FileDeTraitement.Creer(f.Module, f.Nom));
            }
        }
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Initialise les catégories de réclamations standard reliées à la file Support Opérationnel.
    /// </summary>
    private async Task SeedCategoriesReclamationAsync()
    {
        if (!await context.CategoriesReclamation.AnyAsync())
        {
            var fileSupportId = await context.Files
                .Where(f => f.Nom == "Support Opérationnel")
                .Select(f => f.Id)
                .FirstOrDefaultAsync();

            context.CategoriesReclamation.Add(Domain.Reclamations.CategorieReclamation.Creer("FACT", "Problème de facturation", fileSupportId));
            context.CategoriesReclamation.Add(Domain.Reclamations.CategorieReclamation.Creer("TECH", "Problème technique", fileSupportId));
            context.CategoriesReclamation.Add(Domain.Reclamations.CategorieReclamation.Creer("INFO", "Demande d'information", fileSupportId));
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Initialise le référentiel des 4 pièces justificatives officielles pour les créations de compte.
    /// </summary>
    private async Task SeedPiecesExigeesAsync()
    {
        var piecesInitiales = new[]
        {
            (Code: "RCCM", Libelle: "Registre du Commerce et du Crédit Mobilier (RCCM)", Obligatoire: false, Ordre: 1),
            (Code: "DFE", Libelle: "Déclaration Fiscale d'Existence (DFE / Fiche Contribuable)", Obligatoire: false, Ordre: 2),
            (Code: "CNI_REP", Libelle: "Pièce d'Identité du Représentant Légal (CNI / Passeport)", Obligatoire: false, Ordre: 3),
            (Code: "RIB", Libelle: "Relevé d'Identité Bancaire (RIB) Entreprise", Obligatoire: false, Ordre: 4)
        };

        foreach (var (code, libelle, obligatoire, ordre) in piecesInitiales)
        {
            if (!await context.PiecesExigeesCompte.AnyAsync(p => p.Code == code))
            {
                context.PiecesExigeesCompte.Add(PieceExigeeCompte.Definir(code, libelle, obligatoire, ordre));
            }
        }
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Compte Super Administrateur CIT (M. KONATE) :
    /// - Email : m.konate@cotedivoireterminal.com
    /// - Nom : Mamadou KONATE
    /// - Mot de passe : Bonjour@2026#
    /// - Habilitations : Intégralité des 54 permissions du système (Permission.Tous)
    /// - Idempotence : Ne recrée pas le compte s'il existe déjà et ne duplique pas ses habilitations.
    /// </summary>
    private async Task SeedAdminCitAsync(string password)
    {
        const string konateEmail = "m.konate@cotedivoireterminal.com";
        const string konateNom = "Mamadou KONATE";
        const string konatePassword = "Bonjour@2026#";

        var permissionsSuperAdmin = Permission.Tous
            .Select(p => (Permission: p, NomFile: (string?)null))
            .ToArray();

        await CreerAgentAsync(konateEmail, konateNom, konatePassword, permissionsSuperAdmin);
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
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            user.EstActif = true;
            await userManager.UpdateAsync(user);

            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            await userManager.ResetPasswordAsync(user, resetToken, password);
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

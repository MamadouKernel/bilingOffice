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
            logger.LogWarning("La variable d'environnement CIT_TEST_USERS_PASSWORD n'est pas définie. Le jeu de données de test ne sera pas créé.");
            return;
        }

        logger.LogInformation("Début du seeding du jeu de données de test Development.");

        await SeedSocietesEtComptesAsync(password);
        await SeedFilesEtAgentsAsync(password);
        await SeedTicketsDemoAsync();
        await SeedEnrolementAsync(password);
        await SeedAdminCitAsync(password);
        await SeedNotificationsAsync();

        await context.SaveChangesAsync();

        logger.LogInformation("Fin du seeding du jeu de données de test Development.");
    }

    private async Task SeedSocietesEtComptesAsync(string password)
    {
        var societesConf = new[]
        {
            new { Nom = "NORMAL TEST SA", Profil = TypeClient.Normal },
            new { Nom = "EN COMPTE TEST SA", Profil = TypeClient.EnCompte },
            new { Nom = "DEPOSIT TEST SA", Profil = TypeClient.Deposit },
            new { Nom = "IES TEST SA", Profil = TypeClient.IES }
        };

        foreach (var s in societesConf)
        {
            var nomProfil = s.Profil.ToString().ToLowerInvariant();
            var adminEmail = $"client.{nomProfil}.admin@test.local";
            
            // 1. Créer l'utilisateur Admin
            var adminUser = await CreerUtilisateurDeBaseAsync(adminEmail, $"Admin {s.Profil}", password);

            // 2. Créer la société si elle n'existe pas
            var societe = await context.Societes.FirstOrDefaultAsync(x => x.InformationsDeclarees.Contains(s.Nom));
            if (societe == null)
            {
                societe = SocieteCliente.Soumettre(
                    adminUser.Id, 
                    $"{{\"RaisonSociale\": \"{s.Nom}\", \"RCCM\": \"CI-ABJ-2026-B-0000\"}}", 
                    s.Profil, 
                    DateTime.UtcNow);
                societe.Valider();
                context.Societes.Add(societe);
                await context.SaveChangesAsync();
            }

            // 3. Rattacher l'Admin
            // REMÉDIATION POST-AUDIT (post-implementation-audit.md §2.4/§2.5) : Permission.ComplaintSoumettre
            // n'était accordée à aucun compte client seedé — REC-01/use-cases.md C7-01 exigent
            // pourtant cette habilitation pour soumettre une réclamation (spec.md §6, CLIENT).
            await RattacherClientASocieteAsync(adminUser, societe, DroitConsultation.ToutesDemandesSociete, true, new[] { Permission.CompteGererCollaborateurs, Permission.ComplaintSoumettre, Permission.ComplaintRepondre });
            
            // On enregistre l'admin actif si ce n'est pas déjà fait (idempotent basique ici en vérifiant si déjà rattaché)
            if (societe.NombreAdministrateursActifs == 0)
            {
                societe.EnregistrerAdministrateurActif();
                await context.SaveChangesAsync();
            }

            // 4. Créer et rattacher l'utilisateur standard
            var standardEmail = $"client.{nomProfil}.user@test.local";
            var standardUser = await CreerUtilisateurDeBaseAsync(standardEmail, $"User {s.Profil}", password);
            await RattacherClientASocieteAsync(standardUser, societe, DroitConsultation.PropresDemandesUniquement, false, new[] { Permission.ComplaintSoumettre, Permission.ComplaintRepondre });
            
            // Client sans droit (sur la société Normal uniquement)
            if (s.Profil == TypeClient.Normal)
            {
                var sansDroitUser = await CreerUtilisateurDeBaseAsync("client.sansdroit@test.local", "Client Sans Droit", password);
                await RattacherClientASocieteAsync(sansDroitUser, societe, DroitConsultation.PropresDemandesUniquement, false, Array.Empty<string>());
            }
        }
    }

    private async Task<ApplicationUser> CreerUtilisateurDeBaseAsync(string email, string nomAffiche, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                NomAffiche = nomAffiche,
                Nature = NatureUtilisateur.Client,
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
        return user;
    }

    private async Task RattacherClientASocieteAsync(ApplicationUser user, SocieteCliente societe, DroitConsultation droit, bool estAdmin, string[] permissions)
    {
        var existe = await context.UtilisateursSociete.AnyAsync(u => u.UtilisateurId == user.Id);
        if (!existe)
        {
            var rattach = UtilisateurSociete.Rattacher(user.Id, societe.Id, DateTime.UtcNow, estAdmin, droitSoumission: true);
            rattach.ModifierDroits(droit, droitSoumission: true); // simplified for this seed
            context.UtilisateursSociete.Add(rattach);

            foreach (var perm in permissions)
            {
                context.Habilitations.Add(HabilitationUtilisateur.Attribuer(user.Id, perm));
            }
            await context.SaveChangesAsync();
        }
    }

    private async Task SeedFilesEtAgentsAsync(string password)
    {
        var files = new[]
        {
            "Pilote Manifeste",
            "Facturation",
            "Validation Paiement",
            "Contrôle Documentaire",
            "Comptabilité Client",
            "Back Office Export",
            "IES",
            "Support Opérationnel",
            "Avoir & Exonération"
        };

        foreach (var f in files)
        {
            if (!await context.Files.AnyAsync(x => x.Nom == f))
            {
                var module = f == "Avoir & Exonération" ? "AvoirExoneration" : "Billing Office";
                context.Files.Add(FileDeTraitement.Creer(module, f));
            }
        }
        await context.SaveChangesAsync();

        await CreerAgentAsync("agent.import@test.local", "Agent Import", password, new (string, string?)[]
        {
            (Permission.BillingImportSoumettre, null),
            (Permission.BillingImportManifesteInstruire, "Pilote Manifeste")
        });

        await CreerAgentAsync("agent.export@test.local", "Agent Export", password, new (string, string?)[]
        {
            (Permission.BillingExportSoumettre, null),
            (Permission.BillingExportBackOfficeInstruire, "Back Office Export")
        });

        await CreerAgentAsync("agent.facturation@test.local", "Agent Facturation", password, new (string, string?)[]
        {
            (Permission.BillingImportFacturationEditer, "Facturation"),
            (Permission.BillingExportFacturationEditer, "Facturation"),
            (Permission.GuichetRecherche, null),
            (Permission.RendezVousGerer, null),
            (Permission.AvoirExonerationInstruire, "Avoir & Exonération"),
            (Permission.AvoirExonerationDecider, null)
        });

        await CreerAgentAsync("agent.paiement@test.local", "Agent Paiement", password, new (string, string?)[]
        {
            (Permission.BillingImportPaiementValider, "Validation Paiement"),
            (Permission.BillingExportPaiementValider, "Validation Paiement"),
            (Permission.GuichetRecherche, null),
            (Permission.RendezVousGerer, null)
        });

        await CreerAgentAsync("agent.cd@test.local", "Agent CD", password, new (string, string?)[]
        {
            (Permission.BillingImportControleDocumentaireTraiter, "Contrôle Documentaire"),
            (Permission.BillingExportControleDocumentaireTraiter, "Contrôle Documentaire")
        });

        await CreerAgentAsync("agent.compta@test.local", "Agent Compta", password, new (string, string?)[]
        {
            (Permission.BillingImportComptabiliteConfirmerLettrage, "Comptabilité Client"),
            (Permission.BillingExportComptabiliteConfirmerLettrage, "Comptabilité Client"),
            (Permission.AvoirExonerationInstruire, "Avoir & Exonération"),
            (Permission.AvoirExonerationDecider, null)
        });

        await CreerAgentAsync("agent.ies@test.local", "Agent IES", password, new (string, string?)[]
        {
            (Permission.BillingImportIesManifesteInstruire, "IES"),
            (Permission.BillingImportIesControleDocumentaireInstruire, "IES"),
            (Permission.BillingExportIesBackOfficeInstruire, "IES"),
            (Permission.BillingExportIesControleDocumentaireInstruire, "IES")
        });

        await CreerAgentAsync("superviseur@test.local", "Superviseur", password, new (string, string?)[]
        {
            (Permission.SupervisionConsulter, null),
            (Permission.SupervisionAgir, null),
            (Permission.ActionDeMasse, null),
            (Permission.AdministrationFiles, null),
            (Permission.GuichetRecherche, null),
            (Permission.RendezVousGerer, null)
        });

        await CreerAgentAsync("tarification@test.local", "Admin Tarification", password, new (string, string?)[]
        {
            (Permission.TarificationAdministrer, null)
        });

        await CreerAgentAsync("agent.support@test.local", "Agent Support", password, new (string, string?)[]
        {
            (Permission.ComplaintInstruire, "Support Opérationnel"),
            (Permission.AdministrationCategoriesReclamation, null)
        });

        // Agent sans droit (pour tester 403)
        await CreerAgentAsync("agent.sansdroit@test.local", "Agent Sans Droit", password, Array.Empty<(string, string?)>());
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

    private async Task SeedTicketsDemoAsync()
    {
        if (!await context.CategoriesReclamation.AnyAsync())
        {
            // REMÉDIATION POST-AUDIT (post-implementation-audit.md §2.5) : toute catégorie active
            // proposée au client doit avoir une file cible — sans quoi SoumettreReclamationCommand
            // refuse systématiquement la soumission (règle métier déjà existante, non modifiée
            // ici). "INFO" pointait vers null par erreur ; elle route désormais vers la même file
            // Support Opérationnel que FACT/TECH (résolue par nom, jamais par un Guid en dur).
            var fileSupportId = await context.Files.Where(f => f.Nom == "Support Opérationnel").Select(f => f.Id).FirstOrDefaultAsync();
            context.CategoriesReclamation.Add(Domain.Reclamations.CategorieReclamation.Creer("FACT", "Problème de facturation", fileSupportId));
            context.CategoriesReclamation.Add(Domain.Reclamations.CategorieReclamation.Creer("TECH", "Problème technique", fileSupportId));
            context.CategoriesReclamation.Add(Domain.Reclamations.CategorieReclamation.Creer("INFO", "Demande d'information", fileSupportId));
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// FEATURE 008 (Enrôlement Transporteurs, research.md D4/D9, tasks.md T011-T013) : file
    /// "Agent Gate" (Module="Enrolement"), un compte Agent Gate habilité EnrolementInstruire, et un
    /// compte Transporteur/Chauffeur de démonstration — CLAR-ENR-01 : personne physique unique,
    /// sans société rattachée (Nature=Client, aucun UtilisateurSociete créé pour ce compte).
    /// </summary>
    private async Task SeedEnrolementAsync(string password)
    {
        if (!await context.Files.AnyAsync(x => x.Nom == "Agent Gate"))
        {
            context.Files.Add(FileDeTraitement.Creer("Enrolement", "Agent Gate"));
            await context.SaveChangesAsync();
        }

        await CreerAgentAsync("agent.gate@test.local", "Agent Gate", password, new (string, string?)[]
        {
            (Permission.EnrolementInstruire, "Agent Gate"),
            (Permission.GuichetRecherche, null),
            (Permission.RendezVousGerer, null)
        });

        // Décision d'attribution tracée (research.md D3/D9, tasks.md T013) : EnrolementSoumettre
        // est accordée par défaut à tout compte Nature=Client non rattaché à une société — mirroring
        // ComplaintSoumettre/BillingImportSoumettre déjà accordés de façon équivalente.
        var transporteur = await CreerUtilisateurDeBaseAsync("transporteur.chauffeur@test.local", "Transporteur Chauffeur Test", password);
        if (!await context.Habilitations.AnyAsync(h => h.UtilisateurId == transporteur.Id && h.PermissionCode == Permission.EnrolementSoumettre))
        {
            context.Habilitations.Add(HabilitationUtilisateur.Attribuer(transporteur.Id, Permission.EnrolementSoumettre));
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Compte Super Administrateur CIT (M. KONATE) :
    /// - Email : m.konate@cotedivoireterminal.com
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
}

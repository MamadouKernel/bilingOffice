using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Persistence;

public sealed partial class DevelopmentSeeder
{
    /// <summary>
    /// Jeu de démonstration côté CLIENT : une société active, un compte client rattaché, et quatre
    /// dossiers Import à des étapes différentes (nouveau, en cours, en attente client, clôturé) —
    /// de quoi voir « Mes demandes » et l'assistant répondre sur de vraies données YABA.
    ///
    /// Login : voir <c>DemoData:ClientEmail</c> dans appsettings.Development.json. Le mot de passe
    /// est celui de <c>CIT_TEST_USERS_PASSWORD</c> (jamais écrit ici ni en config), comme le compte
    /// Super Admin. Idempotent : ne fait rien si le compte client existe déjà.
    /// </summary>
    private async Task SeedClientDemoAsync(string motDePasse)
    {
        const string emailClient = "client.demo@cit-demo.ci";

        if (await userManager.FindByEmailAsync(emailClient) is not null)
        {
            return;
        }

        var client = new ApplicationUser
        {
            UserName = emailClient,
            Email = emailClient,
            NomAffiche = "Awa Client Démo",
            Nature = NatureUtilisateur.Client,
            EmailConfirmed = true,
            EstActif = true,
        };

        var creation = await userManager.CreateAsync(client, motDePasse);
        if (!creation.Succeeded)
        {
            logger.LogWarning("Amorçage démo client : création du compte échouée ({Erreurs}).",
                string.Join(", ", creation.Errors.Select(e => e.Code)));
            return;
        }

        var maintenant = DateTime.UtcNow;

        var societe = SocieteCliente.Soumettre(
            client.Id, "Société Démo Import SARL", "CI-ABJ-2020-B-12345",
            "Client de démonstration — données fictives.", TypeClient.EnCompte, maintenant);
        societe.Valider();
        societe.EnregistrerAdministrateurActif();
        context.Societes.Add(societe);

        context.UtilisateursSociete.Add(
            UtilisateurSociete.Rattacher(client.Id, societe.Id, maintenant, estAdministrateur: true));

        // Opérateur pour les transitions engageantes : le Super Admin déjà amorcé (repli sur le
        // client si absent, pour ne jamais faire échouer l'amorçage).
        var operateur = await userManager.FindByEmailAsync("m.konate@cotedivoireterminal.com");
        var operateurId = operateur?.Id ?? client.Id;

        var fileManifesteId = await context.Files
            .Where(f => f.Module == "BillingImport" && f.Nom == "Pilote Manifeste")
            .Select(f => f.Id)
            .FirstAsync();

        // ponytail: numéros de démo fixes (préfixe DEMO), hors de la plage des séquences réelles
        // (DOS-000001…) — pas de collision ; une purge les recrée à l'identique.
        void Creer(string suffixe, Action<Ticket> etape)
        {
            var dossier = Dossier.Creer($"DOS-DEMO-{suffixe}", societe.Id, maintenant, client.Id, societe.Profil);
            context.Dossiers.Add(dossier);

            var ticket = Ticket.Creer(
                $"BIL-IMP-DEMO-{suffixe}", "BillingImport", "Import",
                fileManifesteId, dossier.Id, maintenant, numeroBl: $"DEMOBL{suffixe}");
            etape(ticket);
            context.Tickets.Add(ticket);
        }

        Creer("0001", _ => { });                                                  // nouveau (en attente de prise en charge)
        Creer("0002", t => t.PrendreEnCharge(operateurId, maintenant));           // en cours
        Creer("0003", t => { t.PrendreEnCharge(operateurId, maintenant); t.MettreEnAttenteClient(maintenant); }); // complément attendu
        Creer("0004", t => { t.PrendreEnCharge(operateurId, maintenant); t.Cloturer(MotifClotureType.Normale, maintenant); }); // clôturé

        logger.LogInformation("Amorçage démo client terminé : société active + 4 dossiers Import.");
    }
}

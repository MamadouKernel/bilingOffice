using System;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Guichet;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Audit;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Guichet;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.Guichet;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CIT.Plateforme.Web.Tests.E2E;

/// <summary>
/// FEATURE 010 (Continuité Guichet & Rendez-vous) — T061.
/// Rejoue REC-SCE-14 ("Continuité Guichet & Rendez-vous" — docs/04-matrice-recette.md)
/// de bout en bout, couvrant les 14 étapes du scénario contractuel sur SQL Server réel :
/// 1. Création d'un dossier et ticket Import avec BL et document déposé
/// 2. Consultation des créneaux disponibles (C10-03)
/// 3. Prise de rendez-vous guichet (C10-04 / CON-03)
/// 4. Publication de l'événement RendezVousReserve pour le moteur de notifications
/// 5. Consultation de mes rendez-vous par l'usager (C10-09)
/// 6. Report de rendez-vous vers un nouveau créneau (C10-06)
/// 7. Vérification de la libération de l'ancien créneau et occupation du nouveau (CAS-RDV-06)
/// 8. Connexion de l'agent guichet habilité (Permission.GuichetRecherche / Permission.RendezVousGerer)
/// 9. Recherche rapide multicritère par numéro BL (CON-02 / C10-01)
/// 10. Restitution intégrale du dossier guichet avec pièces jointes et SANS AUCUNE RESSAISIE (CON-01 / C10-02)
/// 11. Validation de la présence physique de l'usager au guichet (C10-07 / CON-04)
/// 12. Émission de l'événement RendezVousHonore et consignation d'historique
/// 13. Tentative de double réservation active rejetée par l'index unique filtré (CAS-RDV-01)
/// 14. Régularisation d'une opération physique exécutée en mode dégradé lors d'une indisponibilité (ERR-04 / C10-11)
/// </summary>
public sealed class RecSce14ContinuiteGuichetRendezVousE2ETests : IClassFixture<PlateformeWebApplicationFactory>
{
    private readonly PlateformeWebApplicationFactory _factory;

    public RecSce14ContinuiteGuichetRendezVousE2ETests(PlateformeWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Scenario_Complet_REC_SCE_14_DeBoutEnBout()
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var context = sp.GetRequiredService<PlateformeDbContext>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        // Fixtures explicites : la société de profil Normal et le rattachement du client sont
        // créés ici, et non hérités de l'amorçage applicatif.
        var client = await FixtureComptesDeTest.CreerClientNormalRattacheAsync(sp);
        var agentGuichet = await FixtureComptesDeTest.CreerAgentFacturationAsync(sp);

        var maintenant = DateTime.UtcNow;
        var suffixe = Guid.NewGuid().ToString("N")[..8];
        var numeroBl = $"BL-REC14-{suffixe}";

        // === ÉTAPE 1 : Création dossier & ticket Import avec document joint ===
        var societe = await context.Societes.FirstAsync(s => s.Profil == TypeClient.Normal);
        var dossier = Dossier.Creer($"DOS-REC14-{suffixe}", societe.Id, maintenant, client.Id, TypeClient.Normal);
        context.Dossiers.Add(dossier);
        await context.SaveChangesAsync();

        var file = await context.Files.FirstAsync(f => f.Nom == "Facturation");
        var ticket = Ticket.Creer($"CIT-IMP-REC14-{suffixe}", "BillingImport", "DemandeImport", file.Id, dossier.Id, maintenant, numeroBl);
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var document = Document.Deposer(ticket.Id, client.Id, "connaissement_original.pdf", "application/pdf", 2048, $"stockage/{numeroBl}.pdf", maintenant);
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        // === ÉTAPE 2 & 3 : Consultation des créneaux & Prise de rendez-vous (C10-03, C10-04) ===
        var dateRdv = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var heureRdv = new TimeSpan(9, 0, 0);

        var listerCreneauxQuery = sp.GetRequiredService<ListerCreneauxDisponiblesQuery>();
        var creneauxInitiaux = await listerCreneauxQuery.ExecuterAsync(dateRdv);
        Assert.NotEmpty(creneauxInitiaux);

        var reserverCommand = sp.GetRequiredService<ReserverRendezVousCommand>();
        var rdvId = await reserverCommand.ExecuterAsync(new ReserverRendezVousRequest(
            client.Id, ticket.Id, dateRdv, heureRdv, "Paiement Caisse"));

        Assert.NotEqual(Guid.Empty, rdvId);

        // === ÉTAPE 4 : Vérification de l'événement RendezVousReserve pour le moteur de notifications ===
        var evenementReserve = await context.JournalEvenements
            .OrderByDescending(e => e.HorodatageUtc)
            .FirstOrDefaultAsync(e => e.TypeEvenement == "RendezVousReserve");
        Assert.NotNull(evenementReserve);
        Assert.Contains(rdvId.ToString(), evenementReserve.ChargeUtile);

        // === ÉTAPE 5 : Consultation de mes rendez-vous par l'usager (C10-09) ===
        var listerMesRdvQuery = sp.GetRequiredService<ListerMesRendezVousQuery>();
        var mesRdvs = await listerMesRdvQuery.ExecuterAsync(client.Id);
        Assert.Single(mesRdvs);
        Assert.Equal(rdvId, mesRdvs[0].Id);
        Assert.Equal("Reserve", mesRdvs[0].Statut);

        // === ÉTAPE 6 & 7 : Report du rendez-vous vers un nouveau créneau (C10-06 / CAS-RDV-06) ===
        var nouvelleDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2));
        var nouvelleHeure = new TimeSpan(11, 0, 0);
        var reporterCommand = sp.GetRequiredService<ReporterRendezVousCommand>();

        await reporterCommand.ExecuterAsync(new ReporterRendezVousRequest(
            client.Id, rdvId, nouvelleDate, nouvelleHeure));

        var rdvRepo = sp.GetRequiredService<IRendezVousRepository>();
        var placeAncienCreneau = await rdvRepo.CompterReservationsCreneauAsync(dateRdv, heureRdv);
        var placeNouveauCreneau = await rdvRepo.CompterReservationsCreneauAsync(nouvelleDate, nouvelleHeure);
        Assert.Equal(0, placeAncienCreneau); // Ancien créneau libéré
        Assert.Equal(1, placeNouveauCreneau); // Nouveau créneau occupé

        // === ÉTAPE 8 & 9 : Connexion agent guichet & Recherche rapide par BL (CON-02 / C10-01) ===
        var rechercheQuery = sp.GetRequiredService<RechercherGuichetQuery>();
        var resultatsRecherche = await rechercheQuery.ExecuterAsync(agentGuichet.Id, new CriteresRechercheGuichet(numeroBl, null));
        Assert.Single(resultatsRecherche);
        Assert.Equal(ticket.Id, resultatsRecherche[0].TicketId);
        Assert.True(resultatsRecherche[0].PossedeRendezVousActif);

        // === ÉTAPE 10 : Restitution intégrale du dossier guichet sans ressaisie (CON-01 / C10-02) ===
        var consulterDossierQuery = sp.GetRequiredService<ConsulterDossierGuichetQuery>();
        var dossierGuichet = await consulterDossierQuery.ExecuterAsync(agentGuichet.Id, ticket.Id);
        Assert.NotNull(dossierGuichet);
        Assert.Equal(ticket.Numero, dossierGuichet.NumeroTicket);
        Assert.Equal(dossier.Numero, dossierGuichet.NumeroDossier);
        Assert.Equal(numeroBl, dossierGuichet.ReferenceBlOuBooking);
        Assert.Single(dossierGuichet.Documents);
        Assert.Equal("connaissement_original.pdf", dossierGuichet.Documents[0].NomFichier);
        Assert.NotNull(dossierGuichet.RendezVousActif);
        Assert.Equal(nouvelleDate, dossierGuichet.RendezVousActif.Date);

        // === ÉTAPE 11 & 12 : Validation de la présence physique de l'usager (C10-07 / CON-04) ===
        var marquerHonoreCommand = sp.GetRequiredService<MarquerRendezVousHonoreCommand>();
        await marquerHonoreCommand.ExecuterAsync(new MarquerHonoreRequest(
            agentGuichet.Id, rdvId, "Usager accueilli et orienté vers la caisse", "FILE-E2E-001"));

        var rdvApresPresence = await rdvRepo.ObtenirParIdAsync(rdvId);
        Assert.NotNull(rdvApresPresence);
        Assert.Equal(StatutRendezVous.Honore, rdvApresPresence.Statut);

        var evenementHonore = await context.JournalEvenements
            .OrderByDescending(e => e.HorodatageUtc)
            .FirstOrDefaultAsync(e => e.TypeEvenement == "RendezVousHonore");
        Assert.NotNull(evenementHonore);

        // === ÉTAPE 13 : Tentative de double réservation active sur ticket (CAS-RDV-01) ===
        // Comme le premier rdv est désormais "Honore", un nouveau rdv actif peut être créé si besoin,
        // mais pas deux au statut "Reserve" en même temps.
        var rdvId2 = await reserverCommand.ExecuterAsync(new ReserverRendezVousRequest(
            client.Id, ticket.Id, nouvelleDate, new TimeSpan(14, 0, 0), "Deuxième démarche"));

        // Une tentative immédiate de réserver un 3ème alors que rdvId2 est Reserve doit lever une exception
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            reserverCommand.ExecuterAsync(new ReserverRendezVousRequest(
                client.Id, ticket.Id, nouvelleDate, new TimeSpan(15, 0, 0), "Tentative concurrente")));

        // === ÉTAPE 14 : Régularisation mode dégradé physique (ERR-04 / C10-11) ===
        var regulariserCommand = sp.GetRequiredService<RegulariserModeDegradeCommand>();
        var dateOperationPhysique = DateTime.UtcNow.AddHours(-2);
        await regulariserCommand.ExecuterAsync(new RegulariserModeDegradeRequest(
            agentGuichet.Id,
            ticket.Id,
            dateOperationPhysique,
            "Encaissement physique manuel du montant BL pendant coupure réseau"));

        var auditRegul = await context.JournalAudit
            .OrderByDescending(a => a.HorodatageUtc)
            .FirstOrDefaultAsync(a => a.Action == "ModeDegrade.Regularisation");
        Assert.NotNull(auditRegul);
        Assert.Equal(ticket.Id, auditRegul.EntiteId);

        var historiqueRegul = await context.HistoriqueTickets
            .OrderByDescending(h => h.HorodatageUtc)
            .FirstOrDefaultAsync(h => h.Evenement == "ModeDegradeRegularise");
        Assert.NotNull(historiqueRegul);
        Assert.Contains("MODE DÉGRADÉ", historiqueRegul.Detail);
    }
}

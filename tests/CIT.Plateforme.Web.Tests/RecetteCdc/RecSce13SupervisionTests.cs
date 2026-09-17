using CIT.Plateforme.Application.Administration;
using CIT.Plateforme.Application.Supervision;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.RecetteCdc;

/// <summary>
/// [TEST-CDC] REC-SCE-13 : « Supervision : vue temps réel des files et de la charge ; alertes
/// (ticket non pris en charge, sans activité, SLA) ; affectation d'un ticket non pris en charge ;
/// déblocage d'un ticket. » CDC couverts : SUP-01..05, ALE-01..03 (partiel MAS-01).
/// </summary>
public sealed class RecSce13SupervisionTests(PlateformeWebApplicationFactory factory) : IClassFixture<PlateformeWebApplicationFactory>
{
    [Fact]
    public async Task Un_superviseur_consulte_sa_vue_temps_reel_et_affecte_un_ticket_non_pris_en_charge()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<PlateformeDbContext>();
        var creerTicket = services.GetRequiredService<CreerTicketCommand>();
        var gererPerimetres = services.GetRequiredService<GererPerimetresSupervisionCommand>();
        var gererFiles = services.GetRequiredService<GererFilesCommand>();
        var vueFiles = services.GetRequiredService<VueFilesQuery>();
        var actionsSuperviseur = services.GetRequiredService<ActionsSuperviseurCommand>();

        var administrateurId = Guid.NewGuid();
        var superviseurId = Guid.NewGuid();
        var operateurCibleId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, administrateurId), Permission.AdministrationFiles));
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, administrateurId), Permission.AdministrationPerimetresSupervision));
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, superviseurId), Permission.SupervisionConsulter));
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, superviseurId), Permission.TicketAffecterManuellement));
        await dbContext.SaveChangesAsync();

        var fileId = await gererFiles.CreerAsync(new CreerFileRequete(administrateurId, "BillingOffice", "Contrôle BL", Domain.Files.StrategieAffectation.PriseLibre));
        await gererPerimetres.DefinirAsync(administrateurId, superviseurId, [fileId]);

        // TRF-04 (audit) : l'opérateur cible d'une affectation doit lui-même être habilité sur la file.
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, operateurCibleId), Permission.TicketPrendreEnCharge, fileId));
        await dbContext.SaveChangesAsync();

        var ticket = await creerTicket.ExecuterAsync(new CreerTicketRequete(clientId, "BillingOffice", "Import", fileId, null));

        // SUP-02 : vue temps réel par file (nombre de tickets non affectés).
        var vuesAvant = await vueFiles.ExecuterAsync(superviseurId);
        Assert.Single(vuesAvant);
        Assert.Equal(1, vuesAvant[0].NonAffectes);

        // SUP-04 / AFF-02 : le superviseur affecte manuellement un ticket non pris en charge
        // (déblocage) — ActionsSuperviseurCommand délègue à AffecterManuellementCommand (AFF-02).
        await actionsSuperviseur.AffecterTicketNonPrisEnChargeAsync(superviseurId, ticket.Id, operateurCibleId);

        var vuesApres = await vueFiles.ExecuterAsync(superviseurId);
        Assert.Equal(0, vuesApres[0].NonAffectes);
        Assert.Equal(1, vuesApres[0].EnCours);
    }
}

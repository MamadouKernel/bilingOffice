using CIT.Plateforme.Application.BillingExport;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.BillingExport;

/// <summary>
/// [TEST-SPEC] Contrat C5-02 — Consultation sécurisée d'une file Export.
/// </summary>
public sealed class ConsulterFileBillingExportQueryTests
{
    [Fact]
    public async Task Agent_avec_permission_peut_consulter_la_file_export()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var file = FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.BackOfficeExport);
        dbContext.Files.Add(file);

        var ticket = Ticket.Creer("TCK-01", SoumettreDemandeExportCommand.Module, "EXPORT", file.Id, Guid.NewGuid(), DateTime.UtcNow, numeroBooking: "BKG-01");
        dbContext.Tickets.Add(ticket);

        var agentId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.BillingExportBackOfficeInstruire));
        await dbContext.SaveChangesAsync();

        var query = new ConsulterFileBillingExportQuery(new TicketRepository(dbContext), new FileDeTraitementRepository(dbContext), new AutorisationService(dbContext));
        var resultats = await query.ExecuterAsync(agentId, NomsFilesExport.BackOfficeExport, [Permission.BillingExportBackOfficeInstruire]);

        Assert.Single(resultats);
        Assert.Equal("TCK-01", resultats[0].Numero);
    }

    [Fact]
    public async Task Agent_sans_permission_est_refuse()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var file = FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.BackOfficeExport);
        dbContext.Files.Add(file);
        await dbContext.SaveChangesAsync();

        var agentId = Guid.NewGuid();
        var query = new ConsulterFileBillingExportQuery(new TicketRepository(dbContext), new FileDeTraitementRepository(dbContext), new AutorisationService(dbContext));
        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            query.ExecuterAsync(agentId, NomsFilesExport.BackOfficeExport, [Permission.BillingExportBackOfficeInstruire]));
    }
}

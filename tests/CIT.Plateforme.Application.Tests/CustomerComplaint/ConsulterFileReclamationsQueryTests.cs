using CIT.Plateforme.Application.CustomerComplaint;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.CustomerComplaint;

/// <summary>
/// Plusieurs files peuvent s'appeler « Support Opérationnel » (une file héritée d'un ancien module
/// coexiste avec celle du module des réclamations) : la file Support ne doit pas se limiter à la
/// première trouvée, sinon les réclamations routées vers l'autre deviennent invisibles.
/// </summary>
public sealed class ConsulterFileReclamationsQueryTests
{
    [Fact]
    public async Task Les_reclamations_de_toutes_les_files_Support_Operationnel_autorisees_sont_listees()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var fileHeritee = FileDeTraitement.Creer("Billing Office", "Support Opérationnel");
        var fileReclamations = FileDeTraitement.Creer("CustomerComplaint", "Support Opérationnel");
        dbContext.Files.AddRange(fileHeritee, fileReclamations);

        var ticketHerite = Ticket.Creer("REC-1", "CustomerComplaint", "Reclamation", fileHeritee.Id, Guid.NewGuid(), horloge.MaintenantUtc);
        var ticketReclamation = Ticket.Creer("REC-2", "CustomerComplaint", "Reclamation", fileReclamations.Id, Guid.NewGuid(), horloge.MaintenantUtc);
        dbContext.Tickets.AddRange(ticketHerite, ticketReclamation);

        var agentGlobalId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentGlobalId), Permission.ComplaintInstruire, null));
        var agentHeriteId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentHeriteId), Permission.ComplaintInstruire, fileHeritee.Id));
        await dbContext.SaveChangesAsync();

        var query = new ConsulterFileReclamationsQuery(
            new TicketRepository(dbContext), new FileDeTraitementRepository(dbContext), new AutorisationService(dbContext));

        var vusParAgentGlobal = (await query.ExecuterAsync(agentGlobalId)).Select(t => t.Id).ToList();
        var vusParAgentHerite = (await query.ExecuterAsync(agentHeriteId)).Select(t => t.Id).ToList();

        Assert.Equal(2, vusParAgentGlobal.Count);
        Assert.Contains(ticketReclamation.Id, vusParAgentGlobal);
        Assert.Contains(ticketHerite.Id, vusParAgentGlobal);
        Assert.Equal([ticketHerite.Id], vusParAgentHerite);
    }
}

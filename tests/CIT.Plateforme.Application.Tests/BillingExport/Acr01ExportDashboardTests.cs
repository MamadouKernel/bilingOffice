using CIT.Plateforme.Application.BillingExport;
using CIT.Plateforme.Application.Client;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.BillingExport;

/// <summary>
/// [TEST-SPEC] ACR-01 Export — Les demandes en attente client côté Export remontent dans le dashboard client "Mes actions requises".
/// </summary>
public sealed class Acr01ExportDashboardTests
{
    [Fact]
    public async Task Demande_export_en_attente_client_apparait_dans_les_actions_requises_ACR_01()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var ticketRepository = new TicketRepository(dbContext);
        var dossierRepository = new DossierRepository(dbContext);
        var societeRepository = new SocieteClienteRepository(dbContext);
        var utilisateurSocieteRepository = new UtilisateurSocieteRepository(dbContext);

        var file = FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.BackOfficeExport);
        dbContext.Files.Add(file);

        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "SocieteExportACR", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var utilisateurId = Guid.NewGuid();

        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, utilisateurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));

        var dossier = Dossier.Creer("DOS-EXP-ACR-01", societe.Id, DateTime.UtcNow, auteurId: utilisateurId, typeClient: TypeClient.Normal);
        dbContext.Dossiers.Add(dossier);

        var ticket = Ticket.Creer("TCK-EXP-ACR-01", SoumettreDemandeExportCommand.Module, "EXPORT", file.Id, dossier.Id, DateTime.UtcNow, numeroBooking: "BKG-ACR-01");
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        ticket.MettreEnAttenteClient(DateTime.UtcNow);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        var query = new ObtenirDashboardClientQuery(dossierRepository, ticketRepository, societeRepository, utilisateurSocieteRepository);
        var dashboard = await query.ExecuterAsync(utilisateurId, societe.Id);

        Assert.Single(dashboard.ActionsRequises);
        var action = dashboard.ActionsRequises[0];
        Assert.Equal(ticket.Id, action.TicketId);
        Assert.Equal(TypeActionRequise.ComplementExport, action.TypeAction);
        Assert.Equal("BKG-ACR-01", action.NumeroBooking);
        Assert.Contains("BKG-ACR-01", action.Titre);
    }
}

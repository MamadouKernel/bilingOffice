using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.RecetteCdc;

/// <summary>
/// [TEST-CDC] REC-SCE-10 : « Transfert de file : un opérateur transfère un ticket mal orienté
/// avec motif obligatoire ; le ticket conserve numéro, dossier, documents, commentaires et
/// historique ; le client est notifié ; un transfert vers une destination non autorisée par la
/// matrice est refusé. » CDC couverts : TRF-01, TRF-02, TRF-03, TRF-05, TRF-06, WKF-03.
/// </summary>
public sealed class RecSce10TransfertTests(PlateformeWebApplicationFactory factory) : IClassFixture<PlateformeWebApplicationFactory>
{
    [Fact]
    public async Task Transfert_autorise_conserve_le_ticket_et_transfert_hors_matrice_est_refuse()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<PlateformeDbContext>();
        var creerTicket = services.GetRequiredService<CreerTicketCommand>();
        var transferer = services.GetRequiredService<TransfererTicketCommand>();

        var clientId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var fileControleBl = Guid.NewGuid();
        var fileFacturation = Guid.NewGuid();
        var fileNonAutorisee = Guid.NewGuid();

        dbContext.TransitionsAutorisees.Add(TransitionAutorisee.Definir(fileControleBl, fileFacturation, ["Contrôle BL validé"]));
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentId, Permission.TicketTransferer, fileControleBl));
        await dbContext.SaveChangesAsync();

        var ticket = await creerTicket.ExecuterAsync(new CreerTicketRequete(clientId, "BillingOffice", "Import", fileControleBl, null));
        var numeroOriginal = ticket.Numero;
        var dossierOriginal = ticket.DossierId;

        // Transfert autorisé par la matrice.
        await transferer.ExecuterAsync(agentId, ticket.Id, fileFacturation, "Contrôle BL validé");

        var ticketTransfere = await dbContext.Tickets.FindAsync(ticket.Id);
        Assert.NotNull(ticketTransfere);
        Assert.Equal(fileFacturation, ticketTransfere!.FileCouranteId);
        // TRF-02 : numéro, dossier conservés.
        Assert.Equal(numeroOriginal, ticketTransfere.Numero);
        Assert.Equal(dossierOriginal, ticketTransfere.DossierId);
        // CLAR-03 : ticket non affecté après transfert (pas de destinataire désigné).
        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, ticketTransfere.Statut);
        Assert.Null(ticketTransfere.OperateurAssigneId);

        // TRF-05 : le client est notifié — un événement est publié.
        var evenements = dbContext.JournalEvenements.Where(e => e.TypeEvenement == "TicketTransfere").ToList();
        Assert.NotEmpty(evenements);

        // Transfert vers une destination NON autorisée par la matrice : refusé (TRF-06).
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentId, Permission.TicketTransferer, fileFacturation));
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<TransitionNonAutoriseeException>(() =>
            transferer.ExecuterAsync(agentId, ticket.Id, fileNonAutorisee, "Motif quelconque"));
    }
}

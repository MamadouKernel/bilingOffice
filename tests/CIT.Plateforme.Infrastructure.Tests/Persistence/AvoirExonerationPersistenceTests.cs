using System;
using System.Threading.Tasks;
using CIT.Plateforme.Domain.AvoirExoneration;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Persistence;

/// <summary>
/// FEATURE 007 (AVE-01..04) : Tests d'intégration et de persistance des colonnes additives sous SQL Server réel.
/// </summary>
public sealed class AvoirExonerationPersistenceTests
{
    private static readonly DateTime MaintenantUtc = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Persistance_TicketAvoirEtDecision_RoundtripSqlServerReussi()
    {
        await using var db = BaseDeTestSql.CreerContexte();
        var clientId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var dossier = Dossier.Creer($"DOS-AVO-{Guid.NewGuid():N}"[..16], clientId, MaintenantUtc, clientId);
        db.Dossiers.Add(dossier);

        var file = FileDeTraitement.Creer("AvoirExoneration", $"File-AVO-{Guid.NewGuid():N}"[..16]);
        db.Files.Add(file);
        await db.SaveChangesAsync();

        var numeroTicket = $"AVO-2026-{Guid.NewGuid():N}"[..14];
        var ticket = Ticket.CreerDemandeAvoirExo(
            numero: numeroTicket,
            typeDemande: "Avoir",
            fileCibleId: file.Id,
            dossierId: dossier.Id,
            objet: "Contestation litige acconage",
            description: "Surfacturation",
            montantConteste: 150000m,
            maintenantUtc: MaintenantUtc);

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        // Rechargement depuis la base
        await using var db2 = BaseDeTestSql.CreerContexte();
        var ticketCharge = await db2.Tickets.FirstOrDefaultAsync(t => t.Id == ticket.Id);
        Assert.NotNull(ticketCharge);
        Assert.Equal("AvoirExoneration", ticketCharge.Module);
        Assert.Equal("Avoir", ticketCharge.TypeDemande);
        Assert.Equal(150000m, ticketCharge.MontantConteste);
        Assert.Null(ticketCharge.DecisionAvoirExo);

        // Enregistrement de la décision
        ticketCharge.PrendreEnCharge(agentId, MaintenantUtc);
        ticketCharge.EnregistrerDecisionAvoirExo(
            decision: DecisionAvoirExo.Accorde,
            motifRejet: null,
            montantAccorde: 140000m,
            decideurId: agentId,
            maintenantUtc: MaintenantUtc.AddHours(1));

        await db2.SaveChangesAsync();

        // Vérification de la persistance durable après clôture
        await using var db3 = BaseDeTestSql.CreerContexte();
        var ticketFinal = await db3.Tickets.FirstOrDefaultAsync(t => t.Id == ticket.Id);
        Assert.NotNull(ticketFinal);
        Assert.Equal(StatutTicket.Cloture, ticketFinal.Statut);
        Assert.Equal(DecisionAvoirExo.Accorde, ticketFinal.DecisionAvoirExo);
        Assert.Equal(140000m, ticketFinal.MontantAccorde);
        Assert.Equal(agentId, ticketFinal.DecideurAvoirExoId);
        Assert.Equal(MaintenantUtc.AddHours(1), ticketFinal.DateDecisionAvoirExoUtc);
    }
}

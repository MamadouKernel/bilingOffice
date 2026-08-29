using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tickets;

/// <summary>[TEST-SPEC] Exigence CDC: PRI-01, PRI-02, PRI-03, PRI-04 → US10 → scénario
/// d'acceptation 3 de US10 → Test. Aucun scénario CDC direct dans ce périmètre.</summary>
public sealed class PrioriteTests
{
    [Fact]
    public async Task PRI_03_toute_modification_manuelle_enregistre_ancien_nouveau_auteur_et_motif()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);

        var agentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentId, Permission.PrioriteModifier, fileId));
        var dossier = Dossier.Creer("DOS-PRI-000001", Guid.NewGuid(), horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-PRI-000001", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        var niveauUrgent = NiveauPriorite.Definir("Urgent", 1);
        dbContext.NiveauxPriorite.Add(niveauUrgent);
        await dbContext.SaveChangesAsync();

        var commande = new ModifierPrioriteCommand(ticketRepo, new PrioriteRepository(dbContext), autorisation, journalisation, horloge, uniteDeTravail);
        await commande.ExecuterAsync(agentId, ticket.Id, niveauUrgent.Id, "Client VIP");

        var modification = dbContext.ModificationsPriorite.Single(m => m.TicketId == ticket.Id);
        Assert.Null(modification.AncienNiveauId); // aucune priorité avant
        Assert.Equal(niveauUrgent.Id, modification.NouveauNiveauId);
        Assert.Equal(agentId, modification.AuteurId);
        Assert.Equal("Client VIP", modification.Motif);

        var ticketApres = await dbContext.Tickets.FindAsync(ticket.Id);
        Assert.Equal(niveauUrgent.Id, ticketApres!.PrioriteId);
    }

    [Fact]
    public async Task PRI_04_un_client_ne_peut_jamais_modifier_la_priorite()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);

        var clientId = Guid.NewGuid(); // jamais habilité PrioriteModifier
        var dossier = Dossier.Creer("DOS-PRI-000002", clientId, horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-PRI-000002", "BillingOffice", "Import", Guid.NewGuid(), dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        var niveau = NiveauPriorite.Definir("Critique", 0);
        dbContext.NiveauxPriorite.Add(niveau);
        await dbContext.SaveChangesAsync();

        var commande = new ModifierPrioriteCommand(ticketRepo, new PrioriteRepository(dbContext), autorisation, journalisation, horloge, uniteDeTravail);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            commande.ExecuterAsync(clientId, ticket.Id, niveau.Id, "Je veux être prioritaire"));
    }
}

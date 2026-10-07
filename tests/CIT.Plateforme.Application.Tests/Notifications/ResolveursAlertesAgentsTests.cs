using CIT.Plateforme.Application.Notifications.Resolveurs;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Notifications;

/// <summary>
/// [TEST-SPEC — ALERTES INTERNES] À l'arrivée d'une demande, seuls les AGENTS habilités de la file
/// concernée sont ciblés (cloisonnement) ; à la soumission d'une demande de compte, seuls les
/// détenteurs de la permission de validation des comptes le sont.
/// </summary>
public sealed class ResolveursAlertesAgentsTests
{
    [Fact]
    public async Task AgentsDeLaFile_ne_cible_que_les_habilites_de_la_file_du_ticket()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var file = FileDeTraitement.Creer("CustomerComplaint", "Support Opérationnel");
        db.Files.Add(file);
        var dossier = Dossier.Creer("DOS-N", Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid());
        db.Dossiers.Add(dossier);
        var ticket = Ticket.Creer("TCK-N", "CustomerComplaint", "Reclamation", file.Id, dossier.Id, DateTime.UtcNow, null);
        db.Tickets.Add(ticket);

        var support = Guid.NewGuid();      // habilité réclamations
        var facturier = Guid.NewGuid();    // autre rôle
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(support, Permission.ComplaintInstruire));
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(facturier, Permission.BillingImportFacturationEditer));
        await db.SaveChangesAsync();

        var resolveur = new ResolveurAgentsDeLaFile(new TicketRepository(db), new FileDeTraitementRepository(db), new HabilitationRepository(db));
        var destinataires = await resolveur.ResoudreAsync(ResolveurAgentsDeLaFile.TypeArrivee, $"{{\"ticketId\":\"{ticket.Id}\"}}");

        Assert.Equal([support], destinataires);
        Assert.DoesNotContain(facturier, destinataires);
        Assert.Equal(ticket.Id, await resolveur.ResoudreTicketAsync($"{{\"ticketId\":\"{ticket.Id}\"}}"));
    }

    [Fact]
    public async Task ValideursCompte_cible_les_detenteurs_de_CompteValider()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var valideur = Guid.NewGuid();
        var autre = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(valideur, Permission.CompteValider));
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(autre, Permission.ComplaintInstruire));
        await db.SaveChangesAsync();

        var resolveur = new ResolveurValideursCompte(new HabilitationRepository(db));
        var destinataires = await resolveur.ResoudreAsync(ResolveurValideursCompte.TypeDemande, "{\"societeId\":\"" + Guid.NewGuid() + "\"}");

        Assert.Equal([valideur], destinataires);
        Assert.DoesNotContain(autre, destinataires);
    }
}

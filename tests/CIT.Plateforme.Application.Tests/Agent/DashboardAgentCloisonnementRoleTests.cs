using CIT.Plateforme.Application.Agent;
using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Agent;

/// <summary>
/// [TEST-SPEC — CLOISONNEMENT PAR RÔLE] Un agent ne voit sur son tableau de bord QUE les files de son
/// périmètre : les compteurs et les tickets des autres files (donc des autres rôles) n'apparaissent
/// pas. Un agent « Caisse » (permission Validation Paiement uniquement) ne voit que sa file, jamais
/// les tickets de Pilote Manifeste ou de Facturation.
/// </summary>
public sealed class DashboardAgentCloisonnementRoleTests
{
    private static ObtenirDashboardAgentQuery Query(PlateformeDbContext db) =>
        new(new FileDeTraitementRepository(db), new TicketRepository(db), new HabilitationRepository(db),
            new DisponibiliteRepository(db),
            new CIT.Plateforme.Application.Securite.GardeIdentiteService(new UtilisateurInfoRepository(db)));

    [Fact]
    public async Task Un_agent_Caisse_ne_voit_que_sa_file_pas_les_tickets_des_autres_roles()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var agentId = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = agentId, UserName = "caisse@cit.ci", NomAffiche = "Caissier", Nature = NatureUtilisateur.Agent, EstActif = true });

        var mod = SoumettreDemandeImportCommand.Module;
        var fileManifeste = FileDeTraitement.Creer(mod, SoumettreDemandeImportCommand.FilePiloteManifeste);
        var fileFacturation = FileDeTraitement.Creer(mod, NomsFiles.Facturation);
        var fileValidation = FileDeTraitement.Creer(mod, NomsFiles.ValidationPaiement);
        db.Files.AddRange(fileManifeste, fileFacturation, fileValidation);

        // L'agent n'a QUE la permission Caisse (Validation Paiement).
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentId, Permission.BillingImportPaiementValider));

        var dossier = Dossier.Creer("DOS-CL", Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid());
        db.Dossiers.Add(dossier);
        // Tickets en attente dans 3 files différentes.
        db.Tickets.Add(Ticket.Creer("TCK-M", mod, "Import", fileManifeste.Id, dossier.Id, DateTime.UtcNow, "BL-M"));
        db.Tickets.Add(Ticket.Creer("TCK-F", mod, "Import", fileFacturation.Id, dossier.Id, DateTime.UtcNow, "BL-F"));
        db.Tickets.Add(Ticket.Creer("TCK-V1", mod, "Import", fileValidation.Id, dossier.Id, DateTime.UtcNow, "BL-V1"));
        db.Tickets.Add(Ticket.Creer("TCK-V2", mod, "Import", fileValidation.Id, dossier.Id, DateTime.UtcNow, "BL-V2"));
        await db.SaveChangesAsync();

        var dashboard = await Query(db).ExecuterAsync(agentId);

        // Seule la file Validation Paiement est visible.
        var file = Assert.Single(dashboard.FilesHabilitees);
        Assert.Equal(NomsFiles.ValidationPaiement, file.Nom);
        // Ses compteurs ne reflètent QUE ses propres tickets (2 en attente), jamais ceux des autres files.
        Assert.Equal(2, file.TicketsEnAttente);
        Assert.Equal(2, dashboard.TotalTicketsEnAttenteDansPerimetre);
        // Les files/tickets des autres rôles n'apparaissent pas.
        Assert.DoesNotContain(dashboard.FilesHabilitees, f => f.Nom == SoumettreDemandeImportCommand.FilePiloteManifeste);
        Assert.DoesNotContain(dashboard.FilesHabilitees, f => f.Nom == NomsFiles.Facturation);
    }
}

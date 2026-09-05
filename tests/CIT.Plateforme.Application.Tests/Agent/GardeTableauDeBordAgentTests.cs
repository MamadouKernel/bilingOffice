using CIT.Plateforme.Application.Agent;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Agent;

/// <summary>
/// [SÉCURITÉ — CONSOLIDATION] LE PÉRIMÈTRE DE FILES SE DÉCIDE CÔTÉ SERVEUR.
///
/// <c>ObtenirDashboardAgentQuery</c> déterminait seule les files visibles d'un agent, à partir de
/// ses habilitations, sans jamais vérifier que le compte existait ni qu'il était actif. Sa seule
/// protection était la garde <c>EstAuthentifie</c> de <c>DashboardAgent.razor</c> — une garde
/// d'interface, donc jamais une frontière de sécurité.
///
/// Ces tests n'ouvrent aucune page : ils appellent la requête directement, ce que ferait n'importe
/// quel autre appelant.
/// </summary>
public sealed class GardeTableauDeBordAgentTests
{
    private static (PlateformeDbContext Db, ObtenirDashboardAgentQuery Query) Construire()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var query = new ObtenirDashboardAgentQuery(
            new FileDeTraitementRepository(db),
            new TicketRepository(db),
            new HabilitationRepository(db),
            new DisponibiliteRepository(db),
            new GardeIdentiteService(new UtilisateurInfoRepository(db)));
        return (db, query);
    }

    private static Guid AjouterCompte(PlateformeDbContext db, string cle, NatureUtilisateur nature, bool actif)
    {
        var compte = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"{cle}@dashboard.test",
            Email = $"{cle}@dashboard.test",
            NomAffiche = cle,
            Nature = nature,
            EstActif = actif,
        };
        db.Users.Add(compte);
        db.SaveChanges();
        return compte.Id;
    }

    private static (Guid FileA, Guid FileB) DeuxFiles(PlateformeDbContext db)
    {
        var fileA = FileDeTraitement.Creer("IMPORT", "File A");
        var fileB = FileDeTraitement.Creer("IMPORT", "File B");
        db.Files.AddRange(fileA, fileB);

        // Un ticket en attente dans chaque file, pour que les compteurs aient de quoi bouger.
        var dossier = Dossier.Creer("DOS-DASH-0001", Guid.NewGuid(), DateTime.UtcNow);
        db.Dossiers.Add(dossier);
        db.Tickets.Add(Ticket.Creer("TCK-DASH-0001", "IMPORT", "STANDARD", fileA.Id, dossier.Id, DateTime.UtcNow, "BL-A"));
        db.Tickets.Add(Ticket.Creer("TCK-DASH-0002", "IMPORT", "STANDARD", fileB.Id, dossier.Id, DateTime.UtcNow, "BL-B"));
        db.SaveChanges();
        return (fileA.Id, fileB.Id);
    }

    /// <summary>Un agent habilité sur la file A la voit — témoin sans lequel les refus ne prouvent rien.</summary>
    [Fact]
    public async Task Agent_habilite_sur_la_file_A_la_voit()
    {
        var (db, query) = Construire();
        using var _ = db;
        var (fileA, _) = DeuxFiles(db);
        var agent = AjouterCompte(db, "agent-a", NatureUtilisateur.Agent, actif: true);
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(agent, Permission.TicketPrendreEnCharge, fileA));
        db.SaveChanges();

        var vue = await query.ExecuterAsync(agent);

        Assert.Single(vue.FilesHabilitees);
        Assert.Equal(fileA, vue.FilesHabilitees[0].FileId);
    }

    /// <summary>Il ne voit pas la file B, sur laquelle il n'est pas habilité (SEC-03).</summary>
    [Fact]
    public async Task Agent_non_habilite_sur_la_file_B_ne_la_voit_pas()
    {
        var (db, query) = Construire();
        using var _ = db;
        var (fileA, fileB) = DeuxFiles(db);
        var agent = AjouterCompte(db, "agent-a2", NatureUtilisateur.Agent, actif: true);
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(agent, Permission.TicketPrendreEnCharge, fileA));
        db.SaveChanges();

        var vue = await query.ExecuterAsync(agent);

        Assert.DoesNotContain(vue.FilesHabilitees, f => f.FileId == fileB);
    }

    /// <summary>Un agent sans aucune habilitation n'obtient aucune file.</summary>
    [Fact]
    public async Task Agent_sans_habilitation_n_obtient_aucune_file()
    {
        var (db, query) = Construire();
        using var _ = db;
        DeuxFiles(db);
        var agent = AjouterCompte(db, "agent-sans-droit", NatureUtilisateur.Agent, actif: true);

        var vue = await query.ExecuterAsync(agent);

        Assert.Empty(vue.FilesHabilitees);
    }

    /// <summary>
    /// Un agent DÉSACTIVÉ n'obtient rien — pas même son nom. C'est le cas que la garde de page
    /// couvrait jusqu'ici, et que la requête ignorait.
    /// </summary>
    [Fact]
    public async Task Agent_desactive_n_obtient_aucune_donnee()
    {
        var (db, query) = Construire();
        using var _ = db;
        var (fileA, _) = DeuxFiles(db);
        var agent = AjouterCompte(db, "agent-desactive", NatureUtilisateur.Agent, actif: false);
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(agent, Permission.TicketPrendreEnCharge, fileA));
        db.SaveChanges();

        var vue = await query.ExecuterAsync(agent);

        Assert.Empty(vue.FilesHabilitees);
        Assert.Empty(vue.MesTicketsEnCours);
        Assert.Equal(string.Empty, vue.AgentNom);
        Assert.Equal(0, vue.TotalTicketsEnAttenteDansPerimetre);
        Assert.False(vue.EstDisponible);
    }

    /// <summary>Un acteur d'une autre nature — un client — n'obtient rien non plus.</summary>
    [Fact]
    public async Task Un_client_n_obtient_aucune_donnee_d_agent()
    {
        var (db, query) = Construire();
        using var _ = db;
        var (fileA, _) = DeuxFiles(db);
        var client = AjouterCompte(db, "client", NatureUtilisateur.Client, actif: true);
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(client, Permission.TicketPrendreEnCharge, fileA));
        db.SaveChanges();

        var vue = await query.ExecuterAsync(client);

        Assert.Empty(vue.FilesHabilitees);
        Assert.Equal(string.Empty, vue.AgentNom);
    }

    /// <summary>Un identifiant inconnu n'obtient rien.</summary>
    [Fact]
    public async Task Un_compte_inconnu_n_obtient_aucune_donnee()
    {
        var (db, query) = Construire();
        using var _ = db;
        DeuxFiles(db);

        var vue = await query.ExecuterAsync(Guid.NewGuid());

        Assert.Empty(vue.FilesHabilitees);
        Assert.Empty(vue.MesTicketsEnCours);
    }
}

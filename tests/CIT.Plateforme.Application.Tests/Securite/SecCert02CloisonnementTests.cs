using System;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Securite;

/// <summary>
/// FEATURE 011 — CERTIFICATION SEC-03 · suite TEST-SPEC « SEC-CERT-02 » (T012→T017).
///
/// Texte CDC (SEC-03, Must) : « Cloisonnement strict des données : un client n'accède qu'à ses
/// tickets ; un agent n'accède qu'aux files et actions de son rôle. »
///
/// POURQUOI CETTE SUITE EXISTE : l'audit F011 (research.md D1) a constaté que le cloisonnement
/// est MASSIVEMENT exercé — 55 fichiers de test l'exercent — mais qu'AUCUN ne nomme SEC-03.
/// L'exigence était donc servie sans jamais être certifiée nominativement.
///
/// NON-DUPLICATION (Constitution, Principe IV · tasks.md I5) : cette suite ne réécrit AUCUN des
/// 55 fichiers existants. Elle CONSOMME les services de cloisonnement en place
/// (CloisonnementService, AutorisationService) et démontre les DEUX AXES du texte CDC :
///   - axe client : un client n'accède qu'à SES tickets ;
///   - axe agent  : un agent n'accède qu'aux files et actions de SON rôle.
///
/// AUCUN CODE PRODUIT N'EST MODIFIÉ. Aucun mécanisme de RBAC n'est dupliqué.
///
/// PRINCIPE TRANSVERSE : le refus est prononcé CÔTÉ SERVEUR. Ces tests appellent directement la
/// couche applicative, sans passer par l'interface — un masquage d'écran ne prouverait rien.
/// </summary>
public sealed class SecCert02CloisonnementTests
{
    private const string Module = "Certification";

    private static (PlateformeDbContext Db, CloisonnementService Cloisonnement, AutorisationService Autorisation)
        ConstruireContexte()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        return (
            dbContext,
            new CloisonnementService(new TicketRepository(dbContext), CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAcces(dbContext)),
            new AutorisationService(dbContext));
    }

    private static async Task<Ticket> CreerTicketPourAsync(PlateformeDbContext dbContext, Guid clientId, Guid fileId)
    {
        var dossier = Dossier.Creer($"DOS-{Guid.NewGuid():N}"[..12], CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, clientId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), DateTime.UtcNow);
        dbContext.Dossiers.Add(dossier);

        // Ordre du domaine : (numero, module, typeDemande, fileCibleId, dossierId, maintenantUtc).
        var ticket = Ticket.Creer($"TCK-{Guid.NewGuid():N}"[..12], Module, Module, fileId, dossier.Id, DateTime.UtcNow);
        dbContext.Tickets.Add(ticket);

        await dbContext.SaveChangesAsync();
        return ticket;
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // AXE CLIENT — « un client n'accède qu'à ses tickets »
    // ══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Un_client_accede_a_son_propre_ticket()
    {
        var (dbContext, cloisonnement, _) = ConstruireContexte();
        await using var _db = dbContext;

        var clientA = Guid.NewGuid();
        var ticket = await CreerTicketPourAsync(dbContext, clientA, Guid.NewGuid());

        var exception = await Record.ExceptionAsync(() => cloisonnement.ExigerProprieteClientAsync(clientA, ticket.Id));

        Assert.Null(exception);
    }

    /// <summary>
    /// T013 — ACCÈS DIRECT PAR IDENTIFIANT CONNU (anti-IDOR). Le client A connaît l'identifiant
    /// exact du ticket de B et le demande explicitement : le refus doit venir du serveur, et non
    /// d'un filtrage d'affichage.
    /// </summary>
    [Fact]
    public async Task Un_client_ne_peut_pas_acceder_au_ticket_d_un_autre_client_meme_en_connaissant_son_identifiant()
    {
        var (dbContext, cloisonnement, _) = ConstruireContexte();
        await using var _db = dbContext;

        var clientA = Guid.NewGuid();
        var clientB = Guid.NewGuid();
        var ticketDeB = await CreerTicketPourAsync(dbContext, clientB, Guid.NewGuid());

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => cloisonnement.ExigerProprieteClientAsync(clientA, ticketDeB.Id));
    }

    /// <summary>
    /// Le refus ne doit RIEN divulguer : demander un ticket inexistant et le ticket d'autrui
    /// produit le même type de refus, de sorte que l'appelant ne peut pas déduire l'existence
    /// d'une ressource qui ne lui appartient pas.
    /// </summary>
    [Fact]
    public async Task Le_refus_ne_divulgue_pas_l_existence_du_ticket()
    {
        var (dbContext, cloisonnement, _) = ConstruireContexte();
        await using var _db = dbContext;

        var clientA = Guid.NewGuid();
        var ticketDAutrui = await CreerTicketPourAsync(dbContext, Guid.NewGuid(), Guid.NewGuid());

        var refusAutrui = await Assert.ThrowsAsync<AccesRefuseException>(
            () => cloisonnement.ExigerProprieteClientAsync(clientA, ticketDAutrui.Id));
        var refusInexistant = await Assert.ThrowsAsync<AccesRefuseException>(
            () => cloisonnement.ExigerProprieteClientAsync(clientA, Guid.NewGuid()));

        Assert.Equal(refusInexistant.GetType(), refusAutrui.GetType());
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // AXE AGENT — « un agent n'accède qu'aux files et actions de son rôle »
    // ══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Un_agent_habilite_agit_dans_sa_propre_file()
    {
        var (dbContext, _, autorisation) = ConstruireContexte();
        await using var _db = dbContext;

        var file = FileDeTraitement.Creer(Module, "File de certification");
        dbContext.Files.Add(file);

        var agent = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agent), Permission.TicketPrendreEnCharge, file.Id));
        await dbContext.SaveChangesAsync();

        var exception = await Record.ExceptionAsync(
            () => autorisation.ExigerAsync(agent, Permission.TicketPrendreEnCharge, file.Id));

        Assert.Null(exception);
    }

    /// <summary>
    /// T015 — AGENT HORS DE SON PÉRIMÈTRE. L'agent détient la permission, mais pour une AUTRE
    /// file. Le CDC exige qu'il n'accède qu'aux files de son rôle : la portée doit être vérifiée,
    /// pas seulement la permission.
    /// </summary>
    [Fact]
    public async Task Un_agent_ne_peut_pas_agir_sur_une_file_hors_de_son_perimetre()
    {
        var (dbContext, _, autorisation) = ConstruireContexte();
        await using var _db = dbContext;

        var fileHabilitee = FileDeTraitement.Creer(Module, "File habilitée");
        var fileEtrangere = FileDeTraitement.Creer(Module, "File étrangère");
        dbContext.Files.AddRange(fileHabilitee, fileEtrangere);

        var agent = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agent), Permission.TicketPrendreEnCharge, fileHabilitee.Id));
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => autorisation.ExigerAsync(agent, Permission.TicketPrendreEnCharge, fileEtrangere.Id));
    }

    /// <summary>
    /// T015 (suite) — ACTION HORS DU RÔLE. L'agent est habilité sur la file, mais pour une autre
    /// action. Le CDC exige qu'il n'accède qu'aux ACTIONS de son rôle.
    /// </summary>
    [Fact]
    public async Task Un_agent_ne_peut_pas_exercer_une_action_hors_de_son_role()
    {
        var (dbContext, _, autorisation) = ConstruireContexte();
        await using var _db = dbContext;

        var file = FileDeTraitement.Creer(Module, "File de certification");
        dbContext.Files.Add(file);

        var agent = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agent), Permission.TicketPrendreEnCharge, file.Id));
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => autorisation.ExigerAsync(agent, Permission.TicketCloturer, file.Id));
    }

    /// <summary>
    /// T016 — CONTRÔLE SERVEUR. Ces vérifications sont prononcées par la couche applicative,
    /// appelée directement : aucune interface n'intervient. Une protection qui ne vivrait que
    /// dans l'affichage ne satisferait pas SEC-03.
    /// </summary>
    [Fact]
    public async Task Le_controle_est_prononce_par_la_couche_applicative_sans_interface()
    {
        var (dbContext, cloisonnement, autorisation) = ConstruireContexte();
        await using var _db = dbContext;

        var ticketDAutrui = await CreerTicketPourAsync(dbContext, Guid.NewGuid(), Guid.NewGuid());

        // Aucun composant d'interface n'est instancié : le refus vient du serveur seul.
        await Assert.ThrowsAsync<AccesRefuseException>(
            () => cloisonnement.ExigerProprieteClientAsync(Guid.NewGuid(), ticketDAutrui.Id));
        await Assert.ThrowsAsync<AccesRefuseException>(
            () => autorisation.ExigerAsync(Guid.NewGuid(), Permission.TicketCloturer, Guid.NewGuid()));
    }
}

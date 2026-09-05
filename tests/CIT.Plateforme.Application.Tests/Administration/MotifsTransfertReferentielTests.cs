using CIT.Plateforme.Application.Administration;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Administration;

/// <summary>
/// [TEST-SPEC] § 9.10 / § 17.3 — « les motifs de transfert » figurent parmi les référentiels
/// administrables sans développement. TRF-03 — le motif est obligatoire.
///
/// DÉFAUT CORRIGÉ : le motif était obligatoire mais saisi en texte libre. Deux opérateurs décrivant
/// la même situation produisaient deux libellés différents, et aucune analyse des causes de mauvaise
/// orientation n'était possible.
///
/// LES DEUX TESTS QUI PORTENT LE PLUS : le référentiel vide laisse passer un motif libre — sans quoi
/// la plateforme serait intransférable avant tout paramétrage, et il aurait fallu inventer une
/// nomenclature que le CDC ne donne pas ; et le référentiel peuplé fait autorité — sans quoi
/// l'écran de paramétrage serait décoratif.
/// </summary>
public sealed class MotifsTransfertReferentielTests
{
    private sealed record Contexte(
        TransfererTicketCommand Transferer,
        GererMotifsTransfertCommand Gerer,
        ListerMotifsTransfertQuery Lister,
        Guid AgentId,
        Guid TicketId,
        Guid FileCible);

    private static async Task<Contexte> PreparerAsync(PlateformeDbContext dbContext, params string[] permissions)
    {
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var agentId = Guid.NewGuid();
        var fileSource = Guid.NewGuid();
        var fileCible = Guid.NewGuid();

        // L'administration d'un référentiel est GLOBALE — elle ne se rapporte à aucune file en
        // particulier — tandis que le transfert s'habilite file par file. Attribuer une permission
        // d'administration au périmètre d'une seule file ne la satisfait donc pas, et c'est le
        // comportement voulu : le paramétrage vaut pour toute la plateforme.
        foreach (var permission in permissions)
        {
            var portee = permission.StartsWith("administration.", StringComparison.Ordinal)
                ? (Guid?)null
                : fileSource;
            dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), permission, portee));
        }

        dbContext.TransitionsAutorisees.Add(TransitionAutorisee.Definir(fileSource, fileCible, []));

        var suffixe = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var dossier = Dossier.Creer("DOS-MTF-" + suffixe, Guid.NewGuid(), horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-MTF-" + suffixe, "BillingOffice", "Import", fileSource, dossier.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        return new Contexte(
            new TransfererTicketCommand(
                new TicketRepository(dbContext), new TransitionAutoriseeRepository(dbContext),
                new MotifTransfertRepository(dbContext), autorisation, journalisation, horloge, uniteDeTravail),
            new GererMotifsTransfertCommand(
                new MotifTransfertRepository(dbContext), autorisation, journalisation, uniteDeTravail),
            new ListerMotifsTransfertQuery(new MotifTransfertRepository(dbContext), autorisation),
            agentId, ticket.Id, fileCible);
    }

    /// <summary>
    /// Tant que le référentiel est vide, un motif libre reste recevable. C'est ce qui permet de
    /// livrer la plateforme sans avoir inventé une nomenclature absente du CDC.
    /// </summary>
    [Fact]
    public async Task Referentiel_vide_un_motif_libre_est_accepte()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext, Permission.TicketTransferer);

        await c.Transferer.ExecuterAsync(c.AgentId, c.TicketId, c.FileCible, "orientation initiale erronee");

        var ticket = await dbContext.Tickets.FindAsync(c.TicketId);
        Assert.Equal(c.FileCible, ticket!.FileCouranteId);
    }

    /// <summary>
    /// LA PROPRIÉTÉ DÉCISIVE : dès qu'un motif est paramétré, le référentiel fait autorité.
    /// </summary>
    [Fact]
    public async Task Referentiel_peuple_un_motif_hors_referentiel_est_refuse()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext, Permission.TicketTransferer, Permission.AdministrationFiles);
        await c.Gerer.DefinirAsync(c.AgentId, "mauvaise_orientation", "Ticket mal oriente", 10);

        await Assert.ThrowsAsync<MotifTransfertInconnuException>(() =>
            c.Transferer.ExecuterAsync(c.AgentId, c.TicketId, c.FileCible, "je fais comme je veux"));

        var ticket = await dbContext.Tickets.FindAsync(c.TicketId);
        Assert.NotEqual(c.FileCible, ticket!.FileCouranteId);
    }

    [Fact]
    public async Task Referentiel_peuple_un_motif_du_referentiel_est_accepte()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext, Permission.TicketTransferer, Permission.AdministrationFiles);
        await c.Gerer.DefinirAsync(c.AgentId, "mauvaise_orientation", "Ticket mal oriente", 10);

        await c.Transferer.ExecuterAsync(c.AgentId, c.TicketId, c.FileCible, "mauvaise_orientation");

        var ticket = await dbContext.Tickets.FindAsync(c.TicketId);
        Assert.Equal(c.FileCible, ticket!.FileCouranteId);
    }

    /// <summary>
    /// Un motif désactivé cesse d'être proposé ET d'être accepté : c'est la différence entre
    /// désactiver et se contenter de masquer.
    /// </summary>
    [Fact]
    public async Task Un_motif_desactive_n_est_plus_accepte()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext, Permission.TicketTransferer, Permission.AdministrationFiles);
        await c.Gerer.DefinirAsync(c.AgentId, "encore_valable", "Encore valable", 10);
        var retire = await c.Gerer.DefinirAsync(c.AgentId, "obsolete", "Obsolete", 20);

        await c.Gerer.DefinirActivationAsync(c.AgentId, retire, actif: false);

        await Assert.ThrowsAsync<MotifTransfertInconnuException>(() =>
            c.Transferer.ExecuterAsync(c.AgentId, c.TicketId, c.FileCible, "obsolete"));
    }

    /// <summary>
    /// Le référentiel entièrement désactivé revient au comportement du référentiel vide : la
    /// plateforme ne devient pas intransférable parce qu'un administrateur a tout retiré.
    /// </summary>
    [Fact]
    public async Task Referentiel_entierement_desactive_le_motif_libre_redevient_recevable()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext, Permission.TicketTransferer, Permission.AdministrationFiles);
        var seul = await c.Gerer.DefinirAsync(c.AgentId, "obsolete", "Obsolete", 10);
        await c.Gerer.DefinirActivationAsync(c.AgentId, seul, actif: false);

        await c.Transferer.ExecuterAsync(c.AgentId, c.TicketId, c.FileCible, "motif circonstancie");

        var ticket = await dbContext.Tickets.FindAsync(c.TicketId);
        Assert.Equal(c.FileCible, ticket!.FileCouranteId);
    }

    /// <summary>TRF-03 — le référentiel ne dispense jamais de motif.</summary>
    [Fact]
    public async Task Le_motif_reste_obligatoire()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext, Permission.TicketTransferer);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            c.Transferer.ExecuterAsync(c.AgentId, c.TicketId, c.FileCible, "   "));
    }

    [Fact]
    public async Task Un_code_en_doublon_est_refuse()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext, Permission.AdministrationFiles);
        await c.Gerer.DefinirAsync(c.AgentId, "mauvaise_orientation", "Ticket mal oriente", 10);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            c.Gerer.DefinirAsync(c.AgentId, "mauvaise_orientation", "Autre formulation", 20));
    }

    [Fact]
    public async Task Administrer_le_referentiel_exige_la_permission_administration_files()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext, Permission.TicketTransferer);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            c.Gerer.DefinirAsync(c.AgentId, "code", "Libelle", 0));
    }

    /// <summary>
    /// L'écran de paramétrage voit les motifs désactivés — sinon ils seraient irrécupérables ;
    /// l'opérateur qui transfère ne voit que ceux en vigueur.
    /// </summary>
    [Fact]
    public async Task L_ecran_de_parametrage_voit_les_desactives_l_operateur_non()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext, Permission.TicketTransferer, Permission.AdministrationFiles);

        // Lister les motifs est une capacité GLOBALE : l'opérateur n'a pas encore désigné de
        // ticket, donc aucune file ne borne la question. Le transfert lui-même reste contrôlé sur
        // la file du ticket. Même distinction que ListerFilesQuery.ListerPourTransfertAsync.
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, c.AgentId), Permission.TicketTransferer));
        await dbContext.SaveChangesAsync();

        await c.Gerer.DefinirAsync(c.AgentId, "en_vigueur", "En vigueur", 10);
        var retire = await c.Gerer.DefinirAsync(c.AgentId, "retire", "Retire", 20);
        await c.Gerer.DefinirActivationAsync(c.AgentId, retire, actif: false);

        var vueAdmin = await c.Lister.ExecuterAsync(c.AgentId);
        var vueOperateur = await c.Lister.ListerPourTransfertAsync(c.AgentId);

        Assert.Equal(2, vueAdmin.Count);
        Assert.Single(vueOperateur);
        Assert.Equal("en_vigueur", vueOperateur[0].Code);
    }

    /// <summary>
    /// Reformuler un motif n'invalide pas les transferts déjà tracés : ils portent le code, non le
    /// libellé.
    /// </summary>
    [Fact]
    public async Task Reformuler_un_motif_n_invalide_pas_les_transferts_deja_traces()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext, Permission.TicketTransferer, Permission.AdministrationFiles);
        var id = await c.Gerer.DefinirAsync(c.AgentId, "mauvaise_orientation", "Ticket mal oriente", 10);
        await c.Transferer.ExecuterAsync(c.AgentId, c.TicketId, c.FileCible, "mauvaise_orientation");

        await c.Gerer.ModifierAsync(c.AgentId, id, "Erreur d'orientation initiale", 15);

        var historique = dbContext.HistoriqueTickets
            .Where(h => h.TicketId == c.TicketId && h.Evenement == "TicketTransfere")
            .ToList();

        Assert.Single(historique);
        Assert.Contains("motif=mauvaise_orientation", historique[0].Detail);
    }

    /// <summary>
    /// Le paramétrage est tracé au journal d'audit : § 9.10 rend le référentiel administrable, et
    /// toute modification de paramétrage doit rester imputable.
    /// </summary>
    [Fact]
    public async Task Le_parametrage_est_trace_au_journal_d_audit()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext, Permission.AdministrationFiles);

        var id = await c.Gerer.DefinirAsync(c.AgentId, "mauvaise_orientation", "Ticket mal oriente", 10);
        await c.Gerer.DefinirActivationAsync(c.AgentId, id, actif: false);

        var actions = dbContext.JournalAudit
            .Where(a => a.EntiteId == id)
            .Select(a => a.Action)
            .ToList();

        Assert.Contains("MotifTransfertDefini", actions);
        Assert.Contains("MotifTransfertDesactive", actions);
    }
}

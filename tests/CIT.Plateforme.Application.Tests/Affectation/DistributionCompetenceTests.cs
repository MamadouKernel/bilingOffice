using CIT.Plateforme.Application.Administration;
using CIT.Plateforme.Application.Affectation;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Affectation;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Affectation;

/// <summary>
/// REMÉDIATION AUDIT (post-implementation-audit.md §3.C — AFF-06 COMPÉTENCE, point 4 de la
/// demande de remédiation) : avant correction, AFF-06 n'était exploitable de bout en bout dans
/// aucun cas — <c>DistributionAutomatiqueUseCase.ChoisirStrategie</c> n'avait pas de branche pour
/// <see cref="StrategieAffectation.Competence"/>, et <c>CandidatsAffectationProvider</c>
/// construisait chaque candidat avec une liste de compétences vide en dur. Ces tests exercent le
/// mécanisme complet (file → compétence requise administrée sur la file, opérateur → compétences
/// administrées, distribution automatique), pas seulement <c>CompetenceStrategy</c> isolément
/// (déjà testée par <c>DisponibiliteExclusionTests</c>). Le code de compétence ("Douane") est une
/// valeur de test explicite — aucun catalogue métier réel n'est inventé.
/// </summary>
public sealed class DistributionCompetenceTests
{
    private const string CompetenceDeTest = "Douane";

    private sealed record Contexte(
        PlateformeDbContext DbContext,
        DistributionAutomatiqueUseCase Distribution,
        GererFilesCommand GererFiles,
        GererCompetencesOperateurCommand GererCompetences,
        GererDisponibiliteCommand GererDisponibilite,
        Guid AdministrateurId,
        Guid FileId);

    private static async Task<Contexte> ConstruireContexteAsync(StrategieAffectation strategie)
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);
        var filesRepo = new FileDeTraitementRepository(dbContext);
        var disponibiliteRepo = new DisponibiliteRepository(dbContext);
        var competencesRepo = new CompetenceOperateurRepository(dbContext);
        var candidatsProvider = new CandidatsAffectationProvider(dbContext);

        var administrateurId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, administrateurId), Permission.AdministrationFiles));
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, administrateurId), Permission.CompetenceGerer));
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, administrateurId), Permission.DisponibiliteGerer));
        await dbContext.SaveChangesAsync();

        var gererFiles = new GererFilesCommand(filesRepo, autorisation, journalisation, uniteDeTravail);
        var fileId = await gererFiles.CreerAsync(new CreerFileRequete(administrateurId, "BillingOffice", "FileTest", StrategieAffectation.PriseLibre));
        var competenceRequise = strategie == StrategieAffectation.Competence ? CompetenceDeTest : null;
        await gererFiles.ChangerStrategieAsync(administrateurId, fileId, strategie, competenceRequise);

        var gererCompetences = new GererCompetencesOperateurCommand(competencesRepo, autorisation, journalisation, uniteDeTravail);
        var gererDisponibilite = new GererDisponibiliteCommand(disponibiliteRepo, autorisation, journalisation, horloge, uniteDeTravail);

        var distribution = new DistributionAutomatiqueUseCase(ticketRepo, filesRepo, candidatsProvider, journalisation, horloge, uniteDeTravail);

        return new Contexte(dbContext, distribution, gererFiles, gererCompetences, gererDisponibilite, administrateurId, fileId);
    }

    private static async Task<Guid> CreerTicketEnAttenteAsync(PlateformeDbContext dbContext, Guid fileId, HorlogeDeTest horloge)
    {
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var numerotation = new NumerotationEnMemoirePourTests();
        var creer = new CreerTicketCommand(new TicketRepository(dbContext), new DossierRepository(dbContext), numerotation, journalisation, horloge, uniteDeTravail);
        var ticket = await creer.ExecuterAsync(new CreerTicketRequete(Guid.NewGuid(), "BillingOffice", "FileTest", fileId, null));
        return ticket.Id;
    }

    [Fact]
    public async Task Candidat_avec_competence_compatible_est_selectionne()
    {
        var ctx = await ConstruireContexteAsync(StrategieAffectation.Competence);
        var horloge = HorlogeDeTest.Defaut();

        var operateurCompetent = Guid.NewGuid();
        ctx.DbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(ctx.DbContext, operateurCompetent), Permission.TicketPrendreEnCharge, ctx.FileId));
        await ctx.DbContext.SaveChangesAsync();
        await ctx.GererCompetences.AttribuerAsync(ctx.AdministrateurId, operateurCompetent, CompetenceDeTest);

        var ticketId = await CreerTicketEnAttenteAsync(ctx.DbContext, ctx.FileId, horloge);

        var affecte = await ctx.Distribution.TraiterTicketAsync(ticketId);

        Assert.True(affecte);
        var ticketFinal = await ctx.DbContext.Tickets.FindAsync(ticketId);
        Assert.Equal(operateurCompetent, ticketFinal!.OperateurAssigneId);
    }

    [Fact]
    public async Task Candidat_sans_competence_requise_n_est_jamais_selectionne()
    {
        var ctx = await ConstruireContexteAsync(StrategieAffectation.Competence);
        var horloge = HorlogeDeTest.Defaut();

        // Habilité sur la file, mais AUCUNE compétence administrée pour cet opérateur.
        var operateurSansCompetence = Guid.NewGuid();
        ctx.DbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(ctx.DbContext, operateurSansCompetence), Permission.TicketPrendreEnCharge, ctx.FileId));
        await ctx.DbContext.SaveChangesAsync();

        var ticketId = await CreerTicketEnAttenteAsync(ctx.DbContext, ctx.FileId, horloge);

        var affecte = await ctx.Distribution.TraiterTicketAsync(ticketId);

        Assert.False(affecte);
        var ticketFinal = await ctx.DbContext.Tickets.FindAsync(ticketId);
        Assert.Null(ticketFinal!.OperateurAssigneId);
        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, ticketFinal.Statut);
    }

    [Fact]
    public async Task Agent_competent_mais_indisponible_n_est_jamais_selectionne()
    {
        var ctx = await ConstruireContexteAsync(StrategieAffectation.Competence);
        var horloge = HorlogeDeTest.Defaut();

        var operateurIndisponible = Guid.NewGuid();
        ctx.DbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(ctx.DbContext, operateurIndisponible), Permission.TicketPrendreEnCharge, ctx.FileId));
        await ctx.DbContext.SaveChangesAsync();
        await ctx.GererCompetences.AttribuerAsync(ctx.AdministrateurId, operateurIndisponible, CompetenceDeTest);
        await ctx.GererDisponibilite.DefinirAsync(ctx.AdministrateurId, operateurIndisponible, StatutDisponibilite.Indisponible);

        var ticketId = await CreerTicketEnAttenteAsync(ctx.DbContext, ctx.FileId, horloge);

        var affecte = await ctx.Distribution.TraiterTicketAsync(ticketId);

        Assert.False(affecte); // DIS-01 : jamais d'affectation automatique d'un Indisponible, même compétent.
    }

    [Fact]
    public async Task Une_strategie_differente_n_est_jamais_impactee_par_les_donnees_de_competence()
    {
        // La file utilise RoundRobin (pas Competence) ; l'opérateur n'a AUCUNE compétence
        // administrée — la distribution doit fonctionner normalement, sans lien avec AFF-06.
        var ctx = await ConstruireContexteAsync(StrategieAffectation.RoundRobin);
        var horloge = HorlogeDeTest.Defaut();

        var operateur = Guid.NewGuid();
        ctx.DbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(ctx.DbContext, operateur), Permission.TicketPrendreEnCharge, ctx.FileId));
        await ctx.DbContext.SaveChangesAsync();

        var ticketId = await CreerTicketEnAttenteAsync(ctx.DbContext, ctx.FileId, horloge);

        var affecte = await ctx.Distribution.TraiterTicketAsync(ticketId);

        Assert.True(affecte);
        var ticketFinal = await ctx.DbContext.Tickets.FindAsync(ticketId);
        Assert.Equal(operateur, ticketFinal!.OperateurAssigneId);
    }
}

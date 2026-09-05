using CIT.Plateforme.Application.Supervision;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.SuiviTemps;
using CIT.Plateforme.Domain.Supervision;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Supervision;

/// <summary>
/// [TEST-SPEC] ALE-03 / ALE-02 — escalade paramétrable et destinataires administrés.
///
/// DÉFAUT CORRIGÉ (audit du 2026-09-03, <c>docs/88-audit-conformite-cdc.md</c>) : le CDC exige que
/// le dépassement « puisse déclencher une escalade (notification d'un superviseur, remontée de
/// priorité, réaffectation) <b>selon des règles paramétrables</b> », et le § 17.3 liste
/// « escalades » parmi les référentiels administrables. Seule une notification existait, et
/// <c>IResolveurDestinataires</c> déclarait lui-même « ALE-03 reste hors périmètre de
/// raccordement ». La matrice <c>docs/33</c> donnait pourtant ALE-02 et ALE-03 pour certifiées.
///
/// LE TEST LE PLUS IMPORTANT EST LE PREMIER : référentiel vide ⇒ aucune escalade. C'est lui qui
/// garantit qu'on livre le mécanisme sans avoir inventé la politique que le CDC ne donne pas, et
/// que le comportement d'une plateforme non paramétrée reste inchangé.
/// </summary>
public sealed class RegleEscaladeTests
{
    private const string Module = "BillingOffice";
    private const string TypeDemande = "Import";
    private static readonly Guid FileControleBl = Guid.NewGuid();
    private static readonly Guid FileFacturation = Guid.NewGuid();

    /// <summary>Cible d'une heure, seuils informatifs neutralisés, escalade à 150 %.</summary>
    private sealed class ProviderEscalade(double seuilEscalade) : IParametresSlaProvider
    {
        public Task<(ParametresSla Parametres, CalendrierOuvre Calendrier)?> ObtenirPourAsync(Ticket ticket, CancellationToken ct = default) =>
            Task.FromResult<(ParametresSla, CalendrierOuvre)?>(
                (new ParametresSla(TimeSpan.FromHours(1), 1.0, 1.0, seuilEscalade), CalendrierOuvre.ToujoursOuvert));
    }

    private static SurveillerEcheancesUseCase Construire(
        PlateformeDbContext dbContext, HorlogeDeTest horloge, double seuilEscalade = 1.5)
    {
        var journalisation = new JournalisationService(dbContext, horloge);
        return new SurveillerEcheancesUseCase(
            new TicketRepository(dbContext),
            new PeriodeAttenteClientRepository(dbContext),
            new AlerteRepository(dbContext),
            new ProviderEscalade(seuilEscalade),
            new RegleEscaladeRepository(dbContext),
            new PerimetreSupervisionRepository(dbContext),
            journalisation,
            horloge,
            new UniteDeTravail(dbContext));
    }

    /// <summary>Ticket arrivé en file il y a deux heures : au-delà du seuil d'escalade (1,5 h).</summary>
    private static async Task<(PlateformeDbContext Db, HorlogeDeTest Horloge, Ticket Ticket)> ScenarioAsync(
        Guid? fileId = null)
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var arrivee = horloge.MaintenantUtc.AddHours(-2);

        var dossier = Dossier.Creer("DOS-ESCAL-000001", Guid.NewGuid(), arrivee);
        var ticket = Ticket.Creer("TCK-ESCAL-000001", Module, TypeDemande, fileId ?? FileControleBl, dossier.Id, arrivee);
        ticket.AffecterManuellement(Guid.NewGuid(), arrivee);

        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();

        return (dbContext, horloge, ticket);
    }

    [Fact]
    public async Task Sans_regle_administree_aucune_escalade_n_est_appliquee()
    {
        // LA PROPRIÉTÉ DÉCISIVE : le référentiel est livré vide, et une plateforme non paramétrée
        // se comporte exactement comme avant — le dépassement est signalé, rien n'est escaladé.
        var (db, horloge, ticket) = await ScenarioAsync();
        await using var _ = db;
        var operateurAvant = ticket.OperateurAssigneId;

        var statut = await Construire(db, horloge).TraiterTicketAsync(ticket.Id);

        Assert.Equal(StatutSla.Escalade, statut);
        var relu = await db.Tickets.AsNoTracking().FirstAsync(t => t.Id == ticket.Id);
        Assert.Equal(operateurAvant, relu.OperateurAssigneId);
        Assert.Null(relu.PrioriteId);
    }

    [Fact]
    public async Task Sans_seuil_d_escalade_administre_le_statut_reste_depasse()
    {
        // SLA-02 : sans seuil administré, on ne devine pas l'instant de l'escalade.
        var (db, horloge, ticket) = await ScenarioAsync();
        await using var _ = db;
        db.ReglesEscalade.Add(RegleEscalade.Definir(ActionEscalade.Reaffecter));
        await db.SaveChangesAsync();

        var statut = await Construire(db, horloge, seuilEscalade: 0).TraiterTicketAsync(ticket.Id);

        Assert.Equal(StatutSla.Depasse, statut);
        var relu = await db.Tickets.AsNoTracking().FirstAsync(t => t.Id == ticket.Id);
        Assert.NotNull(relu.OperateurAssigneId);
    }

    [Fact]
    public async Task La_remontee_de_priorite_applique_la_priorite_cible()
    {
        var (db, horloge, ticket) = await ScenarioAsync();
        await using var _ = db;
        var prioriteHaute = Guid.NewGuid();
        db.ReglesEscalade.Add(RegleEscalade.Definir(ActionEscalade.RemonterPriorite, prioriteCibleId: prioriteHaute));
        await db.SaveChangesAsync();

        await Construire(db, horloge).TraiterTicketAsync(ticket.Id);

        var relu = await db.Tickets.AsNoTracking().FirstAsync(t => t.Id == ticket.Id);
        Assert.Equal(prioriteHaute, relu.PrioriteId);
    }

    [Fact]
    public async Task La_reaffectation_remet_le_ticket_en_distribution()
    {
        var (db, horloge, ticket) = await ScenarioAsync();
        await using var _ = db;
        db.ReglesEscalade.Add(RegleEscalade.Definir(ActionEscalade.Reaffecter));
        await db.SaveChangesAsync();

        await Construire(db, horloge).TraiterTicketAsync(ticket.Id);

        var relu = await db.Tickets.AsNoTracking().FirstAsync(t => t.Id == ticket.Id);
        Assert.Null(relu.OperateurAssigneId);
        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, relu.Statut);
    }

    [Fact]
    public async Task Une_regle_bornee_a_une_autre_file_ne_s_applique_pas()
    {
        var (db, horloge, ticket) = await ScenarioAsync(fileId: FileControleBl);
        await using var _ = db;
        db.ReglesEscalade.Add(RegleEscalade.Definir(ActionEscalade.Reaffecter, fileId: FileFacturation));
        await db.SaveChangesAsync();

        await Construire(db, horloge).TraiterTicketAsync(ticket.Id);

        var relu = await db.Tickets.AsNoTracking().FirstAsync(t => t.Id == ticket.Id);
        Assert.NotNull(relu.OperateurAssigneId);
    }

    [Fact]
    public async Task Une_regle_desactivee_ne_s_applique_pas()
    {
        var (db, horloge, ticket) = await ScenarioAsync();
        await using var _ = db;
        var regle = RegleEscalade.Definir(ActionEscalade.Reaffecter);
        regle.Desactiver();
        db.ReglesEscalade.Add(regle);
        await db.SaveChangesAsync();

        await Construire(db, horloge).TraiterTicketAsync(ticket.Id);

        var relu = await db.Tickets.AsNoTracking().FirstAsync(t => t.Id == ticket.Id);
        Assert.NotNull(relu.OperateurAssigneId);
    }

    [Fact]
    public void Une_remontee_de_priorite_sans_cible_est_refusee()
    {
        // Le socle n'a aucune priorité par défaut à substituer (PC-12) : une règle incomplète
        // doit être refusée à la définition, pas ignorée silencieusement à l'exécution.
        Assert.Throws<ArgumentException>(() => RegleEscalade.Definir(ActionEscalade.RemonterPriorite));
    }

    [Fact]
    public async Task Seuls_les_superviseurs_dont_le_perimetre_couvre_la_file_sont_notifies()
    {
        // ALE-02 : le destinataire se déduit des périmètres ADMINISTRÉS. En ajouter un est un acte
        // d'administration, jamais un développement.
        var (db, horloge, ticket) = await ScenarioAsync(fileId: FileControleBl);
        await using var _ = db;

        var superviseurConcerne = Guid.NewGuid();
        var superviseurAilleurs = Guid.NewGuid();
        db.PerimetresSupervision.Add(PerimetreSupervision.Definir(superviseurConcerne, [FileControleBl]));
        db.PerimetresSupervision.Add(PerimetreSupervision.Definir(superviseurAilleurs, [FileFacturation]));
        db.ReglesEscalade.Add(RegleEscalade.Definir(ActionEscalade.NotifierSuperviseur));
        await db.SaveChangesAsync();

        await Construire(db, horloge).TraiterTicketAsync(ticket.Id);

        // Deux événements portent ce type : celui de l'alerte (ticket seul) et celui de la
        // notification d'escalade (avec ses destinataires). C'est le second qui nous intéresse.
        var evenement = await db.JournalEvenements.AsNoTracking()
            .FirstAsync(e => e.TypeEvenement == TypeAlerte.SlaEscalade.ToString()
                          && e.ChargeUtile.Contains("superviseurs"));

        Assert.Contains(superviseurConcerne.ToString(), evenement.ChargeUtile);
        Assert.DoesNotContain(superviseurAilleurs.ToString(), evenement.ChargeUtile);
    }
}

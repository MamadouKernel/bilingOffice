using CIT.Plateforme.Application.Supervision;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.SuiviTemps;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Supervision;

/// <summary>
/// REMÉDIATION AUDIT (post-implementation-audit.md §3.B — demande de remédiation, point 1) :
/// démontre que <see cref="SurveillerEcheancesUseCase"/> calcule désormais la durée SLA écoulée en
/// ne soustrayant jamais une durée brute à une durée ouvrée — le calendrier ouvré configuré est
/// réellement utilisé (avant correction, <c>ParametresSlaProvider</c> retournait toujours
/// <see cref="CalendrierOuvre.ToujoursOuvert"/>, quelle que soit la configuration ; et l'attente
/// client était déduite en temps brut d'une durée ouvrée).
/// <para>
/// Méthode de mesure : la durée effective n'est pas exposée publiquement par le moteur (contrat
/// volontairement minimal, StatutSla uniquement) ; chaque scénario encadre donc la durée attendue
/// par deux évaluations à ±1 minute près (cible juste en-dessous → Dépassé ; cible juste
/// au-dessus → non dépassé), ce qui revient à vérifier une valeur numérique précise sans exposer
/// de détail d'implémentation. Toutes les valeurs (horaires, dates, cibles) sont des valeurs de
/// test explicites — aucune valeur réelle de PC-12 n'est inventée.
/// </para>
/// </summary>
public sealed class SurveillerEcheancesCalendrierTests
{
    // Calendrier de test : Lundi-Vendredi 08h00-17h00 (9h/jour), aucun jour férié.
    private static readonly CalendrierOuvre CalendrierDeTestOuvre = new(
        HeureDebut: new TimeOnly(8, 0),
        HeureFin: new TimeOnly(17, 0),
        JoursOuvres: new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
        JoursFeries: new HashSet<DateOnly>());

    private sealed class ParametresSlaDeTestProvider(ParametresSla parametres, CalendrierOuvre calendrier) : IParametresSlaProvider
    {
        public Task<(ParametresSla Parametres, CalendrierOuvre Calendrier)?> ObtenirPourAsync(Ticket ticket, CancellationToken ct = default) =>
            Task.FromResult<(ParametresSla, CalendrierOuvre)?>((parametres, calendrier));
    }

    private static async Task<StatutSla?> StatutPourCibleAsync(
        PlateformeDbContext dbContext, Guid ticketId, CalendrierOuvre calendrier, TimeSpan cible, HorlogeDeTest horloge)
    {
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var ticketRepo = new TicketRepository(dbContext);
        var alertesRepo = new AlerteRepository(dbContext);
        var periodesRepo = new PeriodeAttenteClientRepository(dbContext);
        // Seuils à 100% : seul le franchissement de la cible elle-même nous intéresse ici.
        var provider = new ParametresSlaDeTestProvider(new ParametresSla(cible, 1.0, 1.0), calendrier);
        var useCase = new SurveillerEcheancesUseCase(ticketRepo, periodesRepo, alertesRepo, provider, new RegleEscaladeRepository(dbContext), new PerimetreSupervisionRepository(dbContext), journalisation, horloge, uniteDeTravail);

        return await useCase.TraiterTicketAsync(ticketId);
    }

    /// <summary>Encadre la durée effective attendue à ±1 minute : la cible juste en-dessous doit
    /// être Dépassée, la cible juste au-dessus ne doit jamais l'être.</summary>
    private static async Task AssertDureeEffectiveAsync(
        PlateformeDbContext dbContext, Guid ticketId, CalendrierOuvre calendrier, HorlogeDeTest horloge, TimeSpan attendu)
    {
        if (attendu > TimeSpan.FromMinutes(1))
        {
            var statutAvant = await StatutPourCibleAsync(dbContext, ticketId, calendrier, attendu - TimeSpan.FromMinutes(1), horloge);
            Assert.Equal(StatutSla.Depasse, statutAvant);
        }

        var statutApres = await StatutPourCibleAsync(dbContext, ticketId, calendrier, attendu + TimeSpan.FromMinutes(1), horloge);
        Assert.NotEqual(StatutSla.Depasse, statutApres);
    }

    private static async Task<(PlateformeDbContext DbContext, Guid TicketId)> CreerTicketAsync(DateTime arriveeUtc)
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var dossier = Dossier.Creer($"DOS-CAL-{Guid.NewGuid():N}", Guid.NewGuid(), arriveeUtc);
        var ticket = Ticket.Creer($"TCK-CAL-{Guid.NewGuid():N}", "BillingOffice", "Import", Guid.NewGuid(), dossier.Id, arriveeUtc);
        dbContext.Dossiers.Add(dossier);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();
        return (dbContext, ticket.Id);
    }

    [Fact]
    public async Task Calendrier_24_7_compte_la_duree_brute_integrale_y_compris_le_week_end()
    {
        // Vendredi 20h -> Lundi 08h = 60h brutes ; calendrier "toujours ouvert" : tout compte.
        var arrivee = new DateTime(2026, 1, 2, 20, 0, 0, DateTimeKind.Utc); // vendredi
        var (dbContext, ticketId) = await CreerTicketAsync(arrivee);
        var horloge = new HorlogeDeTest(arrivee.AddHours(60)); // lundi 08h

        await AssertDureeEffectiveAsync(dbContext, ticketId, CalendrierOuvre.ToujoursOuvert, horloge, TimeSpan.FromHours(60));
    }

    [Fact]
    public async Task Calendrier_ouvre_sans_chevauchement_ne_compte_que_les_heures_ouvrees()
    {
        // Lundi 08h -> Lundi 12h, entièrement en heures ouvrées, aucune attente client.
        var arrivee = new DateTime(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc); // lundi
        var (dbContext, ticketId) = await CreerTicketAsync(arrivee);
        var horloge = new HorlogeDeTest(arrivee.AddHours(4)); // lundi 12h

        await AssertDureeEffectiveAsync(dbContext, ticketId, CalendrierDeTestOuvre, horloge, TimeSpan.FromHours(4));
    }

    [Fact]
    public async Task Attente_client_entierement_en_heures_ouvrees_est_integralement_deduite()
    {
        // Lundi 08h -> 14h (6h ouvrées), attente client 10h-12h (2h, entièrement ouvrée).
        var arrivee = new DateTime(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc);
        var (dbContext, ticketId) = await CreerTicketAsync(arrivee);
        var debutAttente = arrivee.AddHours(2); // 10h
        var finAttente = arrivee.AddHours(4);   // 12h
        dbContext.PeriodesAttenteClient.Add(PeriodeAttenteClient.Ouvrir(ticketId, debutAttente));
        var periode = dbContext.PeriodesAttenteClient.Local.Single();
        periode.Fermer(finAttente);
        await dbContext.SaveChangesAsync();

        var horloge = new HorlogeDeTest(arrivee.AddHours(6)); // 14h

        // 6h ouvrées écoulées - 2h d'attente (ouvrée) = 4h effectives.
        await AssertDureeEffectiveAsync(dbContext, ticketId, CalendrierDeTestOuvre, horloge, TimeSpan.FromHours(4));
    }

    [Fact]
    public async Task Attente_client_traversant_une_periode_non_ouvree_est_deduite_correctement()
    {
        // Lundi 16h00 (arrivée) ; attente client Lundi 16h30 -> Mardi 09h30 (chevauche la nuit).
        // "Maintenant" = Mardi 09h30 (fin exacte de l'attente).
        //   - durée totale ouvrée (Lun 16h -> Mar 9h30)  = 1h (Lun 16-17h) + 1h30 (Mar 8-9h30) = 2h30
        //   - durée attente ouvrée (Lun 16h30 -> Mar 9h30) = 0h30 (Lun 16h30-17h) + 1h30 (Mar 8-9h30) = 2h
        //   - durée effective attendue = 2h30 - 2h = 0h30 (le temps ouvré AVANT la mise en attente)
        // Une soustraction naïve de la durée BRUTE d'attente (17h) à la durée ouvrée (2h30)
        // produirait un résultat négatif (clampé à 0) — donc 0h30 au lieu de 0h est la preuve
        // directe de la correction.
        var arrivee = new DateTime(2026, 1, 5, 16, 0, 0, DateTimeKind.Utc); // lundi
        var (dbContext, ticketId) = await CreerTicketAsync(arrivee);
        var debutAttente = new DateTime(2026, 1, 5, 16, 30, 0, DateTimeKind.Utc);
        var finAttente = new DateTime(2026, 1, 6, 9, 30, 0, DateTimeKind.Utc); // mardi
        dbContext.PeriodesAttenteClient.Add(PeriodeAttenteClient.Ouvrir(ticketId, debutAttente));
        dbContext.PeriodesAttenteClient.Local.Single().Fermer(finAttente);
        await dbContext.SaveChangesAsync();

        var horloge = new HorlogeDeTest(finAttente);

        await AssertDureeEffectiveAsync(dbContext, ticketId, CalendrierDeTestOuvre, horloge, TimeSpan.FromMinutes(30));
    }

    [Fact]
    public async Task Reprise_du_sla_apres_attente_client_continue_a_accumuler_du_temps_ouvre()
    {
        // Suite du scénario précédent : après la reprise (Mardi 09h30), le temps continue de
        // s'écouler normalement jusqu'à Mardi 11h30 (2h de plus, aucune nouvelle attente).
        // Durée effective attendue = 0h30 (avant attente) + 2h (après reprise) = 2h30.
        var arrivee = new DateTime(2026, 1, 5, 16, 0, 0, DateTimeKind.Utc);
        var (dbContext, ticketId) = await CreerTicketAsync(arrivee);
        var debutAttente = new DateTime(2026, 1, 5, 16, 30, 0, DateTimeKind.Utc);
        var finAttente = new DateTime(2026, 1, 6, 9, 30, 0, DateTimeKind.Utc);
        dbContext.PeriodesAttenteClient.Add(PeriodeAttenteClient.Ouvrir(ticketId, debutAttente));
        dbContext.PeriodesAttenteClient.Local.Single().Fermer(finAttente);
        await dbContext.SaveChangesAsync();

        var horloge = new HorlogeDeTest(finAttente.AddHours(2)); // mardi 11h30, aucune attente supplémentaire

        await AssertDureeEffectiveAsync(dbContext, ticketId, CalendrierDeTestOuvre, horloge, TimeSpan.FromHours(2.5));
    }
}
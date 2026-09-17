using CIT.Plateforme.Application.Administration;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Supervision;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.SuiviTemps;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Tests.Partages;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Supervision;

/// <summary>
/// [TEST-SPEC] Exigences CDC : SUP-01 (« proches d'un dépassement SLA, en dépassement SLA, sans
/// activité depuis une durée définie ») et SUP-02 (« ancienneté du ticket le plus ancien, délai
/// moyen »). Démontre que <see cref="VueFilesQuery"/> expose ces indicateurs en réutilisant le
/// moteur SLA existant (<see cref="SurveillerEcheancesUseCase.DeterminerStatutSlaAsync"/> /
/// <see cref="EcheanceSla"/>), sans réimplémenter le calcul.
/// </summary>
public sealed class SupSup01Sup02IndicateursFileTests
{
    private sealed class ParametresSlaFixesProvider(ParametresSla parametres) : IParametresSlaProvider
    {
        public Task<(ParametresSla Parametres, CalendrierOuvre Calendrier)?> ObtenirPourAsync(Ticket ticket, CancellationToken ct = default) =>
            Task.FromResult<(ParametresSla, CalendrierOuvre)?>((parametres, CalendrierOuvre.ToujoursOuvert));
    }

    private static async Task<(VueFilesQuery Query, Guid SuperviseurId, Guid FileId, HorlogeDeTest Horloge)> PreparerAsync(
        PlateformeDbContext dbContext, ParametresSla parametresSla)
    {
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);

        var superviseurId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            ComptesDeTest.CompteActif(dbContext, superviseurId), Permission.SupervisionConsulter));

        var file = FileDeTraitement.Creer("BillingOffice", "Contrôle BL");
        dbContext.Files.Add(file);
        await dbContext.SaveChangesAsync();

        var gererPerimetres = new GererPerimetresSupervisionCommand(
            new PerimetreSupervisionRepository(dbContext), autorisation, journalisation, uniteDeTravail);
        var administrateurId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            ComptesDeTest.CompteActif(dbContext, administrateurId), Permission.AdministrationPerimetresSupervision));
        await dbContext.SaveChangesAsync();
        await gererPerimetres.DefinirAsync(administrateurId, superviseurId, [file.Id]);

        var surveillerEcheances = new SurveillerEcheancesUseCase(
            new TicketRepository(dbContext), new PeriodeAttenteClientRepository(dbContext), new AlerteRepository(dbContext),
            new ParametresSlaFixesProvider(parametresSla), new RegleEscaladeRepository(dbContext),
            new PerimetreSupervisionRepository(dbContext), journalisation, horloge, uniteDeTravail);

        var vueFiles = new VueFilesQuery(
            new FileDeTraitementRepository(dbContext), new TicketRepository(dbContext),
            new PerimetreSupervisionRepository(dbContext), autorisation, surveillerEcheances, horloge);

        return (vueFiles, superviseurId, file.Id, horloge);
    }

    [Fact]
    public async Task SUP_01_un_ticket_au_dela_du_seuil_cible_apparait_en_depassement()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        // Cible 1h, seuils à 100% : au-delà d'1h → Dépassé.
        var (vueFiles, superviseurId, fileId, horloge) = await PreparerAsync(dbContext, new ParametresSla(TimeSpan.FromHours(1), 1.0, 1.0));

        var dossier = Dossier.Creer("DOS-SUP-01", Guid.NewGuid(), horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        var ticketDepasse = Ticket.Creer("TCK-SUP-01", "BillingImport", "Import", fileId, dossier.Id, horloge.MaintenantUtc.AddHours(-2));
        dbContext.Tickets.Add(ticketDepasse);
        await dbContext.SaveChangesAsync();

        var vues = await vueFiles.ExecuterAsync(superviseurId);
        var vue = Assert.Single(vues);

        Assert.Equal(1, vue.EnDepassementSla);
        Assert.Equal(0, vue.ProcheDepassementSla);
    }

    [Fact]
    public async Task SUP_01_un_ticket_dans_le_seuil_d_alerte_apparait_comme_proche_du_depassement()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        // Cible 1h, alerte à 50% : un ticket vieux de 40 minutes a franchi l'alerte sans dépasser.
        var (vueFiles, superviseurId, fileId, horloge) = await PreparerAsync(dbContext, new ParametresSla(TimeSpan.FromHours(1), 0.25, 0.5));

        var dossier = Dossier.Creer("DOS-SUP-02", Guid.NewGuid(), horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        var ticketProche = Ticket.Creer("TCK-SUP-02", "BillingImport", "Import", fileId, dossier.Id, horloge.MaintenantUtc.AddMinutes(-40));
        dbContext.Tickets.Add(ticketProche);
        await dbContext.SaveChangesAsync();

        var vues = await vueFiles.ExecuterAsync(superviseurId);
        var vue = Assert.Single(vues);

        Assert.Equal(1, vue.ProcheDepassementSla);
        Assert.Equal(0, vue.EnDepassementSla);
    }

    [Fact]
    public async Task SUP_02_ancienneté_du_ticket_le_plus_ancien_et_delai_moyen_sont_corrects_sur_une_fixture_connue()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var (vueFiles, superviseurId, fileId, horloge) = await PreparerAsync(dbContext, new ParametresSla(TimeSpan.FromDays(1), 1.0, 1.0));

        var dossier = Dossier.Creer("DOS-SUP-03", Guid.NewGuid(), horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);

        // Arrivées connues : il y a 4h et il y a 2h → ancienneté attendue 4h, délai moyen attendu 3h.
        var ticketAncien = Ticket.Creer("TCK-SUP-03A", "BillingImport", "Import", fileId, dossier.Id, horloge.MaintenantUtc.AddHours(-4));
        var ticketRecent = Ticket.Creer("TCK-SUP-03B", "BillingImport", "Import", fileId, dossier.Id, horloge.MaintenantUtc.AddHours(-2));
        dbContext.Tickets.AddRange(ticketAncien, ticketRecent);
        await dbContext.SaveChangesAsync();

        var vues = await vueFiles.ExecuterAsync(superviseurId);
        var vue = Assert.Single(vues);

        Assert.NotNull(vue.AncienneteTicketLePlusAncien);
        Assert.NotNull(vue.DelaiMoyenTraitement);
        Assert.InRange(vue.AncienneteTicketLePlusAncien!.Value.TotalMinutes, 239, 241);
        Assert.InRange(vue.DelaiMoyenTraitement!.Value.TotalMinutes, 179, 181);
    }

    [Fact]
    public async Task SUP_01_un_ticket_sans_activite_recente_est_compte_comme_sans_activite()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);

        var superviseurId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            ComptesDeTest.CompteActif(dbContext, superviseurId), Permission.SupervisionConsulter));
        var administrateurId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            ComptesDeTest.CompteActif(dbContext, administrateurId), Permission.AdministrationPerimetresSupervision));

        var file = FileDeTraitement.Creer("BillingOffice", "Contrôle BL");
        dbContext.Files.Add(file);
        await dbContext.SaveChangesAsync();

        var gererPerimetres = new GererPerimetresSupervisionCommand(
            new PerimetreSupervisionRepository(dbContext), autorisation, journalisation, uniteDeTravail);
        await gererPerimetres.DefinirAsync(administrateurId, superviseurId, [file.Id]);

        var surveillerEcheances = new SurveillerEcheancesUseCase(
            new TicketRepository(dbContext), new PeriodeAttenteClientRepository(dbContext), new AlerteRepository(dbContext),
            new ParametresSlaAbsentProvider(), new RegleEscaladeRepository(dbContext),
            new PerimetreSupervisionRepository(dbContext), journalisation, horloge, uniteDeTravail);

        // Seuil « sans activité » réduit à 1h pour le test — la durée réelle relève de
        // l'administration, non testée ici.
        var vueFiles = new VueFilesQuery(
            new FileDeTraitementRepository(dbContext), new TicketRepository(dbContext),
            new PerimetreSupervisionRepository(dbContext), autorisation, surveillerEcheances, horloge,
            seuilSansActivite: TimeSpan.FromHours(1));

        var dossier = Dossier.Creer("DOS-SUP-04", Guid.NewGuid(), horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);
        var ticketInactif = Ticket.Creer("TCK-SUP-04", "BillingImport", "Import", file.Id, dossier.Id, horloge.MaintenantUtc.AddHours(-5));
        dbContext.Tickets.Add(ticketInactif);
        await dbContext.SaveChangesAsync();

        var vues = await vueFiles.ExecuterAsync(superviseurId);
        var vue = Assert.Single(vues);

        Assert.Equal(1, vue.SansActivite);
    }
}

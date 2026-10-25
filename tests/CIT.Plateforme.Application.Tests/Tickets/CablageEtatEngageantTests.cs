using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Infrastructure.Documents;
using Microsoft.Extensions.Configuration;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.BillingExport;
using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Tests.Partages;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tickets;

/// <summary>
/// [P2-04] L'ÉTAT ENGAGEANT EST POSÉ PAR LES COMMANDES, ET IL PRODUIT SES EFFETS.
///
/// POURQUOI CETTE SUITE EXISTE. <c>Ticket.MarquerEtatEngageant()</c> n'avait, à l'origine,
/// <b>aucun appelant en production</b> : le drapeau restait faux à jamais, ce qui privait d'effet
/// la fermeture de l'annulation directe (RG-TCK-07) comme la garde sur la suggestion de clôture
/// pour absence de réponse (RG-TCK-11). La remédiation P1-01 a câblé six commandes.
///
/// MAIS LE CÂBLAGE N'ÉTAIT PROTÉGÉ PAR AUCUN TEST. Les deux seules suites qui manipulaient le
/// drapeau l'appelaient <b>à la main</b> — exactement comme celles qui n'avaient pas vu que la
/// méthode n'avait aucun appelant. Supprimer les six appels laissait la suite entièrement verte.
///
/// Chaque test ci-dessous exécute une COMMANDE RÉELLE, RECHARGE le ticket depuis la base, puis
/// vérifie le drapeau. Aucun n'écrit lui-même l'état qu'il vérifie.
/// </summary>
public sealed class CablageEtatEngageantTests
{
    private sealed record Contexte(
        PlateformeDbContext Db,
        Guid AgentId,
        Guid ClientId,
        Ticket Ticket,
        FileDeTraitement FileCourante);

    private static readonly DateTime Maintenant = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly ReglesDepotDocument Regles = new(["pdf"], TailleMaxOctets: 1024);

    private static Stream Contenu() => new MemoryStream("FACTURE."u8.ToArray());

    private static DeposerDocumentCommand DepotDeTest(
        PlateformeDbContext db, IAutorisationService auto, IJournalisationService journal, IHorloge horloge, IUniteDeTravail uow) =>
        new(new DocumentRepository(db), new TicketRepository(db),
            new StockageDocumentsFichiers(new ConfigurationBuilder().AddInMemoryCollection().Build(), new EnvironnementDeTest("Development")),
            new AucuneAnalyseAntivirusConfiguree(), auto,
            new CloisonnementService(new TicketRepository(db), FabriqueRequetes.ControleAcces(db)),
            journal, horloge, uow);

    /// <summary>
    /// Prépare un ticket du module demandé, posé dans la file d'où part la commande, et un agent
    /// habilité sur cette file.
    /// </summary>
    private static Contexte Preparer(string module, string nomFileCourante, string nomFileCible, string permission)
    {
        var db = ContexteDeTest.CreerNouveauContexte();

        var fileCourante = FileDeTraitement.Creer(module, nomFileCourante);
        var fileCible = FileDeTraitement.Creer(module, nomFileCible);
        db.Files.AddRange(fileCourante, fileCible);

        var societeId = Guid.NewGuid();
        var clientId = ComptesDeTest.CompteActif(db, Guid.NewGuid(), NatureUtilisateur.Client);
        var agentId = ComptesDeTest.CompteActif(db, Guid.NewGuid(), NatureUtilisateur.Agent);
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentId, permission, fileCourante.Id));
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentId, Permission.DocumentDeposer, fileCourante.Id));

        // L'instantané de type client est requis par EditerFacture* pour choisir la file cible (IMP-06).
        var dossier = Dossier.Creer($"DOS-ENG-{Guid.NewGuid():N}"[..14], societeId, Maintenant, clientId,
            typeClient: CIT.Plateforme.Domain.Comptes.TypeClient.Normal);
        var ticket = Ticket.Creer($"TCK-ENG-{Guid.NewGuid():N}"[..14], module, "STANDARD", fileCourante.Id, dossier.Id, Maintenant, "BL-ENG");
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        db.SaveChanges();

        return new Contexte(db, agentId, clientId, ticket, fileCourante);
    }

    private static (IHorloge Horloge, IJournalisationService Journal, IUniteDeTravail Uow, IAutorisationService Auto) Socle(PlateformeDbContext db)
    {
        var horloge = new HorlogeSysteme();
        return (horloge, new JournalisationService(db, horloge), new UniteDeTravail(db), new AutorisationService(db));
    }

    /// <summary>Recharge le ticket depuis la base — jamais l'instance en mémoire.</summary>
    private static async Task<Ticket> RechargerAsync(PlateformeDbContext db, Guid ticketId)
    {
        db.ChangeTracker.Clear();
        return await new TicketRepository(db).ObtenirParIdAsync(ticketId)
            ?? throw new InvalidOperationException("Ticket introuvable après rechargement.");
    }

    // ---------------------------------------------------------------------
    // Les six commandes engageantes
    // ---------------------------------------------------------------------

    /// <summary>Paiement confirmé — IMPORT. RG-TCK-11 : « paiement confirmé ».</summary>
    [Fact]
    public async Task ValiderPreuvePaiement_import_rend_le_ticket_engageant()
    {
        var c = Preparer(SoumettreDemandeImportCommand.Module, NomsFiles.ValidationPaiement, NomsFiles.ControleDocumentaire, Permission.BillingImportPaiementValider);
        using var db = c.Db;
        var (horloge, journal, uow, auto) = Socle(db);

        Assert.False(c.Ticket.EstEnEtatEngageant);

        await PriseEnChargeDeTest.PrendreAsync(db, c.Ticket.Id, c.AgentId);
        await new ValiderPreuvePaiementCommand(new TicketRepository(db), new FileDeTraitementRepository(db), auto, journal, horloge, uow)
            .ValiderAsync(c.AgentId, c.Ticket.Id);

        Assert.True((await RechargerAsync(db, c.Ticket.Id)).EstEnEtatEngageant);
    }

    /// <summary>Lettrage confirmé — IMPORT. RG-TCK-11 : « paiement confirmé ».</summary>
    [Fact]
    public async Task ConfirmerLettrage_import_rend_le_ticket_engageant()
    {
        var c = Preparer(SoumettreDemandeImportCommand.Module, NomsFiles.ComptabiliteClient, NomsFiles.ControleDocumentaire, Permission.BillingImportComptabiliteConfirmerLettrage);
        using var db = c.Db;
        var (horloge, journal, uow, auto) = Socle(db);

        Assert.False(c.Ticket.EstEnEtatEngageant);

        await PriseEnChargeDeTest.PrendreAsync(db, c.Ticket.Id, c.AgentId);
        await new ConfirmerLettrageCommand(new TicketRepository(db), new FileDeTraitementRepository(db), auto, journal, horloge, uow)
            .ConfirmerAsync(c.AgentId, c.Ticket.Id);

        Assert.True((await RechargerAsync(db, c.Ticket.Id)).EstEnEtatEngageant);
    }

    /// <summary>Paiement confirmé — EXPORT.</summary>
    [Fact]
    public async Task ValiderPreuvePaiement_export_rend_le_ticket_engageant()
    {
        var c = Preparer(SoumettreDemandeExportCommand.Module, NomsFilesExport.ValidationPaiement, NomsFilesExport.ControleDocumentaire, Permission.BillingExportPaiementValider);
        using var db = c.Db;
        var (horloge, journal, uow, auto) = Socle(db);

        Assert.False(c.Ticket.EstEnEtatEngageant);

        await PriseEnChargeDeTest.PrendreAsync(db, c.Ticket.Id, c.AgentId);
        await new ValiderPreuvePaiementExportCommand(new TicketRepository(db), new FileDeTraitementRepository(db), auto, journal, horloge, uow)
            .ValiderAsync(c.AgentId, c.Ticket.Id);

        Assert.True((await RechargerAsync(db, c.Ticket.Id)).EstEnEtatEngageant);
    }

    /// <summary>Lettrage confirmé — EXPORT.</summary>
    [Fact]
    public async Task ConfirmerLettrage_export_rend_le_ticket_engageant()
    {
        var c = Preparer(SoumettreDemandeExportCommand.Module, NomsFilesExport.ComptabiliteClient, NomsFilesExport.ControleDocumentaire, Permission.BillingExportComptabiliteConfirmerLettrage);
        using var db = c.Db;
        var (horloge, journal, uow, auto) = Socle(db);

        Assert.False(c.Ticket.EstEnEtatEngageant);

        await PriseEnChargeDeTest.PrendreAsync(db, c.Ticket.Id, c.AgentId);
        await new ConfirmerLettrageExportCommand(new TicketRepository(db), new FileDeTraitementRepository(db), auto, journal, horloge, uow)
            .ExecuterAsync(c.AgentId, c.Ticket.Id);

        Assert.True((await RechargerAsync(db, c.Ticket.Id)).EstEnEtatEngageant);
    }

    /// <summary>Facture éditée — IMPORT. RG-TCK-11 : « document officiel produit ».</summary>
    [Fact]
    public async Task EditerFacture_import_rend_le_ticket_engageant()
    {
        var c = Preparer(SoumettreDemandeImportCommand.Module, NomsFiles.Facturation, NomsFiles.ValidationPaiement, Permission.BillingImportFacturationEditer);
        using var db = c.Db;
        var (horloge, journal, uow, auto) = Socle(db);

        Assert.False(c.Ticket.EstEnEtatEngageant);

        await ProformaDeTest.ValiderAsync(db, c.Ticket.Id, c.AgentId);
        await new EditerFactureCommand(
                new TicketRepository(db), new DossierRepository(db), new FileDeTraitementRepository(db),
                DepotDeTest(db, auto, journal, horloge, uow), auto, journal, horloge, uow)
            .ExecuterAsync(c.AgentId, c.Ticket.Id, "facture.pdf", "application/pdf", Contenu(), 8, Regles);

        Assert.True((await RechargerAsync(db, c.Ticket.Id)).EstEnEtatEngageant);
    }

    /// <summary>Facture éditée — EXPORT.</summary>
    [Fact]
    public async Task EditerFacture_export_rend_le_ticket_engageant()
    {
        var c = Preparer(SoumettreDemandeExportCommand.Module, NomsFilesExport.Facturation, NomsFilesExport.ValidationPaiement, Permission.BillingExportFacturationEditer);
        using var db = c.Db;
        var (horloge, journal, uow, auto) = Socle(db);

        Assert.False(c.Ticket.EstEnEtatEngageant);

        // RG-EXP-03 : le Draft IN précède la facture. Il ne rend PAS le ticket engageant —
        // un brouillon n'est pas un document officiel — ce que l'assertion ci-dessous vérifie.
        await PriseEnChargeDeTest.PrendreAsync(db, c.Ticket.Id, c.AgentId);
        await new EmettreDraftInExportCommand(
                new TicketRepository(db), DepotDeTest(db, auto, journal, horloge, uow),
                auto, journal, horloge, uow)
            .ExecuterAsync(c.AgentId, c.Ticket.Id, "draft-in.pdf", "application/pdf", Contenu(), 8, Regles);

        Assert.False((await RechargerAsync(db, c.Ticket.Id)).EstEnEtatEngageant);

        await ProformaDeTest.ValiderAsync(db, c.Ticket.Id, c.AgentId);
        await new EditerFactureExportCommand(
                new TicketRepository(db), new DossierRepository(db), new FileDeTraitementRepository(db),
                DepotDeTest(db, auto, journal, horloge, uow), auto, journal, horloge, uow)
            .ExecuterAsync(c.AgentId, c.Ticket.Id, "facture.pdf", "application/pdf", Contenu(), 8, Regles);

        Assert.True((await RechargerAsync(db, c.Ticket.Id)).EstEnEtatEngageant);
    }

    // ---------------------------------------------------------------------
    // Les effets attendus du drapeau — c'est ce qui donne son sens au câblage
    // ---------------------------------------------------------------------

    /// <summary>
    /// RG-TCK-07 — une fois le ticket engageant, l'annulation directe par le client est fermée.
    /// C'est l'effet que l'absence d'appelant privait de sens.
    /// </summary>
    [Fact]
    public async Task Un_ticket_devenu_engageant_ne_peut_plus_etre_annule_directement()
    {
        var c = Preparer(SoumettreDemandeImportCommand.Module, NomsFiles.ValidationPaiement, NomsFiles.ControleDocumentaire, Permission.BillingImportPaiementValider);
        using var db = c.Db;
        var (horloge, journal, uow, auto) = Socle(db);

        await PriseEnChargeDeTest.PrendreAsync(db, c.Ticket.Id, c.AgentId);
        await new ValiderPreuvePaiementCommand(new TicketRepository(db), new FileDeTraitementRepository(db), auto, journal, horloge, uow)
            .ValiderAsync(c.AgentId, c.Ticket.Id);

        var ticket = await RechargerAsync(db, c.Ticket.Id);
        Assert.True(ticket.EstEnEtatEngageant);

        // L'annulation directe consulte ce même drapeau (AnnulerTicketCommand) : le ticket est
        // désormais hors de portée d'une annulation libre.
        Assert.False(ticket.EstEligibleASuggestionDeCloture(nombreRelancesMax: 1));
    }

    /// <summary>
    /// RG-TCK-11 — la suggestion de clôture pour absence de réponse est interdite dès lors que le
    /// ticket a atteint un état engageant, même après épuisement des relances.
    /// </summary>
    [Fact]
    public async Task Un_ticket_engageant_n_est_jamais_suggere_a_la_cloture_pour_absence_de_reponse()
    {
        var c = Preparer(SoumettreDemandeImportCommand.Module, NomsFiles.ComptabiliteClient, NomsFiles.ControleDocumentaire, Permission.BillingImportComptabiliteConfirmerLettrage);
        using var db = c.Db;
        var (horloge, journal, uow, auto) = Socle(db);

        await PriseEnChargeDeTest.PrendreAsync(db, c.Ticket.Id, c.AgentId);
        await new ConfirmerLettrageCommand(new TicketRepository(db), new FileDeTraitementRepository(db), auto, journal, horloge, uow)
            .ConfirmerAsync(c.AgentId, c.Ticket.Id);

        var ticket = await RechargerAsync(db, c.Ticket.Id);
        ticket.PrendreEnCharge(c.AgentId, Maintenant);
        ticket.MettreEnAttenteClient(Maintenant);
        ticket.EnregistrerRelance(Maintenant);

        Assert.True(ticket.EstEnEtatEngageant);
        Assert.False(ticket.EstEligibleASuggestionDeCloture(nombreRelancesMax: 1));
    }

    /// <summary>
    /// TÉMOIN — sans état engageant, la suggestion de clôture reste possible. Sans lui, une
    /// implémentation qui refuserait toujours passerait le test précédent.
    /// </summary>
    [Fact]
    public void Un_ticket_non_engageant_reste_suggerable_a_la_cloture()
    {
        var c = Preparer(SoumettreDemandeImportCommand.Module, NomsFiles.ComptabiliteClient, NomsFiles.ControleDocumentaire, Permission.BillingImportComptabiliteConfirmerLettrage);
        using var db = c.Db;

        var ticket = c.Ticket;
        ticket.PrendreEnCharge(c.AgentId, Maintenant);
        ticket.MettreEnAttenteClient(Maintenant);
        ticket.EnregistrerRelance(Maintenant);

        Assert.False(ticket.EstEnEtatEngageant);
        Assert.True(ticket.EstEligibleASuggestionDeCloture(nombreRelancesMax: 1));
    }
}

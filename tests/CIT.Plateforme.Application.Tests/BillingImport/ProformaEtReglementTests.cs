using System.Text;
using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Documents;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CIT.Plateforme.Application.Tests.BillingImport;

/// <summary>
/// Parcours TO BE (documents métier du 05/10/2026) : proforma dans le ticket (dépôt, validation ou
/// correction par le client, facture conditionnée), date d'enlèvement souhaitée, règlement saisi
/// par la caisse (moyen + référence), preuve de paiement classée, étapes métier lisibles.
/// </summary>
public sealed class ProformaEtReglementTests
{
    private static readonly ReglesDepotDocument Regles = new(["pdf"], TailleMaxOctets: 1024);

    private sealed record Contexte(
        PlateformeDbContext Db,
        SoumettreDemandeImportCommand Soumettre,
        ValiderTicketImportCommand Valider,
        GestionProformaCommand Proforma,
        EditerFactureCommand EditerFacture,
        DeposerPreuvePaiementCommand DeposerPreuve,
        ValiderPreuvePaiementCommand ValiderPreuve,
        Guid AgentId,
        Guid SocieteId,
        Guid AuteurId,
        DateOnly Aujourdhui);

    private static async Task<Contexte> ConstruireAsync()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(db, horloge);
        var unite = new UniteDeTravail(db);
        var tickets = new TicketRepository(db);
        var dossiers = new DossierRepository(db);
        var files = new FileDeTraitementRepository(db);
        var autorisation = new AutorisationService(db);
        var cloisonnement = new CloisonnementService(tickets, FabriqueRequetes.ControleAcces(db));
        var deposerDocument = new DeposerDocumentCommand(
            new DocumentRepository(db), tickets,
            new StockageDocumentsFichiers(new ConfigurationBuilder().AddInMemoryCollection().Build(), new EnvironnementDeTest("Development")),
            new AucuneAnalyseAntivirusConfiguree(), autorisation, cloisonnement, journalisation, horloge, unite);

        db.Files.AddRange(
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, SoumettreDemandeImportCommand.FilePiloteManifeste),
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.Facturation),
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.ValidationPaiement),
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.ControleDocumentaire),
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.FileIes));

        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        db.Societes.Add(societe);
        db.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, auteurId, NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        db.Habilitations.AddRange(
            HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, agentId), Permission.BillingImportManifesteInstruire),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingImportFacturationEditer),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingImportPaiementValider),
            HabilitationUtilisateur.Attribuer(agentId, Permission.DocumentDeposer));
        await db.SaveChangesAsync();

        return new Contexte(
            db,
            new SoumettreDemandeImportCommand(tickets, dossiers, new SocieteClienteRepository(db), new UtilisateurSocieteRepository(db), files,
                new NumerotationEnMemoirePourTests(), journalisation, horloge, unite),
            new ValiderTicketImportCommand(tickets, dossiers, files, new SuiviBrancheIesRepository(db), autorisation, journalisation, horloge, unite),
            new GestionProformaCommand(tickets, deposerDocument, cloisonnement, autorisation, journalisation, horloge, unite),
            new EditerFactureCommand(tickets, dossiers, files, deposerDocument, autorisation, journalisation, horloge, unite),
            new DeposerPreuvePaiementCommand(deposerDocument, tickets, journalisation, horloge, unite),
            new ValiderPreuvePaiementCommand(tickets, files, autorisation, journalisation, horloge, unite),
            agentId, societe.Id, auteurId, DateOnly.FromDateTime(horloge.MaintenantUtc));
    }

    private static MemoryStream Pdf(string texte) => new(Encoding.UTF8.GetBytes(texte));

    private static async Task<Guid> TicketEnFacturationAsync(Contexte c, string bl, DateOnly? dateEnlevement = null)
    {
        var soumission = await c.Soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(c.SocieteId, c.AuteurId, [bl], DateEnlevementSouhaitee: dateEnlevement));
        var id = soumission.TicketsCrees[0].Id;
        await c.Valider.ExecuterAsync(c.AgentId, id);
        return id;
    }

    private static async Task EmettreProformaAsync(Contexte c, Guid ticketId, DateOnly? date)
    {
        using var pdf = Pdf("proforma");
        await c.Proforma.EmettreAsync(c.AgentId, ticketId, "proforma.pdf", "application/pdf", pdf, pdf.Length, Regles, date);
    }

    [Fact]
    public async Task La_date_d_enlevement_souhaitee_est_portee_par_le_ticket_et_ne_peut_etre_passee()
    {
        var c = await ConstruireAsync();
        var ticketId = await TicketEnFacturationAsync(c, "BL-DATE-1", c.Aujourdhui.AddDays(4));

        Assert.Equal(c.Aujourdhui.AddDays(4), (await c.Db.Tickets.FindAsync(ticketId))!.DateEnlevementSouhaitee);
        await Assert.ThrowsAsync<ArgumentException>(() => c.Soumettre.ExecuterAsync(
            new SoumettreDemandeImportRequete(c.SocieteId, c.AuteurId, ["BL-DATE-2"], DateEnlevementSouhaitee: c.Aujourdhui.AddDays(-1))));
    }

    [Fact]
    public async Task Une_proforma_import_sans_date_d_enlevement_est_refusee()
    {
        var c = await ConstruireAsync();
        var ticketId = await TicketEnFacturationAsync(c, "BL-PRO-0");

        await Assert.ThrowsAsync<ArgumentException>(() => EmettreProformaAsync(c, ticketId, null));
    }

    [Fact]
    public async Task La_facture_est_refusee_tant_que_la_proforma_n_est_pas_validee_par_le_client()
    {
        var c = await ConstruireAsync();
        var ticketId = await TicketEnFacturationAsync(c, "BL-PRO-1");
        using var facture = Pdf("facture");

        await Assert.ThrowsAsync<ProformaNonValideeException>(() =>
            c.EditerFacture.ExecuterAsync(c.AgentId, ticketId, "facture.pdf", "application/pdf", facture, facture.Length, Regles));

        await EmettreProformaAsync(c, ticketId, c.Aujourdhui.AddDays(2));
        await Assert.ThrowsAsync<ProformaNonValideeException>(() =>
            c.EditerFacture.ExecuterAsync(c.AgentId, ticketId, "facture.pdf", "application/pdf", facture, facture.Length, Regles));
    }

    [Fact]
    public async Task Proforma_emise_puis_validee_par_le_client_ouvre_la_facture()
    {
        var c = await ConstruireAsync();
        var ticketId = await TicketEnFacturationAsync(c, "BL-PRO-2", c.Aujourdhui.AddDays(3));

        await EmettreProformaAsync(c, ticketId, c.Aujourdhui.AddDays(5));
        var ticket = (await c.Db.Tickets.FindAsync(ticketId))!;
        Assert.Equal(EtatProforma.EnAttenteValidation, ticket.EtatProforma);
        Assert.Equal(StatutTicket.EnAttenteClient, ticket.Statut); // temps interne suspendu
        Assert.Equal(c.Aujourdhui.AddDays(5), ticket.DateEnlevementSouhaitee); // date retenue par la facturation
        Assert.Contains(c.Db.JournalEvenements, e => e.TypeEvenement == "ProformaEmise" && e.ChargeUtile.Contains(ticketId.ToString()));

        await c.Proforma.ValiderAsync(c.AuteurId, ticketId);
        Assert.Equal(EtatProforma.Validee, ticket.EtatProforma);
        Assert.Equal(StatutTicket.EnCours, ticket.Statut);

        using var facture = Pdf("facture");
        await c.EditerFacture.ExecuterAsync(c.AgentId, ticketId, "facture.pdf", "application/pdf", facture, facture.Length, Regles);
        Assert.Equal(NomsFiles.ValidationPaiement, (await c.Db.Files.FindAsync(ticket.FileCouranteId))!.Nom);
    }

    [Fact]
    public async Task Le_client_demande_une_correction_motivee_avec_nouvelle_date_puis_une_nouvelle_proforma_suit()
    {
        var c = await ConstruireAsync();
        var ticketId = await TicketEnFacturationAsync(c, "BL-PRO-3");
        await EmettreProformaAsync(c, ticketId, c.Aujourdhui.AddDays(2));

        await Assert.ThrowsAsync<ArgumentException>(() => c.Proforma.DemanderCorrectionAsync(c.AuteurId, ticketId, " ", null));
        await c.Proforma.DemanderCorrectionAsync(c.AuteurId, ticketId, "Magasinage compté deux fois", c.Aujourdhui.AddDays(6));

        var ticket = (await c.Db.Tickets.FindAsync(ticketId))!;
        Assert.Equal(EtatProforma.CorrectionDemandee, ticket.EtatProforma);
        Assert.Equal(c.Aujourdhui.AddDays(6), ticket.DateEnlevementSouhaitee);
        Assert.Contains(c.Db.HistoriqueTickets, h => h.TicketId == ticketId && h.Evenement == "CorrectionProformaDemandee"
            && h.Detail!.Contains("Magasinage compté deux fois"));

        await EmettreProformaAsync(c, ticketId, ticket.DateEnlevementSouhaitee);
        Assert.Equal(EtatProforma.EnAttenteValidation, ticket.EtatProforma);
    }

    [Fact]
    public async Task Un_client_d_une_autre_societe_ne_peut_pas_repondre_a_la_proforma()
    {
        var c = await ConstruireAsync();
        var ticketId = await TicketEnFacturationAsync(c, "BL-PRO-4");
        await EmettreProformaAsync(c, ticketId, c.Aujourdhui.AddDays(2));

        var autreSociete = SocieteCliente.Soumettre(Guid.NewGuid(), "Autre", TypeClient.Normal, DateTime.UtcNow);
        autreSociete.Valider();
        var intrus = Guid.NewGuid();
        c.Db.Societes.Add(autreSociete);
        c.Db.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(c.Db, intrus, NatureUtilisateur.Client), autreSociete.Id, DateTime.UtcNow, droitSoumission: true));
        await c.Db.SaveChangesAsync();

        await Assert.ThrowsAsync<AccesRefuseException>(() => c.Proforma.ValiderAsync(intrus, ticketId));
    }

    [Fact]
    public async Task La_caisse_saisit_moyen_et_reference_ensemble_et_ils_sont_traces()
    {
        var c = await ConstruireAsync();
        var ticketId = await TicketEnFacturationAsync(c, "BL-REG-1");
        await ProformaDeTest.ValiderAsync(c.Db, ticketId, c.AgentId);
        using (var facture = Pdf("facture"))
        {
            await c.EditerFacture.ExecuterAsync(c.AgentId, ticketId, "facture.pdf", "application/pdf", facture, facture.Length, Regles);
        }

        await Assert.ThrowsAsync<ArgumentException>(() => c.ValiderPreuve.ValiderAsync(c.AgentId, ticketId, MoyenPaiement.Wave, null));
        await c.ValiderPreuve.ValiderAsync(c.AgentId, ticketId, MoyenPaiement.Wave, " WV-778812 ");

        Assert.Contains(c.Db.HistoriqueTickets, h => h.TicketId == ticketId && h.Evenement == "PaiementValide" && h.Detail == "moyen=Wave; ref=WV-778812");
    }

    [Fact]
    public async Task Une_nouvelle_preuve_apres_refus_est_classee_et_relance_le_traitement()
    {
        var c = await ConstruireAsync();
        var ticketId = await TicketEnFacturationAsync(c, "BL-REG-2");
        await ProformaDeTest.ValiderAsync(c.Db, ticketId, c.AgentId);
        using (var facture = Pdf("facture"))
        {
            await c.EditerFacture.ExecuterAsync(c.AgentId, ticketId, "facture.pdf", "application/pdf", facture, facture.Length, Regles);
        }

        await c.ValiderPreuve.RefuserAsync(c.AgentId, ticketId, "Montant incorrect");
        var ticket = (await c.Db.Tickets.FindAsync(ticketId))!;
        Assert.Equal(StatutTicket.EnAttenteClient, ticket.Statut);

        using var preuve = Pdf("preuve");
        await c.DeposerPreuve.ExecuterAsync(c.AuteurId, ticketId, "preuve.pdf", "application/pdf", preuve, preuve.Length, Regles);

        Assert.Equal(StatutTicket.EnCours, ticket.Statut);
        Assert.Contains(c.Db.Documents, d => d.TicketId == ticketId && d.Categorie == CategorieDocumentBillingImport.PreuvePaiement);
    }

    /// <summary>Paramètre « Proforma obligatoire » : par défaut la facture exige une proforma validée ;
    /// désactivé, la facture directe est permise — jamais par-dessus une proforma en cours d'échange.</summary>
    [Fact]
    public async Task La_proforma_obligatoire_est_parametrable_sans_jamais_facturer_par_dessus_un_echange_en_cours()
    {
        Assert.Equal(1, CIT.Plateforme.Application.Exploitation.CatalogueParametresPlateforme
            .Trouver(CIT.Plateforme.Application.Exploitation.CatalogueParametresPlateforme.ProformaObligatoire)!.Defaut);

        var c = await ConstruireAsync();
        var ticket = (await c.Db.Tickets.FindAsync(await TicketEnFacturationAsync(c, "BL-PARAM-1")))!;

        Assert.False(GestionProformaCommand.FacturePermise(ticket, proformaObligatoire: true));
        Assert.True(GestionProformaCommand.FacturePermise(ticket, proformaObligatoire: false));

        await EmettreProformaAsync(c, ticket.Id, c.Aujourdhui.AddDays(2));
        Assert.False(GestionProformaCommand.FacturePermise(ticket, proformaObligatoire: false));

        await c.Proforma.ValiderAsync(c.AuteurId, ticket.Id);
        Assert.True(GestionProformaCommand.FacturePermise(ticket, proformaObligatoire: true));
    }

    [Theory]
    [InlineData("Pilote Manifeste", StatutTicket.EnAttenteDePriseEnCharge, EtatProforma.Aucune, false, "Etape.Soumis")]
    [InlineData("Pilote Manifeste", StatutTicket.EnAttenteClient, EtatProforma.Aucune, false, "Etape.ComplementDemande")]
    [InlineData("Facturation", StatutTicket.EnAttenteDePriseEnCharge, EtatProforma.Aucune, false, "Etape.BlValide")]
    [InlineData("Facturation", StatutTicket.EnAttenteClient, EtatProforma.EnAttenteValidation, false, "Etape.ProformaAValider")]
    [InlineData("Facturation", StatutTicket.EnCours, EtatProforma.CorrectionDemandee, false, "Etape.ProformaEnCorrection")]
    [InlineData("Facturation", StatutTicket.EnCours, EtatProforma.Validee, false, "Etape.FactureEnPreparation")]
    [InlineData("Validation Paiement", StatutTicket.EnAttenteDePriseEnCharge, EtatProforma.Validee, false, "Etape.EnAttentePaiement")]
    [InlineData("Validation Paiement", StatutTicket.EnAttenteDePriseEnCharge, EtatProforma.Validee, true, "Etape.PaiementAValider")]
    [InlineData("Validation Paiement", StatutTicket.EnAttenteClient, EtatProforma.Validee, true, "Etape.NouvellePreuveAttendue")]
    [InlineData("Comptabilité Client", StatutTicket.EnCours, EtatProforma.Validee, false, "Etape.LettrageEnCours")]
    [InlineData("Contrôle Documentaire", StatutTicket.EnCours, EtatProforma.Validee, false, "Etape.ControleDocumentaire")]
    [InlineData("Contrôle Documentaire", StatutTicket.Cloture, EtatProforma.Validee, false, "Etape.Cloture")]
    public void L_etape_metier_se_deduit_de_la_file_du_statut_et_de_la_proforma(
        string file, StatutTicket statut, EtatProforma proforma, bool preuve, string attendu)
    {
        Assert.Equal(attendu, EtapeParcoursClient.Determiner(SoumettreDemandeImportCommand.Module, file, statut, proforma, estRejete: false, preuve));
    }

    [Fact]
    public void Un_rejet_s_affiche_rejete_et_hors_billing_aucune_etape()
    {
        Assert.Equal("Etape.Rejete", EtapeParcoursClient.Determiner("BillingImport", "Pilote Manifeste", StatutTicket.Annule, EtatProforma.Aucune, true, false));
        Assert.Equal("Etape.Codification", EtapeParcoursClient.Determiner("BillingExport", "Back Office Export", StatutTicket.EnCours, EtatProforma.Aucune, false, false));
        Assert.Null(EtapeParcoursClient.Determiner("CustomerComplaint", "Service Client", StatutTicket.EnCours, EtatProforma.Aucune, false, false));
    }
}

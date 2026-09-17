using ClosedXML.Excel;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tickets;

/// <summary>
/// [TEST-SPEC] Exigence CDC: SOC-14 → US6 (F012).
/// SOC-14 : « Export des indicateurs et des listes de tickets aux formats usuels (Excel, PDF). »
/// Démontre la génération d'un classeur Excel (.xlsx) valide et relisible, avec les mêmes colonnes
/// et le même respect du cloisonnement multi-tenant (CPT-04) que l'export CSV.
/// </summary>
public sealed class SocCert14ExportExcelTests
{
    [Fact]
    public async Task SOC_CERT_14_C_export_excel_produit_un_classeur_valide_avec_colonnes_et_lignes_attendues()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var ticketRepo = new TicketRepository(dbContext);
        var dossierRepo = new DossierRepository(dbContext);
        var fileRepo = new FileDeTraitementRepository(dbContext);
        var prioriteRepo = new PrioriteRepository(dbContext);
        var utilSocieteRepo = new UtilisateurSocieteRepository(dbContext);
        var autorisation = new AutorisationService(dbContext);

        var filePilote = FileDeTraitement.Creer("BillingImport", "Pilote Manifeste");
        dbContext.Files.Add(filePilote);

        var niveauStandard = NiveauPriorite.Definir("Normale", 1);
        dbContext.NiveauxPriorite.Add(niveauStandard);

        var clientAuteurId = Guid.NewGuid();
        var societeA = SocieteCliente.Soumettre(clientAuteurId, "Societe A", TypeClient.Normal, horloge.MaintenantUtc);
        societeA.Valider();
        societeA.EnregistrerAdministrateurActif();
        dbContext.Societes.Add(societeA);

        var rattachementA = UtilisateurSociete.Rattacher(clientAuteurId, societeA.Id, horloge.MaintenantUtc, estAdministrateur: true);
        dbContext.UtilisateursSociete.Add(rattachementA);

        var dossierA = Dossier.Creer("DOS-EXP-XL1", societeA.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossierA);

        var ticket1 = Ticket.Creer("TCK-EXP-XL1", "BillingImport", "Import", filePilote.Id, dossierA.Id, horloge.MaintenantUtc, "BL-XL01");
        ticket1.DefinirPriorite(niveauStandard.Id);
        dbContext.Tickets.Add(ticket1);

        var ticket2 = Ticket.Creer("TCK-EXP-XL2", "BillingImport", "Import", filePilote.Id, dossierA.Id, horloge.MaintenantUtc.AddMinutes(5), "BL-XL02");
        dbContext.Tickets.Add(ticket2);

        await dbContext.SaveChangesAsync();

        var query = new ExporterTicketsExcelQuery(
            ticketRepo, dossierRepo, fileRepo, prioriteRepo, utilSocieteRepo, autorisation);

        var octetsXlsx = await query.ExecuterPourClientAsync(clientAuteurId, societeA.Id);

        Assert.NotEmpty(octetsXlsx);

        // Round-trip : ClosedXML relit ce qu'il a écrit — preuve que le fichier produit est un
        // .xlsx valide et non un flux corrompu.
        using var flux = new MemoryStream(octetsXlsx);
        using var classeur = new XLWorkbook(flux);
        var feuille = classeur.Worksheet("Tickets");

        Assert.Equal("NumeroTicket", feuille.Cell(1, 1).GetString());
        Assert.Equal("Module", feuille.Cell(1, 2).GetString());
        Assert.Equal("TypeDemande", feuille.Cell(1, 3).GetString());
        Assert.Equal("Statut", feuille.Cell(1, 4).GetString());
        Assert.Equal("FileCourante", feuille.Cell(1, 5).GetString());
        Assert.Equal("DateCreationUtc", feuille.Cell(1, 6).GetString());
        Assert.Equal("Priorite", feuille.Cell(1, 7).GetString());
        Assert.Equal("ReferenceDossier", feuille.Cell(1, 8).GetString());

        // 2 lignes de données + 1 ligne d'en-tête
        var derniereLigneUtilisee = feuille.LastRowUsed()!.RowNumber();
        Assert.Equal(3, derniereLigneUtilisee);

        Assert.Equal("TCK-EXP-XL1", feuille.Cell(2, 1).GetString());
        Assert.Equal("Pilote Manifeste", feuille.Cell(2, 5).GetString());
        Assert.Equal("Normale", feuille.Cell(2, 7).GetString());
        Assert.Equal("DOS-EXP-XL1", feuille.Cell(2, 8).GetString());

        Assert.Equal("TCK-EXP-XL2", feuille.Cell(3, 1).GetString());
        Assert.Equal(string.Empty, feuille.Cell(3, 7).GetString()); // Pas de priorité définie
    }

    [Fact]
    public async Task SOC_CERT_14_D_securite_client_ne_peut_jamais_exporter_en_excel_les_tickets_d_une_autre_societe()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var ticketRepo = new TicketRepository(dbContext);
        var dossierRepo = new DossierRepository(dbContext);
        var fileRepo = new FileDeTraitementRepository(dbContext);
        var prioriteRepo = new PrioriteRepository(dbContext);
        var utilSocieteRepo = new UtilisateurSocieteRepository(dbContext);
        var autorisation = new AutorisationService(dbContext);

        var fileId = Guid.NewGuid();

        var clientAId = Guid.NewGuid();
        var societeA = SocieteCliente.Soumettre(clientAId, "Societe A", TypeClient.Normal, horloge.MaintenantUtc);
        societeA.Valider();
        societeA.EnregistrerAdministrateurActif();
        dbContext.Societes.Add(societeA);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, clientAId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societeA.Id, horloge.MaintenantUtc, estAdministrateur: true));

        var dossierA = Dossier.Creer("DOS-AAA-XL", societeA.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossierA);
        var ticketA = Ticket.Creer("TCK-AAA-XL", "BillingImport", "Import", fileId, dossierA.Id, horloge.MaintenantUtc);
        dbContext.Tickets.Add(ticketA);

        var clientBId = Guid.NewGuid();
        var societeB = SocieteCliente.Soumettre(clientBId, "Societe B", TypeClient.Normal, horloge.MaintenantUtc);
        societeB.Valider();
        societeB.EnregistrerAdministrateurActif();
        dbContext.Societes.Add(societeB);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, clientBId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societeB.Id, horloge.MaintenantUtc, estAdministrateur: true));

        var dossierB = Dossier.Creer("DOS-BBB-XL", societeB.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossierB);
        var ticketB = Ticket.Creer("TCK-BBB-XL", "BillingImport", "Import", fileId, dossierB.Id, horloge.MaintenantUtc);
        dbContext.Tickets.Add(ticketB);

        await dbContext.SaveChangesAsync();

        var query = new ExporterTicketsExcelQuery(
            ticketRepo, dossierRepo, fileRepo, prioriteRepo, utilSocieteRepo, autorisation);

        var xlsxClientA = await query.ExecuterPourClientAsync(clientAId, societeA.Id);
        using var flux = new MemoryStream(xlsxClientA);
        using var classeur = new XLWorkbook(flux);
        var feuille = classeur.Worksheet("Tickets");
        var valeurs = feuille.RowsUsed().Select(r => r.Cell(1).GetString()).ToList();

        Assert.Contains("TCK-AAA-XL", valeurs);
        Assert.DoesNotContain("TCK-BBB-XL", valeurs);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            query.ExecuterPourClientAsync(clientAId, societeB.Id));
    }
}

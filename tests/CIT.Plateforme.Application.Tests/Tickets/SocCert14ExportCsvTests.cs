using System.Text;
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
/// SOC-14 : « Export des données en format tabulaire standard (CSV) depuis les listes et tableaux. »
/// Démontre la génération CSV UTF-8 avec BOM, séparateur point-virgule, dates ISO et respect strict du cloisonnement multi-tenant (CPT-04 / SEC-03).
/// </summary>
public sealed class SocCert14ExportCsvTests
{
    [Fact]
    public async Task SOC_CERT_14_A_export_csv_respecte_format_utf8_bom_colonnes_et_separateur()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var ticketRepo = new TicketRepository(dbContext);
        var dossierRepo = new DossierRepository(dbContext);
        var fileRepo = new FileDeTraitementRepository(dbContext);
        var prioriteRepo = new PrioriteRepository(dbContext);
        var societeRepo = new SocieteClienteRepository(dbContext);
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

        var dossierA = Dossier.Creer("DOS-EXP-001", societeA.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossierA);

        var ticket1 = Ticket.Creer("TCK-EXP-001", "BillingImport", "Import", filePilote.Id, dossierA.Id, horloge.MaintenantUtc, "BL-1234");
        ticket1.DefinirPriorite(niveauStandard.Id);
        dbContext.Tickets.Add(ticket1);

        await dbContext.SaveChangesAsync();

        var query = new ExporterTicketsCsvQuery(
            ticketRepo, dossierRepo, fileRepo, prioriteRepo, utilSocieteRepo, autorisation);

        var fluxCsv = await query.ExecuterPourClientAsync(clientAuteurId, societeA.Id);

        // Vérification UTF-8 avec BOM
        var bom = Encoding.UTF8.GetPreamble();
        Assert.True(fluxCsv.Length >= 3);
        Assert.Equal(bom[0], fluxCsv[0]);
        Assert.Equal(bom[1], fluxCsv[1]);
        Assert.Equal(bom[2], fluxCsv[2]);

        var contenuCsv = Encoding.UTF8.GetString(fluxCsv);
        var lignes = contenuCsv.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);

        // En-tête exact
        Assert.Equal("NumeroTicket;Module;TypeDemande;Statut;FileCourante;DateCreationUtc;Priorite;ReferenceDossier", lignes[0].TrimStart('\uFEFF'));

        // Ligne de données
        Assert.Contains("TCK-EXP-001;BillingImport;Import;", lignes[1]);
        Assert.Contains(";Pilote Manifeste;", lignes[1]);
        Assert.Contains(";Normale;DOS-EXP-001", lignes[1]);
    }

    [Fact]
    public async Task SOC_CERT_14_B_securite_client_ne_peut_jamais_exporter_tickets_d_une_autre_societe()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var ticketRepo = new TicketRepository(dbContext);
        var dossierRepo = new DossierRepository(dbContext);
        var fileRepo = new FileDeTraitementRepository(dbContext);
        var prioriteRepo = new PrioriteRepository(dbContext);
        var societeRepo = new SocieteClienteRepository(dbContext);
        var utilSocieteRepo = new UtilisateurSocieteRepository(dbContext);
        var autorisation = new AutorisationService(dbContext);

        var fileId = Guid.NewGuid();

        // Société A
        var clientAId = Guid.NewGuid();
        var societeA = SocieteCliente.Soumettre(clientAId, "Societe A", TypeClient.Normal, horloge.MaintenantUtc);
        societeA.Valider();
        societeA.EnregistrerAdministrateurActif();
        dbContext.Societes.Add(societeA);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, clientAId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societeA.Id, horloge.MaintenantUtc, estAdministrateur: true));

        var dossierA = Dossier.Creer("DOS-AAA", societeA.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossierA);
        var ticketA = Ticket.Creer("TCK-AAA", "BillingImport", "Import", fileId, dossierA.Id, horloge.MaintenantUtc);
        dbContext.Tickets.Add(ticketA);

        // Société B
        var clientBId = Guid.NewGuid();
        var societeB = SocieteCliente.Soumettre(clientBId, "Societe B", TypeClient.Normal, horloge.MaintenantUtc);
        societeB.Valider();
        societeB.EnregistrerAdministrateurActif();
        dbContext.Societes.Add(societeB);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, clientBId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societeB.Id, horloge.MaintenantUtc, estAdministrateur: true));

        var dossierB = Dossier.Creer("DOS-BBB", societeB.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossierB);
        var ticketB = Ticket.Creer("TCK-BBB", "BillingImport", "Import", fileId, dossierB.Id, horloge.MaintenantUtc);
        dbContext.Tickets.Add(ticketB);

        await dbContext.SaveChangesAsync();

        var query = new ExporterTicketsCsvQuery(
            ticketRepo, dossierRepo, fileRepo, prioriteRepo, utilSocieteRepo, autorisation);

        // Client A tente d'exporter sa société
        var csvClientA = await query.ExecuterPourClientAsync(clientAId, societeA.Id);
        var texteA = Encoding.UTF8.GetString(csvClientA);

        Assert.Contains("TCK-AAA", texteA);
        Assert.DoesNotContain("TCK-BBB", texteA); // Strict cloisonnement CPT-04

        // Client A tente d'exporter la société B (attaque inter-sociétés) → AccesRefuseException
        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            query.ExecuterPourClientAsync(clientAId, societeB.Id));
    }
}

using System.Text;
using CIT.Plateforme.Application.BillingExport;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.BillingExport;

/// <summary>
/// [TEST-CDC] Rejeu intégral de REC-SCE-05 (docs/04-matrice-recette.md) :
/// Parcours EXPORT pour les 4 profils clients (Normal, En Compte, Deposit, IES)
/// et indépendance multi-bookings dans l'application composée réelle (DI + SQL Server réel).
/// Exigences : EXP-01→04, UNI-04.
/// </summary>
public sealed class RecSce05ExportE2ETests(PlateformeWebApplicationFactory factory) : IClassFixture<PlateformeWebApplicationFactory>
{
    private static readonly ReglesDepotDocument Regles = new(["pdf", "jpg"], TailleMaxOctets: 1024);

    private static async Task AssurerFilesExportAsync(PlateformeDbContext dbContext)
    {
        var nomsRequis = new[]
        {
            NomsFilesExport.BackOfficeExport,
            NomsFilesExport.Facturation,
            NomsFilesExport.ValidationPaiement,
            NomsFilesExport.ComptabiliteClient,
            NomsFilesExport.ControleDocumentaire,
            NomsFilesExport.FileIes
        };

        var ajoutes = false;
        foreach (var nom in nomsRequis)
        {
            if (!await dbContext.Files.AnyAsync(f => f.Module == SoumettreDemandeExportCommand.Module && f.Nom == nom))
            {
                dbContext.Files.Add(FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, nom));
                ajoutes = true;
            }
        }
        if (ajoutes)
        {
            await dbContext.SaveChangesAsync();
        }
    }

    private static async Task AssurerHabilitationsAgentAsync(PlateformeDbContext dbContext, Guid agentId)
    {
        var files = await dbContext.Files.Where(f => f.Module == SoumettreDemandeExportCommand.Module).ToListAsync();
        var perms = new[]
        {
            Permission.BillingExportBackOfficeInstruire,
            Permission.BillingExportFacturationEditer,
            Permission.BillingExportPaiementValider,
            Permission.BillingExportComptabiliteConfirmerLettrage,
            Permission.BillingExportControleDocumentaireTraiter,
            Permission.BillingExportIesBackOfficeInstruire,
            Permission.BillingExportIesControleDocumentaireInstruire,
            Permission.DocumentDeposer,
            Permission.TicketPrendreEnCharge,
            Permission.TicketTransferer,
            Permission.TicketCloturer
        };

        foreach (var p in perms)
        {
            if (!await dbContext.Habilitations.AnyAsync(h => h.UtilisateurId == agentId && h.PermissionCode == p && h.FileId == null))
            {
                dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), p));
            }
        }
        await dbContext.SaveChangesAsync();
    }

    private static MemoryStream Contenu(string texte) => new(Encoding.UTF8.GetBytes(texte));

    [Fact]
    public async Task REC_SCE_05_Circuit_Client_Normal_Export_Complet()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<PlateformeDbContext>();
        await AssurerFilesExportAsync(dbContext);

        var agentId = Guid.NewGuid();
        await AssurerHabilitationsAgentAsync(dbContext, agentId);

        var clientId = Guid.NewGuid();
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "ExportNormalSA", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, clientId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        await dbContext.SaveChangesAsync();

        var soumettre = services.GetRequiredService<SoumettreDemandeExportCommand>();
        var validerBO = services.GetRequiredService<ValiderTicketExportCommand>();
        var emettreDraftIn = services.GetRequiredService<EmettreDraftInExportCommand>();
        var editerFacture = services.GetRequiredService<EditerFactureExportCommand>();
        var deposerPreuve = services.GetRequiredService<DeposerPreuvePaiementExportCommand>();
        var validerPaiement = services.GetRequiredService<ValiderPreuvePaiementExportCommand>();
        var finaliserDoc = services.GetRequiredService<FinaliserControleDocumentaireExportCommand>();

        var bookingNo = $"BKG-NORM-{Guid.NewGuid():N}"[..15];
        var resultat = await soumettre.ExecuterAsync(new SoumettreDemandeExportRequete(
            societe.Id, clientId, [bookingNo]));

        Assert.Single(resultat.TicketsCrees);
        var ticketId = resultat.TicketsCrees[0].Id;

        // 1. Back Office Export
        await validerBO.ExecuterAsync(agentId, ticketId);

        // 2. Facturation
        // RG-EXP-03 : le Draft IN précède la facture.
        await emettreDraftIn.ExecuterAsync(agentId, ticketId, "draft-in.pdf", "application/pdf", Contenu("DRAFTIN"), 7, Regles);
        await editerFacture.ExecuterAsync(agentId, ticketId, "facture-export.pdf", "application/pdf", Contenu("FACTURE"), 7, Regles);

        // 3. Client dépose preuve paiement
        await deposerPreuve.ExecuterAsync(clientId, ticketId, "virement.pdf", "application/pdf", Contenu("VIREMENT"), 8, Regles);

        // 4. Validation Paiement
        await validerPaiement.ValiderAsync(agentId, ticketId);

        // 5. Contrôle Documentaire (BEE) & Clôture
        await finaliserDoc.ExecuterAsync(agentId, ticketId, "bee.pdf", "application/pdf", Contenu("BEE"), 3, Regles);

        var ticketFinal = await dbContext.Tickets.AsNoTracking().SingleAsync(t => t.Id == ticketId);
        Assert.Equal(StatutTicket.Cloture, ticketFinal.Statut);
    }

    [Fact]
    public async Task REC_SCE_05_Circuit_Client_EnCompte_Export_Complet()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<PlateformeDbContext>();
        await AssurerFilesExportAsync(dbContext);

        var agentId = Guid.NewGuid();
        await AssurerHabilitationsAgentAsync(dbContext, agentId);

        var clientId = Guid.NewGuid();
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "ExportEnCompteSA", TypeClient.EnCompte, DateTime.UtcNow);
        societe.Valider();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, clientId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        await dbContext.SaveChangesAsync();

        var soumettre = services.GetRequiredService<SoumettreDemandeExportCommand>();
        var validerBO = services.GetRequiredService<ValiderTicketExportCommand>();
        var emettreDraftIn = services.GetRequiredService<EmettreDraftInExportCommand>();
        var editerFacture = services.GetRequiredService<EditerFactureExportCommand>();
        var finaliserDoc = services.GetRequiredService<FinaliserControleDocumentaireExportCommand>();

        var bookingNo = $"BKG-CPTE-{Guid.NewGuid():N}"[..15];
        var res = await soumettre.ExecuterAsync(new SoumettreDemandeExportRequete(
            societe.Id, clientId, [bookingNo]));
        var ticketId = res.TicketsCrees[0].Id;

        // 1. Back Office
        await validerBO.ExecuterAsync(agentId, ticketId);

        // 2. Facturation -> Direct Contrôle Doc (pas de caisse)
        // RG-EXP-03 : le Draft IN précède la facture.
        await emettreDraftIn.ExecuterAsync(agentId, ticketId, "draft-in.pdf", "application/pdf", Contenu("DRAFTIN"), 7, Regles);
        await editerFacture.ExecuterAsync(agentId, ticketId, "facture-export.pdf", "application/pdf", Contenu("FACTURE"), 7, Regles);

        var fileDoc = await dbContext.Files.SingleAsync(f => f.Module == SoumettreDemandeExportCommand.Module && f.Nom == NomsFilesExport.ControleDocumentaire);
        var ticketFacture = await dbContext.Tickets.AsNoTracking().SingleAsync(t => t.Id == ticketId);
        Assert.Equal(fileDoc.Id, ticketFacture.FileCouranteId);

        // 3. Contrôle Doc (BEE) & Clôture
        await finaliserDoc.ExecuterAsync(agentId, ticketId, "bee.pdf", "application/pdf", Contenu("BEE"), 3, Regles);

        var ticketFinal = await dbContext.Tickets.AsNoTracking().SingleAsync(t => t.Id == ticketId);
        Assert.Equal(StatutTicket.Cloture, ticketFinal.Statut);
    }

    [Fact]
    public async Task REC_SCE_05_Circuit_Client_Deposit_Export_Complet()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<PlateformeDbContext>();
        await AssurerFilesExportAsync(dbContext);

        var agentId = Guid.NewGuid();
        await AssurerHabilitationsAgentAsync(dbContext, agentId);

        var clientId = Guid.NewGuid();
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "ExportDepositSA", TypeClient.Deposit, DateTime.UtcNow);
        societe.Valider();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, clientId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        await dbContext.SaveChangesAsync();

        var soumettre = services.GetRequiredService<SoumettreDemandeExportCommand>();
        var validerBO = services.GetRequiredService<ValiderTicketExportCommand>();
        var emettreDraftIn = services.GetRequiredService<EmettreDraftInExportCommand>();
        var editerFacture = services.GetRequiredService<EditerFactureExportCommand>();
        var confirmerLettrage = services.GetRequiredService<ConfirmerLettrageExportCommand>();
        var finaliserDoc = services.GetRequiredService<FinaliserControleDocumentaireExportCommand>();

        var bookingNo = $"BKG-DEPO-{Guid.NewGuid():N}"[..15];
        var res = await soumettre.ExecuterAsync(new SoumettreDemandeExportRequete(
            societe.Id, clientId, [bookingNo]));
        var ticketId = res.TicketsCrees[0].Id;

        // 1. Back Office
        await validerBO.ExecuterAsync(agentId, ticketId);

        // 2. Facturation -> Comptabilité Client
        // RG-EXP-03 : le Draft IN précède la facture.
        await emettreDraftIn.ExecuterAsync(agentId, ticketId, "draft-in.pdf", "application/pdf", Contenu("DRAFTIN"), 7, Regles);
        await editerFacture.ExecuterAsync(agentId, ticketId, "facture-export.pdf", "application/pdf", Contenu("FACTURE"), 7, Regles);

        // 3. Confirmation Lettrage
        await confirmerLettrage.ExecuterAsync(agentId, ticketId);

        // 4. Contrôle Doc (BEE) & Clôture
        await finaliserDoc.ExecuterAsync(agentId, ticketId, "bee.pdf", "application/pdf", Contenu("BEE"), 3, Regles);

        var ticketFinal = await dbContext.Tickets.AsNoTracking().SingleAsync(t => t.Id == ticketId);
        Assert.Equal(StatutTicket.Cloture, ticketFinal.Statut);
    }

    [Fact]
    public async Task REC_SCE_05_Circuit_Client_IES_Export_Complet()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<PlateformeDbContext>();
        await AssurerFilesExportAsync(dbContext);

        var agentId = Guid.NewGuid();
        await AssurerHabilitationsAgentAsync(dbContext, agentId);

        var clientId = Guid.NewGuid();
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "ExportIesSA", TypeClient.IES, DateTime.UtcNow);
        societe.Valider();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, clientId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        await dbContext.SaveChangesAsync();

        var soumettre = services.GetRequiredService<SoumettreDemandeExportCommand>();
        var validerBO = services.GetRequiredService<ValiderTicketExportCommand>();
        var instruireIes = services.GetRequiredService<InstruireBrancheIesExportCommand>();
        var constaterJointure = services.GetRequiredService<ConstaterJointureIesExportEtFinaliserCommand>();

        var bookingNo = $"BKG-IES-{Guid.NewGuid():N}"[..15];
        var res = await soumettre.ExecuterAsync(new SoumettreDemandeExportRequete(
            societe.Id, clientId, [bookingNo]));
        var ticketId = res.TicketsCrees[0].Id;

        // 1. Validation Back Office -> Aiguillage automatique vers File IES et création des branches de suivi
        await validerBO.ExecuterAsync(agentId, ticketId);

        // 2. Instruction parallèle : BackOffice et Contrôle Doc
        await instruireIes.ExecuterAsync(agentId, ticketId, BrancheIes.BackOffice);
        await instruireIes.ExecuterAsync(agentId, ticketId, BrancheIes.ControleDocumentaire);

        // 3. Constatation Jointure & Clôture
        await constaterJointure.ExecuterAsync(agentId, ticketId);

        var ticketFinal = await dbContext.Tickets.AsNoTracking().SingleAsync(t => t.Id == ticketId);
        Assert.Equal(StatutTicket.Cloture, ticketFinal.Statut);
    }

    [Fact]
    public async Task REC_SCE_05_Multi_Bookings_Independance_Instruction()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<PlateformeDbContext>();
        await AssurerFilesExportAsync(dbContext);

        var agentId = Guid.NewGuid();
        await AssurerHabilitationsAgentAsync(dbContext, agentId);

        var clientId = Guid.NewGuid();
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "ExportMultiBkgSA", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, clientId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        await dbContext.SaveChangesAsync();

        var soumettre = services.GetRequiredService<SoumettreDemandeExportCommand>();
        var validerBO = services.GetRequiredService<ValiderTicketExportCommand>();
        var rejeterBO = services.GetRequiredService<RejeterTicketExportCommand>();
        var gestionComplement = services.GetRequiredService<GestionComplementExportCommand>();

        var bkg1 = $"BKG-M1-{Guid.NewGuid():N}"[..15];
        var bkg2 = $"BKG-M2-{Guid.NewGuid():N}"[..15];
        var bkg3 = $"BKG-M3-{Guid.NewGuid():N}"[..15];

        var res = await soumettre.ExecuterAsync(new SoumettreDemandeExportRequete(
            societe.Id, clientId, [bkg1, bkg2, bkg3]));

        Assert.Equal(3, res.TicketsCrees.Count);
        var tck1 = res.TicketsCrees[0].Id;
        var tck2 = res.TicketsCrees[1].Id;
        var tck3 = res.TicketsCrees[2].Id;

        // TCK 1: Valider
        await validerBO.ExecuterAsync(agentId, tck1);
        var state1 = await dbContext.Tickets.AsNoTracking().SingleAsync(t => t.Id == tck1);
        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, state1.Statut);

        // TCK 2: Rejeter
        await rejeterBO.ExecuterAsync(agentId, tck2, "Booking inexistant chez l'armateur");
        var state2 = await dbContext.Tickets.AsNoTracking().SingleAsync(t => t.Id == tck2);
        Assert.Equal(StatutTicket.Annule, state2.Statut);
        Assert.Equal(TypeAnnulationExport.RejetBackOffice, state2.TypeAnnulationExport);

        // TCK 3: Complément puis Réponse
        await gestionComplement.DemanderComplementAsync(agentId, tck3, "Document d'empotage requis");
        var state3a = await dbContext.Tickets.AsNoTracking().SingleAsync(t => t.Id == tck3);
        Assert.Equal(StatutTicket.EnAttenteClient, state3a.Statut);

        await gestionComplement.DeposerComplementAsync(clientId, tck3);
        var state3b = await dbContext.Tickets.AsNoTracking().SingleAsync(t => t.Id == tck3);
        Assert.Equal(StatutTicket.EnCours, state3b.Statut);
    }
}

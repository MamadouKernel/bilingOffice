using System.Text;
using CIT.Plateforme.Application.BillingImport;
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

namespace CIT.Plateforme.Web.Tests.BillingImport;

/// <summary>
/// [TEST-CDC] Rejeu intégral de la portion IMP/UNI de REC-SCE-01 (docs/04-matrice-recette.md),
/// bout-en-bout sur l'application composée réelle (DI + SQL Server réel). CDC : IMP-01→06,
/// IMP-10→14, UNI-01→03. 3 BL soumis ensemble dans un même dossier multi-BL (IMP-02) : 1 validé et
/// mené jusqu'à clôture (circuit Client Normal complet), 1 rejeté (motif obligatoire), 1 mis en
/// attente de complément puis validé après réponse du client — aucune interférence entre les trois
/// (D4, indépendance par BL).
/// </summary>
public sealed class RecSce01ImportNormalTests(PlateformeWebApplicationFactory factory) : IClassFixture<PlateformeWebApplicationFactory>
{
    private static readonly ReglesDepotDocument Regles = new(["pdf", "jpg"], TailleMaxOctets: 1024);

    private static async Task AssurerFilesAsync(PlateformeDbContext dbContext)
    {
        var nomsRequis = new[]
        {
            SoumettreDemandeImportCommand.FilePiloteManifeste, NomsFiles.Facturation,
            NomsFiles.ValidationPaiement, NomsFiles.ControleDocumentaire, NomsFiles.FileIes,
        };
        var manquants = nomsRequis.Where(nom =>
            !dbContext.Files.Any(f => f.Module == SoumettreDemandeImportCommand.Module && f.Nom == nom));
        var ajoutes = false;
        foreach (var nom in manquants)
        {
            dbContext.Files.Add(FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, nom));
            ajoutes = true;
        }
        if (ajoutes)
        {
            await dbContext.SaveChangesAsync();
        }
    }

    private static MemoryStream Contenu(string texte) => new(Encoding.UTF8.GetBytes(texte));

    [Fact]
    public async Task Trois_bl_dans_un_dossier_multi_bl_sont_instruits_independamment_sans_interference()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<PlateformeDbContext>();
        await AssurerFilesAsync(dbContext);

        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, auteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        dbContext.Habilitations.AddRange(
            HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.BillingImportManifesteInstruire),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingImportFacturationEditer),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingImportPaiementValider),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingImportControleDocumentaireTraiter),
            HabilitationUtilisateur.Attribuer(agentId, Permission.DocumentDeposer));
        await dbContext.SaveChangesAsync();

        var suffixe = Guid.NewGuid().ToString("N")[..8];
        var soumettre = services.GetRequiredService<SoumettreDemandeImportCommand>();
        var soumission = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(
            societe.Id, auteurId, [$"REC1-{suffixe}", $"REC2-{suffixe}", $"REC3-{suffixe}"]));
        Assert.Equal(3, soumission.TicketsCrees.Count);
        var (ticketValide, ticketRejete, ticketComplement) =
            (soumission.TicketsCrees[0], soumission.TicketsCrees[1], soumission.TicketsCrees[2]);

        // --- BL2 : rejeté par le Pilote Manifeste, motif obligatoire (IMP-04). ---
        var rejeter = services.GetRequiredService<RejeterTicketImportCommand>();
        await rejeter.ExecuterAsync(agentId, ticketRejete.Id, "BL illisible sur le document joint");

        // --- BL3 : complément demandé, déposé par le client, puis validé (IMP-05). ---
        var complement = services.GetRequiredService<GestionComplementManifesteCommand>();
        var valider = services.GetRequiredService<ValiderTicketImportCommand>();
        await complement.DemanderComplementAsync(agentId, ticketComplement.Id, "Facture d'origine manquante");
        var ticketComplementApresDemande = await dbContext.Tickets.FindAsync(ticketComplement.Id);
        Assert.Equal(StatutTicket.EnAttenteClient, ticketComplementApresDemande!.Statut);
        await complement.DeposerComplementAsync(auteurId, ticketComplement.Id);
        await valider.ExecuterAsync(agentId, ticketComplement.Id);

        // --- BL1 : validé et mené jusqu'à clôture (circuit Client Normal complet, IMP-06, IMP-10→14). ---
        await valider.ExecuterAsync(agentId, ticketValide.Id);

        var demanderFacture = services.GetRequiredService<DemanderFactureCommand>();
        await demanderFacture.ExecuterAsync(auteurId, ticketValide.Id);

        var editerFacture = services.GetRequiredService<EditerFactureCommand>();
        using (var facture = Contenu("facture"))
        {
            // Parcours TO BE : proforma déposée par le Pilote Facturation, validée par le client, puis facture.
            var proforma1 = Contenu("proforma");
            await services.GetRequiredService<CIT.Plateforme.Application.BillingImport.GestionProformaCommand>().EmettreAsync(
                agentId, ticketValide.Id, "proforma.pdf", "application/pdf", proforma1, proforma1.Length, Regles, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)));
            await services.GetRequiredService<CIT.Plateforme.Application.BillingImport.GestionProformaCommand>().ValiderAsync(auteurId, ticketValide.Id);
            await editerFacture.ExecuterAsync(agentId, ticketValide.Id, "facture.pdf", "application/pdf", facture, facture.Length, Regles);
        }

        var deposerPreuve = services.GetRequiredService<DeposerPreuvePaiementCommand>();
        using (var preuve = Contenu("preuve"))
        {
            await deposerPreuve.ExecuterAsync(auteurId, ticketValide.Id, "preuve.pdf", "application/pdf", preuve, preuve.Length, Regles);
        }

        var validerPreuve = services.GetRequiredService<ValiderPreuvePaiementCommand>();
        await validerPreuve.ValiderAsync(agentId, ticketValide.Id);

        var controleEtCloturer = services.GetRequiredService<DeposerDocumentsControleEtCloturerCommand>();
        using (var badt = Contenu("badt"))
        {
            await controleEtCloturer.ExecuterAsync(agentId, ticketValide.Id, "badt.pdf", "application/pdf", badt, badt.Length, Regles);
        }

        // --- Assertions finales : les trois BL du même dossier n'ont pas interféré entre eux. ---
        var finValide = await dbContext.Tickets.FindAsync(ticketValide.Id);
        var finRejete = await dbContext.Tickets.FindAsync(ticketRejete.Id);
        var finComplement = await dbContext.Tickets.FindAsync(ticketComplement.Id);

        Assert.Equal(StatutTicket.Cloture, finValide!.Statut);
        Assert.Equal(MotifClotureType.Normale, finValide.MotifCloture);

        Assert.Equal(StatutTicket.Annule, finRejete!.Statut);
        Assert.Equal(TypeAnnulationImport.RejetManifeste, finRejete.TypeAnnulationImport);

        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, finComplement!.Statut);
        Assert.NotEqual(StatutTicket.Annule, finComplement.Statut);
        Assert.NotEqual(StatutTicket.Cloture, finComplement.Statut);

        Assert.Equal(ticketValide.DossierId, finRejete.DossierId);
        Assert.Equal(ticketValide.DossierId, finComplement.DossierId);
    }
}

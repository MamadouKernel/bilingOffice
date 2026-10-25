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
/// [TEST-CDC] Rejeu intégral de REC-SCE-02 (docs/04-matrice-recette.md) : « Facture éditée →
/// passage direct au Contrôle Documentaire → remise des documents sans étape de paiement. » CDC :
/// IMP-20, IMP-21, IMP-22. Bout-en-bout sur l'application composée réelle (DI + SQL Server réel).
/// </summary>
public sealed class RecSce02ImportEnCompteTests(PlateformeWebApplicationFactory factory) : IClassFixture<PlateformeWebApplicationFactory>
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
    public async Task Facture_editee_passe_directement_au_controle_documentaire_sans_etape_de_paiement_puis_le_client_recupere_les_documents()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<PlateformeDbContext>();
        await AssurerFilesAsync(dbContext);

        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", TypeClient.EnCompte, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, auteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        dbContext.Habilitations.AddRange(
            HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.BillingImportManifesteInstruire),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingImportFacturationEditer),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingImportControleDocumentaireTraiter),
            HabilitationUtilisateur.Attribuer(agentId, Permission.DocumentDeposer));
        await dbContext.SaveChangesAsync();

        async Task PrendreEnChargeAsync(Guid tId) =>
            await services.GetRequiredService<CIT.Plateforme.Application.Tickets.PrendreEnChargeEtapeBillingCommand>()
                .ExecuterAsync(agentId, tId, [Permission.BillingImportManifesteInstruire, Permission.BillingImportFacturationEditer, Permission.BillingImportControleDocumentaireTraiter]);

        var suffixe = Guid.NewGuid().ToString("N")[..8];
        var soumettre = services.GetRequiredService<SoumettreDemandeImportCommand>();
        var soumission = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societe.Id, auteurId, [$"REC2-{suffixe}"]));
        var ticket = soumission.TicketsCrees[0];

        var valider = services.GetRequiredService<ValiderTicketImportCommand>();
        await PrendreEnChargeAsync(ticket.Id);
        await valider.ExecuterAsync(agentId, ticket.Id);

        var editerFacture = services.GetRequiredService<EditerFactureCommand>();
        using (var facture = Contenu("facture"))
        {
            // Parcours TO BE : proforma déposée par le Pilote Facturation, validée par le client, puis facture.
            var proforma1 = Contenu("proforma");
            await PrendreEnChargeAsync(ticket.Id);
            await services.GetRequiredService<CIT.Plateforme.Application.BillingImport.GestionProformaCommand>().EmettreAsync(
                agentId, ticket.Id, "proforma.pdf", "application/pdf", proforma1, proforma1.Length, Regles, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)));
            await services.GetRequiredService<CIT.Plateforme.Application.BillingImport.GestionProformaCommand>().ValiderAsync(auteurId, ticket.Id);
            // La validation client de la proforma laisse le ticket EnCours, toujours affecté à l'agent.
            await editerFacture.ExecuterAsync(agentId, ticket.Id, "facture.pdf", "application/pdf", facture, facture.Length, Regles);
        }

        // Passage direct au Contrôle Documentaire, aucune étape de paiement (IMP-20, IMP-21).
        var ticketApresFacture = await dbContext.Tickets.FindAsync(ticket.Id);
        var fileApresFacture = await dbContext.Files.FindAsync(ticketApresFacture!.FileCouranteId);
        Assert.Equal(NomsFiles.ControleDocumentaire, fileApresFacture!.Nom);

        var controleEtCloturer = services.GetRequiredService<DeposerDocumentsControleEtCloturerCommand>();
        using (var badt = Contenu("badt"))
        {
            await PrendreEnChargeAsync(ticket.Id);
            await controleEtCloturer.ExecuterAsync(agentId, ticket.Id, "badt.pdf", "application/pdf", badt, badt.Length, Regles);
        }

        var ticketCloture = await dbContext.Tickets.FindAsync(ticket.Id);
        Assert.Equal(StatutTicket.Cloture, ticketCloture!.Statut);

        // Remise des documents (IMP-22) : le client récupère l'ensemble (proforma + facture + BADT).
        var consulterDocuments = services.GetRequiredService<ConsulterDocumentsTicketClotureQuery>();
        var documents = await consulterDocuments.ExecuterAsync(auteurId, ticket.Id);
        Assert.Equal(3, documents.Count);
        Assert.Contains(documents, d => d.NomFichier == "proforma.pdf");
        Assert.Contains(documents, d => d.NomFichier == "facture.pdf");
        Assert.Contains(documents, d => d.NomFichier == "badt.pdf");
    }
}

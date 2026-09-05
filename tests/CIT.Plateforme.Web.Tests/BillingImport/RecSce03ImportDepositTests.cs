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
/// [TEST-CDC] Rejeu intégral de REC-SCE-03 (docs/04-matrice-recette.md) : « Facture → lettrage
/// Comptabilité → Contrôle Documentaire → clôture. » CDC : IMP-30, IMP-31, IMP-32. Bout-en-bout sur
/// l'application composée réelle (DI + SQL Server réel).
/// </summary>
public sealed class RecSce03ImportDepositTests(PlateformeWebApplicationFactory factory) : IClassFixture<PlateformeWebApplicationFactory>
{
    private static readonly ReglesDepotDocument Regles = new(["pdf", "jpg"], TailleMaxOctets: 1024);

    private static async Task AssurerFilesAsync(PlateformeDbContext dbContext)
    {
        var nomsRequis = new[]
        {
            SoumettreDemandeImportCommand.FilePiloteManifeste, NomsFiles.Facturation,
            NomsFiles.ValidationPaiement, NomsFiles.ComptabiliteClient, NomsFiles.ControleDocumentaire,
            NomsFiles.FileIes,
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
    public async Task Facture_puis_lettrage_confirme_puis_controle_documentaire_puis_cloture()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<PlateformeDbContext>();
        await AssurerFilesAsync(dbContext);

        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", TypeClient.Deposit, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, auteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        dbContext.Habilitations.AddRange(
            HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.BillingImportManifesteInstruire),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingImportFacturationEditer),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingImportComptabiliteConfirmerLettrage),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingImportControleDocumentaireTraiter),
            HabilitationUtilisateur.Attribuer(agentId, Permission.DocumentDeposer));
        await dbContext.SaveChangesAsync();

        var suffixe = Guid.NewGuid().ToString("N")[..8];
        var soumettre = services.GetRequiredService<SoumettreDemandeImportCommand>();
        var soumission = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societe.Id, auteurId, [$"REC3-{suffixe}"]));
        var ticket = soumission.TicketsCrees[0];

        var valider = services.GetRequiredService<ValiderTicketImportCommand>();
        await valider.ExecuterAsync(agentId, ticket.Id);

        var editerFacture = services.GetRequiredService<EditerFactureCommand>();
        using (var facture = Contenu("facture"))
        {
            await editerFacture.ExecuterAsync(agentId, ticket.Id, "facture.pdf", "application/pdf", facture, facture.Length, Regles);
        }

        var ticketApresFacture = await dbContext.Tickets.FindAsync(ticket.Id);
        var fileApresFacture = await dbContext.Files.FindAsync(ticketApresFacture!.FileCouranteId);
        Assert.Equal(NomsFiles.ComptabiliteClient, fileApresFacture!.Nom);

        var confirmerLettrage = services.GetRequiredService<ConfirmerLettrageCommand>();
        await confirmerLettrage.ConfirmerAsync(agentId, ticket.Id);

        var ticketApresLettrage = await dbContext.Tickets.FindAsync(ticket.Id);
        var fileApresLettrage = await dbContext.Files.FindAsync(ticketApresLettrage!.FileCouranteId);
        Assert.Equal(NomsFiles.ControleDocumentaire, fileApresLettrage!.Nom);

        var controleEtCloturer = services.GetRequiredService<DeposerDocumentsControleEtCloturerCommand>();
        using (var badt = Contenu("badt"))
        {
            await controleEtCloturer.ExecuterAsync(agentId, ticket.Id, "badt.pdf", "application/pdf", badt, badt.Length, Regles);
        }

        var ticketFinal = await dbContext.Tickets.FindAsync(ticket.Id);
        Assert.Equal(StatutTicket.Cloture, ticketFinal!.Statut);
        Assert.Equal(MotifClotureType.Normale, ticketFinal.MotifCloture);
    }
}

using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
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
/// [TEST-CDC] Rejeu intégral de REC-SCE-04 (docs/04-matrice-recette.md) : « Traitement parallèle
/// Manifeste/CD avec champs réservés par rôle ; jointure ; remise des documents ; clôture. » CDC :
/// IMP-40, IMP-41, IMP-42, SEC-02. Bout-en-bout sur l'application composée réelle (DI + SQL Server
/// réel), y compris le mécanisme générique de confidentialité par branche (PC-05 : liste exacte des
/// champs non résolue, hors périmètre de ce rejeu structurel).
/// </summary>
public sealed class RecSce04ImportIesTests(PlateformeWebApplicationFactory factory) : IClassFixture<PlateformeWebApplicationFactory>
{
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

    [Fact]
    public async Task Traitement_parallele_avec_champs_reserves_par_role_jointure_et_cloture_sont_rejoues()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<PlateformeDbContext>();
        await AssurerFilesAsync(dbContext);

        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", TypeClient.IES, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        var agentManifesteId = Guid.NewGuid();
        var agentControleDocumentaireId = Guid.NewGuid();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, auteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        dbContext.Habilitations.AddRange(
            HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentManifesteId), Permission.BillingImportManifesteInstruire),
            HabilitationUtilisateur.Attribuer(agentManifesteId, Permission.BillingImportIesManifesteInstruire),
            HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentControleDocumentaireId), Permission.BillingImportIesControleDocumentaireInstruire));
        await dbContext.SaveChangesAsync();

        async Task PrendreEnChargeAsync(Guid tId) =>
            await services.GetRequiredService<CIT.Plateforme.Application.Tickets.PrendreEnChargeEtapeBillingCommand>()
                .ExecuterAsync(agentManifesteId, tId, [Permission.BillingImportManifesteInstruire]);

        var suffixe = Guid.NewGuid().ToString("N")[..8];
        var soumettre = services.GetRequiredService<SoumettreDemandeImportCommand>();
        var soumission = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societe.Id, auteurId, [$"REC4-{suffixe}"]));
        var ticket = soumission.TicketsCrees[0];

        var valider = services.GetRequiredService<ValiderTicketImportCommand>();
        await PrendreEnChargeAsync(ticket.Id);
        await valider.ExecuterAsync(agentManifesteId, ticket.Id);

        var ticketAiguille = await dbContext.Tickets.FindAsync(ticket.Id);
        var fileAiguillage = await dbContext.Files.FindAsync(ticketAiguille!.FileCouranteId);
        Assert.Equal(NomsFiles.FileIes, fileAiguillage!.Nom);

        var instruire = services.GetRequiredService<InstruireBrancheIesCommand>();
        var constaterJointure = services.GetRequiredService<ConstaterJointureIesEtFinaliserCommand>();

        // Champs réservés par rôle (mécanisme générique, PC-05) : chaque pilote ne peut agir que sur
        // sa propre branche.
        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            instruire.ExecuterAsync(agentManifesteId, ticket.Id, BrancheIes.ControleDocumentaire));
        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            instruire.ExecuterAsync(agentControleDocumentaireId, ticket.Id, BrancheIes.Manifeste));

        // Une seule branche achevée → jointure non atteinte, aucune restitution.
        await instruire.ExecuterAsync(agentManifesteId, ticket.Id, BrancheIes.Manifeste);
        await Assert.ThrowsAsync<JointureIesIncompleteException>(() => constaterJointure.ExecuterAsync(agentManifesteId, ticket.Id));

        // Seconde branche achevée → jointure atteinte, remise des documents et clôture (IMP-42).
        await instruire.ExecuterAsync(agentControleDocumentaireId, ticket.Id, BrancheIes.ControleDocumentaire);
        await constaterJointure.ExecuterAsync(agentControleDocumentaireId, ticket.Id);

        var ticketFinal = await dbContext.Tickets.FindAsync(ticket.Id);
        Assert.Equal(StatutTicket.Cloture, ticketFinal!.Statut);
        Assert.Equal(MotifClotureType.Normale, ticketFinal.MotifCloture);
    }
}

using CIT.Plateforme.Application.BillingExport;
using CIT.Plateforme.Application.Exploitation;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.BillingExport;

/// <summary>
/// Écart 06/10 #2 (EXPORT) — paiement déclaratif paramétrable. Symétrique de l'import : client
/// déclare « Règlement effectué », facturier confirme → Contrôle Documentaire export. Tests d'une
/// même classe séquentiels (xUnit) : manipulation sûre du paramètre global.
/// </summary>
[Collection(Support.CollectionParametresCourants.Nom)]
public sealed class PaiementDeclaratifExportParametrableTests : IDisposable
{
    public void Dispose() => ParametresCourants.Reinitialiser();

    private sealed record Contexte(
        PlateformeDbContext Db,
        SoumettreDemandeExportCommand Soumettre,
        ValiderTicketExportCommand Valider,
        ReglementDeclaratifExportCommand Reglement,
        Guid AgentId,
        Guid SocieteId,
        Guid AuteurId);

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

        db.Files.AddRange(
            FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.BackOfficeExport),
            FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.Facturation),
            FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.ValidationPaiement),
            FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.ControleDocumentaire));

        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "SocieteExportDecl", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        db.Societes.Add(societe);
        db.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, auteurId, NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        db.Habilitations.AddRange(
            HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, agentId), Permission.BillingExportBackOfficeInstruire),
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingExportFacturationEditer));
        await db.SaveChangesAsync();

        return new Contexte(
            db,
            new SoumettreDemandeExportCommand(tickets, dossiers, new SocieteClienteRepository(db), new UtilisateurSocieteRepository(db), files,
                new NumerotationEnMemoirePourTests(), journalisation, horloge, unite),
            new ValiderTicketExportCommand(tickets, dossiers, files, new SuiviBrancheIesRepository(db), autorisation, journalisation, horloge, unite),
            new ReglementDeclaratifExportCommand(tickets, files, cloisonnement, autorisation, journalisation, horloge, unite),
            agentId, societe.Id, auteurId);
    }

    private static async Task<Guid> TicketEnFacturationAsync(Contexte c, string booking)
    {
        var soumission = await c.Soumettre.ExecuterAsync(new SoumettreDemandeExportRequete(c.SocieteId, c.AuteurId, [booking]));
        var id = soumission.TicketsCrees[0].Id;
        await c.Valider.ExecuterAsync(c.AgentId, id);
        return id;
    }

    [Fact]
    public async Task En_mode_declaratif_le_client_declare_puis_le_facturier_confirme_export()
    {
        ParametresCourants.Definir(CatalogueParametresPlateforme.ModeConfirmationPaiement, 1);
        var c = await ConstruireAsync();
        var ticketId = await TicketEnFacturationAsync(c, "BK-DECL-1");

        // Après facture, le ticket d'un client normal est en Validation Paiement : on reproduit cet état.
        var vp = c.Db.Files.Single(f => f.Module == SoumettreDemandeExportCommand.Module && f.Nom == NomsFilesExport.ValidationPaiement);
        (await c.Db.Tickets.FindAsync(ticketId))!.Transferer(vp.Id, null, DateTime.UtcNow);
        await c.Db.SaveChangesAsync();

        // Déclaration : le ticket REVIENT en file Facturation (doc §4.6).
        await c.Reglement.DeclarerAsync(c.AuteurId, ticketId);
        var ticket = (await c.Db.Tickets.FindAsync(ticketId))!;
        Assert.NotNull(ticket.ReglementDeclareUtc);
        Assert.False(ticket.EstEnEtatEngageant);
        Assert.Equal(NomsFilesExport.Facturation, (await c.Db.Files.FindAsync(ticket.FileCouranteId))!.Nom);

        await c.Reglement.ConfirmerAsync(c.AgentId, ticketId);
        ticket = (await c.Db.Tickets.FindAsync(ticketId))!;
        Assert.True(ticket.EstEnEtatEngageant);
        Assert.Equal(NomsFilesExport.ControleDocumentaire, (await c.Db.Files.FindAsync(ticket.FileCouranteId))!.Nom);
    }

    [Fact]
    public async Task En_mode_preuve_caisse_la_declaration_export_est_refusee()
    {
        ParametresCourants.Definir(CatalogueParametresPlateforme.ModeConfirmationPaiement, 0);
        var c = await ConstruireAsync();
        var ticketId = await TicketEnFacturationAsync(c, "BK-DECL-2");

        await Assert.ThrowsAsync<InvalidOperationException>(() => c.Reglement.DeclarerAsync(c.AuteurId, ticketId));
    }
}

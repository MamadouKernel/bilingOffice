using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.Documents;
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

namespace CIT.Plateforme.Application.Tests.BillingImport;

/// <summary>
/// Écart 06/10 #2 — mode de confirmation du paiement PARAMÉTRABLE. Par défaut : preuve + caisse
/// (comportement historique, couvert ailleurs). En mode « déclaratif » : le client déclare
/// « Règlement effectué » sans preuve, le facturier confirme, et le ticket passe en Contrôle
/// Documentaire (engageant). Les tests d'une même classe s'exécutent séquentiellement (xUnit) :
/// la manipulation du paramètre global <see cref="ParametresCourants"/> est donc sûre ici.
/// </summary>
[Collection(Support.CollectionParametresCourants.Nom)]
public sealed class PaiementDeclaratifParametrableTests : IDisposable
{
    public void Dispose() => ParametresCourants.Reinitialiser();

    private sealed record Contexte(
        PlateformeDbContext Db,
        SoumettreDemandeImportCommand Soumettre,
        ValiderTicketImportCommand Valider,
        ReglementDeclaratifCommand Reglement,
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
            HabilitationUtilisateur.Attribuer(agentId, Permission.BillingImportFacturationEditer));
        await db.SaveChangesAsync();

        return new Contexte(
            db,
            new SoumettreDemandeImportCommand(tickets, dossiers, new SocieteClienteRepository(db), new UtilisateurSocieteRepository(db), files,
                new NumerotationEnMemoirePourTests(), journalisation, horloge, unite),
            new ValiderTicketImportCommand(tickets, dossiers, files, new SuiviBrancheIesRepository(db), autorisation, journalisation, horloge, unite),
            new ReglementDeclaratifCommand(tickets, files, cloisonnement, autorisation, journalisation, horloge, unite),
            agentId, societe.Id, auteurId);
    }

    private static async Task<Guid> TicketEnFacturationAsync(Contexte c, string bl)
    {
        var soumission = await c.Soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(c.SocieteId, c.AuteurId, [bl]));
        var id = soumission.TicketsCrees[0].Id;
        await PriseEnChargeDeTest.PrendreAsync(c.Db, id, c.AgentId);
        await c.Valider.ExecuterAsync(c.AgentId, id);
        return id;
    }

    [Fact]
    public void Le_mode_par_defaut_est_preuve_et_caisse()
    {
        Assert.Equal(0, CatalogueParametresPlateforme.Trouver(CatalogueParametresPlateforme.ModeConfirmationPaiement)!.Defaut);
        ParametresCourants.Reinitialiser();
        Assert.False(ParametresCourants.PaiementDeclaratif);
    }

    [Fact]
    public async Task En_mode_declaratif_le_client_declare_puis_le_facturier_confirme()
    {
        ParametresCourants.Definir(CatalogueParametresPlateforme.ModeConfirmationPaiement, 1);
        var c = await ConstruireAsync();
        var ticketId = await TicketEnFacturationAsync(c, "BL-DECL-1");

        // Après facture, le ticket d'un client normal est en Validation Paiement : on reproduit cet état.
        var vp = c.Db.Files.Single(f => f.Module == SoumettreDemandeImportCommand.Module && f.Nom == NomsFiles.ValidationPaiement);
        (await c.Db.Tickets.FindAsync(ticketId))!.Transferer(vp.Id, null, DateTime.UtcNow);
        await c.Db.SaveChangesAsync();

        // Le client déclare « Règlement effectué » : pas de preuve, pas encore engageant, et le
        // ticket REVIENT en file Facturation (doc §3.6) pour que le facturier le confirme.
        await c.Reglement.DeclarerAsync(c.AuteurId, ticketId);
        var ticket = (await c.Db.Tickets.FindAsync(ticketId))!;
        Assert.NotNull(ticket.ReglementDeclareUtc);
        Assert.False(ticket.EstEnEtatEngageant);
        Assert.Equal(NomsFiles.Facturation, (await c.Db.Files.FindAsync(ticket.FileCouranteId))!.Nom);

        // Le facturier confirme : ticket engageant et transféré au Contrôle Documentaire.
        await PriseEnChargeDeTest.PrendreAsync(c.Db, ticketId, c.AgentId);
        await c.Reglement.ConfirmerAsync(c.AgentId, ticketId);
        ticket = (await c.Db.Tickets.FindAsync(ticketId))!;
        Assert.True(ticket.EstEnEtatEngageant);
        Assert.Equal(NomsFiles.ControleDocumentaire, (await c.Db.Files.FindAsync(ticket.FileCouranteId))!.Nom);
    }

    [Fact]
    public async Task En_mode_preuve_caisse_la_declaration_est_refusee()
    {
        ParametresCourants.Definir(CatalogueParametresPlateforme.ModeConfirmationPaiement, 0);
        var c = await ConstruireAsync();
        var ticketId = await TicketEnFacturationAsync(c, "BL-DECL-2");

        await Assert.ThrowsAsync<InvalidOperationException>(() => c.Reglement.DeclarerAsync(c.AuteurId, ticketId));
    }
}

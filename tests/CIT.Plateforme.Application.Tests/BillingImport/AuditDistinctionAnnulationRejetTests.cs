using CIT.Plateforme.Application.BillingImport;
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

namespace CIT.Plateforme.Application.Tests.BillingImport;

/// <summary>
/// [AUDIT — TEST DE PREUVE] Section 14 : vérifie, via les commandes RÉELLES (pas une manipulation
/// directe du domaine) que l'annulation client (F001, <see cref="AnnulerTicketCommand"/>, CYC-01)
/// et le rejet Pilote Manifeste (Feature 003, <see cref="RejeterTicketImportCommand"/>, IMP-04)
/// aboutissent bien au même <see cref="StatutTicket.Annule"/> mais restent distinguables via
/// <see cref="Ticket.TypeAnnulationImport"/>, resté <c>null</c> pour une annulation client ordinaire.
/// </summary>
public sealed class AuditDistinctionAnnulationRejetTests
{
    [Fact]
    public async Task Annulation_client_ordinaire_laisse_TypeAnnulationImport_null_contrairement_au_rejet_manifeste()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var numerotation = new NumerotationEnMemoirePourTests();
        var ticketRepository = new TicketRepository(dbContext);
        var dossierRepository = new DossierRepository(dbContext);
        var fileRepository = new FileDeTraitementRepository(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var cloisonnement = new CloisonnementService(ticketRepository, CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAcces(dbContext));

        dbContext.Files.Add(FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, SoumettreDemandeImportCommand.FilePiloteManifeste));
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, auteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.BillingImportManifesteInstruire));
        await dbContext.SaveChangesAsync();

        var soumettre = new SoumettreDemandeImportCommand(
            ticketRepository, dossierRepository, new SocieteClienteRepository(dbContext),
            new UtilisateurSocieteRepository(dbContext), fileRepository, numerotation, journalisation, horloge, uniteDeTravail);
        var annulerClient = new AnnulerTicketCommand(ticketRepository, autorisation, journalisation, cloisonnement, new EtapeBloquanteAnnulationRepository(dbContext), new PassageEnFileRepository(dbContext), horloge, uniteDeTravail);
        var rejeterManifeste = new RejeterTicketImportCommand(ticketRepository, autorisation, journalisation, horloge, uniteDeTravail);

        var soumission = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societe.Id, auteurId, ["BL-ANNUL-CLIENT", "BL-REJET-MANIFESTE"]));
        var (ticketAnnuleParClient, ticketRejeteParManifeste) = (soumission.TicketsCrees[0], soumission.TicketsCrees[1]);

        // Annulation client ordinaire (F001, réutilisée telle quelle — pas d'étape bloquante
        // atteinte). Le cloisonnement porte sur la société (Dossier.ClientId), pas sur l'auteur
        // individuel — même convention que DemanderFactureCommand/DeposerPreuvePaiementCommand.
        await annulerClient.AnnulerLibrementAsync(auteurId, ticketAnnuleParClient.Id, "Je n'ai plus besoin de ce dossier");

        // Rejet Pilote Manifeste (Feature 003) — l'agent prend d'abord en charge le ticket (option B).
        await PriseEnChargeDeTest.PrendreAsync(dbContext, ticketRejeteParManifeste.Id, agentId);
        await rejeterManifeste.ExecuterAsync(agentId, ticketRejeteParManifeste.Id, "BL illisible");

        var finAnnuleParClient = await dbContext.Tickets.FindAsync(ticketAnnuleParClient.Id);
        var finRejeteParManifeste = await dbContext.Tickets.FindAsync(ticketRejeteParManifeste.Id);

        Assert.Equal(StatutTicket.Annule, finAnnuleParClient!.Statut);
        Assert.Null(finAnnuleParClient.TypeAnnulationImport);

        Assert.Equal(StatutTicket.Annule, finRejeteParManifeste!.Statut);
        Assert.Equal(TypeAnnulationImport.RejetManifeste, finRejeteParManifeste.TypeAnnulationImport);

        // Les deux BL sont-ils libérés pour une nouvelle soumission, quelle que soit la voie
        // d'annulation empruntée (UNI-01/CAS4-6) ?
        var reprise = await soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(
            societe.Id, auteurId, ["BL-ANNUL-CLIENT", "BL-REJET-MANIFESTE"]));
        Assert.Equal(2, reprise.TicketsCrees.Count);
        Assert.Empty(reprise.BlRefuses);
    }
}

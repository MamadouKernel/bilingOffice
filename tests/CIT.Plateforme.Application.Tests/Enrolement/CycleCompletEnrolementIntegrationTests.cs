using System.Text;
using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Application.Enrolement;
using CIT.Plateforme.Application.Messagerie;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Documents;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Enrolement;

// FEATURE 008 (Enrôlement Transporteurs) — T037. Cycle complet réel (persistance EF) : soumission
// → prise en charge → demande de complément → réponse (message + pièce) → reprise → visible en
// file. Aucune commande n'est recréée : tout passe par le socle F001 déjà éprouvé (research.md D3/D7).
public sealed class CycleCompletEnrolementIntegrationTests
{
    [Fact]
    public async Task Soumission_PriseEnCharge_Complement_Reponse_Reprise_TicketRedevientEnCours()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uow = new UniteDeTravail(dbContext);
        var numerotation = new NumerotationEnMemoirePourTests();
        var autorisation = new AutorisationService(dbContext);
        var ticketRepo = new TicketRepository(dbContext);
        var dossierRepo = new DossierRepository(dbContext);
        var fileRepo = new FileDeTraitementRepository(dbContext);
        var periodeRepo = new PeriodeAttenteClientRepository(dbContext);
        var messageRepo = new MessageRepository(dbContext);
        var cloisonnement = new CloisonnementService(ticketRepo, dossierRepo);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var stockage = new StockageDocumentsFichiers(configuration);
        var antivirus = new AucuneAnalyseAntivirusConfiguree();
        var documentRepo = new DocumentRepository(dbContext);

        var fileAgentGate = FileDeTraitement.Creer("Enrolement", "Agent Gate");
        dbContext.Files.Add(fileAgentGate);

        var transporteurId = Guid.NewGuid();
        var agentGateId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentGateId, Permission.EnrolementInstruire, fileAgentGate.Id));
        await dbContext.SaveChangesAsync();

        var soumettre = new SoumettreEnrolementCommand(ticketRepo, dossierRepo, fileRepo, numerotation, journalisation, horloge, autorisation, uow);
        var prendreEnCharge = new PrendreEnChargeCommand(ticketRepo, autorisation, journalisation, horloge, uow);
        var attenteClient = new AttenteClientCommand(ticketRepo, periodeRepo, autorisation, journalisation, horloge, uow);
        var publierMessage = new PublierMessageCommand(messageRepo, ticketRepo, autorisation, cloisonnement, journalisation, horloge, uow);
        var deposerDocument = new DeposerDocumentCommand(documentRepo, ticketRepo, stockage, antivirus, autorisation, cloisonnement, journalisation, horloge, uow);

        // Un compte transporteur/chauffeur — Nature=Client, sans société (CLAR-ENR-01).
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(transporteurId, Permission.EnrolementSoumettre));
        await dbContext.SaveChangesAsync();

        // 1. Soumission (ENR-01).
        var resultat = await soumettre.ExecuterAsync(transporteurId, new SoumettreEnrolementRequete("Enrôlement chauffeur", "Camion frigorifique."));
        Assert.NotEmpty(resultat.NumeroTicket);

        // Prise en charge (Agent Gate).
        await prendreEnCharge.ExecuterAsync(agentGateId, resultat.TicketId);
        var ticketApresPriseEnCharge = await ticketRepo.ObtenirParIdAsync(resultat.TicketId);
        Assert.Equal(StatutTicket.EnCours, ticketApresPriseEnCharge!.Statut);

        // 2. Demande de complément (ENR-02, C8-04, réutilisation AttenteClientCommand).
        await attenteClient.MettreEnAttenteAsync(agentGateId, resultat.TicketId, "Permis de conduire manquant");
        var ticketEnAttente = await ticketRepo.ObtenirParIdAsync(resultat.TicketId);
        Assert.Equal(StatutTicket.EnAttenteClient, ticketEnAttente!.Statut);

        // Réponse du transporteur : message (générique, PublierMessageClientAsync, inchangée).
        var message = await publierMessage.PublierMessageClientAsync(transporteurId, resultat.TicketId, "Voici mon permis de conduire.");
        Assert.NotNull(message);

        // + pièce complémentaire (générique, DeposerDocumentCommand, inchangée).
        using var contenu = new MemoryStream(Encoding.UTF8.GetBytes("permis-simule"));
        var document = await deposerDocument.DeposerPourClientAsync(transporteurId, resultat.TicketId, "permis.pdf", "application/pdf", contenu, contenu.Length, new ReglesDepotDocument(["pdf"], 1024));
        Assert.Equal("permis.pdf", document.NomFichier);

        // Reprise (générique, RependreApresReponseAsync, inchangée).
        await attenteClient.RependreApresReponseAsync(resultat.TicketId);
        var ticketApresReprise = await ticketRepo.ObtenirParIdAsync(resultat.TicketId);
        Assert.Equal(StatutTicket.EnCours, ticketApresReprise!.Statut);

        // Visible en file Agent Gate.
        var ticketsDeLaFile = await ticketRepo.ListerParFileAsync(fileAgentGate.Id);
        Assert.Contains(ticketsDeLaFile, t => t.Id == resultat.TicketId);
    }
}

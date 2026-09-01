using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.AvoirExoneration;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.AvoirExoneration;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.E2E;

/// <summary>
/// FEATURE 007 — Avoir & Exonération : Recette formelle des 10 TEST-SPEC et contrôles de sécurité Anti-IDOR (T051, T052).
/// Invariant I-FINANCE : zéro calcul ERP/comptable, zéro workflow N1/N2/DAF dans l'application.
/// </summary>
public sealed class AveSpecTests : IClassFixture<PlateformeWebApplicationFactory>
{
    private readonly PlateformeWebApplicationFactory _factory;
    private static readonly DateTime MaintenantUtc = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

    public AveSpecTests(PlateformeWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Guid ClientId, Guid SocieteId, Guid AgentId, Guid AgentDecideurId)> PreparerActeursAsync(PlateformeDbContext db)
    {
        var suffixe = Guid.NewGuid().ToString("N")[..8];
        var clientId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var agentDecideurId = Guid.NewGuid();

        var soc = SocieteCliente.Soumettre(clientId, $"SOCIETE-{suffixe}", TypeClient.Normal, MaintenantUtc);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, societeId);
        soc.Valider();
        db.Societes.Add(soc);

        var rattach = UtilisateurSociete.Rattacher(clientId, societeId, MaintenantUtc, false, true);
        db.UtilisateursSociete.Add(rattach);

        // Habilitations agent
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentId, Permission.AvoirExonerationInstruire));
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentDecideurId, Permission.AvoirExonerationInstruire));
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentDecideurId, Permission.AvoirExonerationDecider));

        if (!await db.Files.AnyAsync(f => f.Module == "AvoirExoneration"))
        {
            db.Files.Add(FileDeTraitement.Creer("AvoirExoneration", "Avoir & Exonération"));
        }

        await db.SaveChangesAsync();
        return (clientId, societeId, agentId, agentDecideurId);
    }

    [Fact]
    public async Task TEST_SPEC_01_Et_02_SoumissionNominaleAvoirEtExoneration()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var soumettreCmd = scope.ServiceProvider.GetRequiredService<SoumettreDemandeAvoirExonerationCommand>();

        var (clientId, _, _, _) = await PreparerActeursAsync(db);

        // 1. Soumission Avoir
        var reqAvoir = new SoumettreDemandeAvoirExoRequete(
            TypeDemande: "Avoir",
            Objet: "Trop-perçu facturation acconage",
            Description: "Contestation détaillée avec pièces jointes",
            MontantConteste: 150000m,
            ReferenceMetier: null,
            TicketParentId: null,
            PiecesJointes: [new PieceJointeSoumission("facture.pdf", "application/pdf", [1, 2, 3, 4])]
        );

        var repAvoir = await soumettreCmd.ExecuterAsync(clientId, reqAvoir);
        Assert.NotNull(repAvoir.NumeroTicket);
        Assert.Contains("AVO", repAvoir.NumeroTicket);

        // 2. Soumission Exonération
        var reqExo = new SoumettreDemandeAvoirExoRequete(
            TypeDemande: "Exoneration",
            Objet: "Exonération magasinage cause force majeure",
            Description: "Blocage douanier justifié",
            MontantConteste: 300000m,
            ReferenceMetier: null,
            TicketParentId: null,
            PiecesJointes: [new PieceJointeSoumission("attestation.pdf", "application/pdf", [5, 6, 7])]
        );

        var repExo = await soumettreCmd.ExecuterAsync(clientId, reqExo);
        Assert.NotNull(repExo.NumeroTicket);
        Assert.Contains("EXO", repExo.NumeroTicket);
    }

    [Fact]
    public async Task TEST_SPEC_03_RejetSoumissionSiZeroPieceJustificative()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var soumettreCmd = scope.ServiceProvider.GetRequiredService<SoumettreDemandeAvoirExonerationCommand>();

        var (clientId, _, _, _) = await PreparerActeursAsync(db);

        var req = new SoumettreDemandeAvoirExoRequete(
            TypeDemande: "Avoir",
            Objet: "Demande sans justificatif",
            Description: "Tentative sans pièce",
            MontantConteste: 50000m,
            ReferenceMetier: null,
            TicketParentId: null,
            PiecesJointes: []
        );

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => soumettreCmd.ExecuterAsync(clientId, req));
        Assert.Contains("justificative", ex.Message);
    }

    [Fact]
    public async Task TEST_SPEC_04_LiaisonTicketParentOptionnel()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var soumettreCmd = scope.ServiceProvider.GetRequiredService<SoumettreDemandeAvoirExonerationCommand>();
        var obtenirFicheQuery = scope.ServiceProvider.GetRequiredService<ObtenirFicheAvoirExonerationQuery>();

        var (clientId, societeId, _, _) = await PreparerActeursAsync(db);

        // Création d'un ticket parent pour la même société
        var dossierParent = Dossier.Creer($"DOS-PAR-{Guid.NewGuid():N}"[..16], societeId, MaintenantUtc, clientId);
        db.Dossiers.Add(dossierParent);
        var file = await db.Files.FirstAsync();
        var ticketParent = Ticket.Creer($"IMP-PAR-{Guid.NewGuid():N}"[..16], "BillingImport", "DemandeImport", file.Id, dossierParent.Id, MaintenantUtc, "BL-123");
        db.Tickets.Add(ticketParent);
        await db.SaveChangesAsync();

        var req = new SoumettreDemandeAvoirExoRequete(
            TypeDemande: "Avoir",
            Objet: "Avoir rattaché au dossier import",
            Description: "Explication litige",
            MontantConteste: 75000m,
            ReferenceMetier: null,
            TicketParentId: ticketParent.Id,
            PiecesJointes: [new PieceJointeSoumission("justif.pdf", "application/pdf", [1, 2, 3])]
        );

        var rep = await soumettreCmd.ExecuterAsync(clientId, req);

        var fiche = await obtenirFicheQuery.ExecuterAsync(clientId, rep.TicketId);
        Assert.NotNull(fiche.TicketParent);
        Assert.Equal(ticketParent.Id, fiche.TicketParent.TicketId);
        Assert.Equal(ticketParent.Numero, fiche.TicketParent.Numero);
    }

    [Fact]
    public async Task TEST_SPEC_05_ConsultationFileAgentAvecFiltres()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var soumettreCmd = scope.ServiceProvider.GetRequiredService<SoumettreDemandeAvoirExonerationCommand>();
        var fileQuery = scope.ServiceProvider.GetRequiredService<ConsulterFileAvoirExonerationQuery>();

        var (clientId, _, agentId, _) = await PreparerActeursAsync(db);

        var repAvoir = await soumettreCmd.ExecuterAsync(clientId, new SoumettreDemandeAvoirExoRequete(
            "Avoir", "Objet AVO", "Desc", 10000m, null, null, [new PieceJointeSoumission("doc.pdf", "app/pdf", [1])]));

        var ticketsAvoir = await fileQuery.ExecuterAsync(agentId, new CriteresFiltreFileAvoirExo("Avoir", null, null));
        Assert.Contains(ticketsAvoir, t => t.TicketId == repAvoir.TicketId);

        var ticketsExo = await fileQuery.ExecuterAsync(agentId, new CriteresFiltreFileAvoirExo("Exoneration", null, null));
        Assert.DoesNotContain(ticketsExo, t => t.TicketId == repAvoir.TicketId);
    }

    [Fact]
    public async Task TEST_SPEC_06_DemandeComplementBasculeStatutAttenteClient()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var soumettreCmd = scope.ServiceProvider.GetRequiredService<SoumettreDemandeAvoirExonerationCommand>();
        var prendreEnChargeCmd = scope.ServiceProvider.GetRequiredService<PrendreEnChargeAvoirExoCommand>();
        var demanderComplementCmd = scope.ServiceProvider.GetRequiredService<DemanderComplementAvoirExoCommand>();
        var obtenirFicheQuery = scope.ServiceProvider.GetRequiredService<ObtenirFicheAvoirExonerationQuery>();

        var (clientId, _, agentId, _) = await PreparerActeursAsync(db);

        var rep = await soumettreCmd.ExecuterAsync(clientId, new SoumettreDemandeAvoirExoRequete(
            "Avoir", "Objet Complément", "Desc", 10000m, null, null, [new PieceJointeSoumission("doc.pdf", "app/pdf", [1])]));

        await prendreEnChargeCmd.ExecuterAsync(agentId, rep.TicketId);
        await demanderComplementCmd.ExecuterAsync(agentId, rep.TicketId, "Veuillez fournir le bon à délivrer.");

        var fiche = await obtenirFicheQuery.ExecuterAsync(clientId, rep.TicketId);
        Assert.Equal(StatutTicket.EnAttenteClient.ToString(), fiche.Statut);
    }

    [Fact]
    public async Task TEST_SPEC_07_DecisionAccordAvecMontantEtCloture()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var soumettreCmd = scope.ServiceProvider.GetRequiredService<SoumettreDemandeAvoirExonerationCommand>();
        var prendreEnChargeCmd = scope.ServiceProvider.GetRequiredService<PrendreEnChargeAvoirExoCommand>();
        var deciderCmd = scope.ServiceProvider.GetRequiredService<DeciderDemandeAvoirExoCommand>();
        var obtenirFicheQuery = scope.ServiceProvider.GetRequiredService<ObtenirFicheAvoirExonerationQuery>();

        var (clientId, _, _, agentDecideurId) = await PreparerActeursAsync(db);

        var rep = await soumettreCmd.ExecuterAsync(clientId, new SoumettreDemandeAvoirExoRequete(
            "Avoir", "Objet Accord", "Desc", 200000m, null, null, [new PieceJointeSoumission("doc.pdf", "app/pdf", [1])]));

        await prendreEnChargeCmd.ExecuterAsync(agentDecideurId, rep.TicketId);

        var reqDecision = new DeciderDemandeAvoirExoRequete(
            TicketId: rep.TicketId,
            Decision: DecisionAvoirExo.Accorde,
            MotifRejet: null,
            MontantAccorde: 180000m,
            DocumentDecision: new PieceJointeSoumission("decision_accord.pdf", "application/pdf", [1, 2, 3])
        );

        await deciderCmd.ExecuterAsync(agentDecideurId, reqDecision);

        var fiche = await obtenirFicheQuery.ExecuterAsync(clientId, rep.TicketId);
        Assert.Equal(StatutTicket.Cloture.ToString(), fiche.Statut);
        Assert.Equal("Accorde", fiche.Decision);
        Assert.Equal(180000m, fiche.MontantAccorde);
        Assert.NotNull(fiche.DateDecisionUtc);
        Assert.Equal(agentDecideurId, fiche.DecideurId);
        Assert.Contains(fiche.Documents, d => d.NomFichier == "decision_accord.pdf");
    }

    [Fact]
    public async Task TEST_SPEC_08_Et_09_DecisionRejetAvecMotifObligatoire()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var soumettreCmd = scope.ServiceProvider.GetRequiredService<SoumettreDemandeAvoirExonerationCommand>();
        var prendreEnChargeCmd = scope.ServiceProvider.GetRequiredService<PrendreEnChargeAvoirExoCommand>();
        var deciderCmd = scope.ServiceProvider.GetRequiredService<DeciderDemandeAvoirExoCommand>();
        var obtenirFicheQuery = scope.ServiceProvider.GetRequiredService<ObtenirFicheAvoirExonerationQuery>();

        var (clientId, _, _, agentDecideurId) = await PreparerActeursAsync(db);

        var rep = await soumettreCmd.ExecuterAsync(clientId, new SoumettreDemandeAvoirExoRequete(
            "Exoneration", "Objet Rejet", "Desc", 50000m, null, null, [new PieceJointeSoumission("doc.pdf", "app/pdf", [1])]));

        await prendreEnChargeCmd.ExecuterAsync(agentDecideurId, rep.TicketId);

        // TEST-SPEC-09 : tentative rejet sans motif -> erreur
        var reqSansMotif = new DeciderDemandeAvoirExoRequete(
            TicketId: rep.TicketId,
            Decision: DecisionAvoirExo.Rejete,
            MotifRejet: "   ",
            MontantAccorde: null,
            DocumentDecision: null
        );

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => deciderCmd.ExecuterAsync(agentDecideurId, reqSansMotif));
        Assert.Contains("motif", ex.Message, StringComparison.OrdinalIgnoreCase);

        // TEST-SPEC-08 : rejet valide avec motif formel
        var reqValide = new DeciderDemandeAvoirExoRequete(
            TicketId: rep.TicketId,
            Decision: DecisionAvoirExo.Rejete,
            MotifRejet: "Délai légal de contestation dépassé (plus de 30 jours après facturation).",
            MontantAccorde: null,
            DocumentDecision: null
        );

        await deciderCmd.ExecuterAsync(agentDecideurId, reqValide);

        var fiche = await obtenirFicheQuery.ExecuterAsync(clientId, rep.TicketId);
        Assert.Equal(StatutTicket.Cloture.ToString(), fiche.Statut);
        Assert.Equal("Rejete", fiche.Decision);
        Assert.Equal("Délai légal de contestation dépassé (plus de 30 jours après facturation).", fiche.MotifRejet);
    }

    [Fact]
    public async Task TEST_SPEC_10_SecuriteAntiIdor_RejetAccesAutreSociete()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var soumettreCmd = scope.ServiceProvider.GetRequiredService<SoumettreDemandeAvoirExonerationCommand>();
        var obtenirFicheQuery = scope.ServiceProvider.GetRequiredService<ObtenirFicheAvoirExonerationQuery>();

        var (clientA, _, _, _) = await PreparerActeursAsync(db);
        var (clientB, _, _, _) = await PreparerActeursAsync(db);

        // Société A soumet une demande
        var repA = await soumettreCmd.ExecuterAsync(clientA, new SoumettreDemandeAvoirExoRequete(
            "Avoir", "Demande Société A", "Desc", 10000m, null, null, [new PieceJointeSoumission("doc.pdf", "app/pdf", [1])]));

        // Client B tente d'accéder à la fiche de la Société A (SEC-AVE-01) -> Refusé
        await Assert.ThrowsAsync<AccesRefuseException>(() => obtenirFicheQuery.ExecuterAsync(clientB, repA.TicketId));

        // Client B tente de lier sa nouvelle demande au ticket de Société A (SEC-AVE-02) -> Refusé
        var reqLiaisonIllegitime = new SoumettreDemandeAvoirExoRequete(
            "Avoir", "Attaque IDOR Liaison", "Desc", 10000m, null, repA.TicketId, [new PieceJointeSoumission("doc.pdf", "app/pdf", [1])]);

        await Assert.ThrowsAsync<AccesRefuseException>(() => soumettreCmd.ExecuterAsync(clientB, reqLiaisonIllegitime));
    }
}

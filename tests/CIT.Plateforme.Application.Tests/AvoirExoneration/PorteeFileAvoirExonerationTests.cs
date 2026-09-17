using CIT.Plateforme.Application.AvoirExoneration;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.AvoirExoneration;

/// <summary>
/// [P3] LA PORTÉE DE FILE SUR LE MODULE AVOIR / EXONÉRATION.
///
/// Les quatre contrôles du module appellent
/// <c>EstAutoriseAsync(agentId, AvoirExonerationInstruire, fileId: null)</c>.
///
/// L'AUDIT AVAIT LU CE <c>null</c> COMME « TOUTE FILE ». C'est l'inverse. Le prédicat du service
/// d'autorisation est <c>h.FileId == null || h.FileId == fileId</c> : passer <c>null</c> le réduit
/// à <c>h.FileId == null</c>, c'est-à-dire à la seule habilitation NON BORNÉE. Un agent habilité
/// sur la file Avoir &amp; Exonération — la seule du module — est donc REFUSÉ.
///
/// Ce n'est pas une faille : le contrôle est trop strict, pas trop permissif. C'est un défaut
/// fonctionnel, de la même forme que celui qui avait conduit l'interface à contourner
/// <c>CloisonnementService</c> : une règle qui refuse les cas légitimes finit par être contournée.
///
/// Ces tests établissent d'abord le comportement réel, avant toute correction.
/// </summary>
public sealed class PorteeFileAvoirExonerationTests
{
    private sealed record Contexte(
        PlateformeDbContext Db,
        Guid AgentBorneSurLaFile,
        Guid AgentNonBorne,
        Guid AgentAutreFile,
        Ticket Ticket);

    private static Guid AjouterAgent(PlateformeDbContext db, string cle)
    {
        var compte = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"{cle}@ave.test",
            Email = $"{cle}@ave.test",
            NomAffiche = cle,
            Nature = NatureUtilisateur.Agent,
            EstActif = true,
        };
        db.Users.Add(compte);
        db.SaveChanges();
        return compte.Id;
    }

    private static Contexte Preparer()
    {
        var db = ContexteDeTest.CreerNouveauContexte();

        var fileAve = FileDeTraitement.Creer("AvoirExoneration", "Avoir & Exonération");
        var fileAutre = FileDeTraitement.Creer("BillingOffice", "Pilote Manifeste");
        db.Files.AddRange(fileAve, fileAutre);

        var agentBorne = AjouterAgent(db, "agent-borne-ave");
        var agentNonBorne = AjouterAgent(db, "agent-non-borne");
        var agentAutreFile = AjouterAgent(db, "agent-autre-file");

        db.Habilitations.AddRange(
            // Borné à la file du module — le cas nominal d'un instructeur Avoir/Exonération.
            HabilitationUtilisateur.Attribuer(agentBorne, Permission.AvoirInstruire, fileAve.Id),
            // Non borné — un superviseur transverse.
            HabilitationUtilisateur.Attribuer(agentNonBorne, Permission.AvoirInstruire),
            // Borné à une AUTRE file — ne doit jamais instruire un avoir.
            HabilitationUtilisateur.Attribuer(agentAutreFile, Permission.AvoirInstruire, fileAutre.Id));

        var dossier = Dossier.Creer("DOS-AVE-0001", Guid.NewGuid(), DateTime.UtcNow);
        var ticket = Ticket.CreerDemandeAvoirExo(
            "AVO-P3-001", "Avoir", fileAve.Id, dossier.Id, "Objet", "Description", null, DateTime.UtcNow);
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        db.SaveChanges();

        return new Contexte(db, agentBorne, agentNonBorne, agentAutreFile, ticket);
    }

    /// <summary>
    /// L'agent habilité SUR LA FILE du module doit pouvoir prendre en charge. C'est le cas
    /// nominal, et c'est celui que le <c>null</c> refusait.
    /// </summary>
    [Fact]
    public async Task Un_agent_habilite_sur_la_file_du_module_prend_en_charge()
    {
        var c = Preparer();
        using var db = c.Db;

        var commande = new PrendreEnChargeAvoirExoCommand(
            new TicketRepository(db),
            new JournalisationService(db, new HorlogeSysteme()),
            new HorlogeSysteme(),
            new AutorisationService(db),
            new UniteDeTravail(db));

        await commande.ExecuterAsync(c.AgentBorneSurLaFile, c.Ticket.Id);

        db.ChangeTracker.Clear();
        var recharge = await new TicketRepository(db).ObtenirParIdAsync(c.Ticket.Id);
        Assert.Equal(c.AgentBorneSurLaFile, recharge!.OperateurAssigneId);
    }

    /// <summary>Un agent non borné conserve l'accès : la correction ne doit rien retirer.</summary>
    [Fact]
    public async Task Un_agent_non_borne_prend_toujours_en_charge()
    {
        var c = Preparer();
        using var db = c.Db;

        var commande = new PrendreEnChargeAvoirExoCommand(
            new TicketRepository(db),
            new JournalisationService(db, new HorlogeSysteme()),
            new HorlogeSysteme(),
            new AutorisationService(db),
            new UniteDeTravail(db));

        await commande.ExecuterAsync(c.AgentNonBorne, c.Ticket.Id);

        db.ChangeTracker.Clear();
        var recharge = await new TicketRepository(db).ObtenirParIdAsync(c.Ticket.Id);
        Assert.Equal(c.AgentNonBorne, recharge!.OperateurAssigneId);
    }

    /// <summary>
    /// Un agent habilité sur une AUTRE file reste refusé — SEC-03. C'est le témoin qui empêche la
    /// correction d'ouvrir la porte en la débloquant.
    /// </summary>
    [Fact]
    public async Task Un_agent_habilite_sur_une_autre_file_reste_refuse()
    {
        var c = Preparer();
        using var db = c.Db;

        var commande = new PrendreEnChargeAvoirExoCommand(
            new TicketRepository(db),
            new JournalisationService(db, new HorlogeSysteme()),
            new HorlogeSysteme(),
            new AutorisationService(db),
            new UniteDeTravail(db));

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => commande.ExecuterAsync(c.AgentAutreFile, c.Ticket.Id));
    }
}

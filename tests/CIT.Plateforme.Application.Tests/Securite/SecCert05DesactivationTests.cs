using System;
using System.Linq;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Audit;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Securite;

/// <summary>
/// FEATURE 011 — CERTIFICATION SEC-11 · suite TEST-SPEC « SEC-CERT-05 » (T023→T025).
///
/// Texte CDC (SEC-11, Must) : « Les comptes agents peuvent être désactivés immédiatement, SANS
/// SUPPRESSION DE LEUR HISTORIQUE. »
///
/// POURQUOI CETTE SUITE EXISTE : l'audit F011 (research.md D1) a constaté que l'indicateur
/// d'activité du compte est exercé par les suites de gestion des collaborateurs — sous l'angle
/// de l'ADMINISTRATION, jamais sous celui de l'ACCÈS, et jamais conjointement avec la
/// conservation de l'historique.
///
/// LE CAS DÉCISIF est la CONJONCTION : le CDC n'exige pas seulement la désactivation, ni
/// seulement la conservation — il exige les deux ENSEMBLE. Un test qui ne démontrerait qu'un
/// seul volet ne certifierait pas l'exigence.
///
/// AUCUN CODE PRODUIT N'EST MODIFIÉ.
///
/// HORS PÉRIMÈTRE, VOLONTAIREMENT (contracts C11-C05) : la suppression de compte n'est évoquée
/// par aucun texte du CDC — ne pas extrapoler.
///
/// RÉVISION P0-ter — CE QUI N'EST PLUS HORS PÉRIMÈTRE. Cette note excluait aussi « l'invalidation
/// d'une session déjà ouverte », au motif que le CDC n'en parlait pas. C'était une lecture trop
/// littérale : SEC-11 exige une désactivation IMMÉDIATE, et une désactivation qui laisse agir
/// jusqu'à l'expiration du cookie n'est pas immédiate. La session ouverte relève donc bien de
/// l'exigence. Elle est traitée côté serveur (GardeIdentiteService, consultée par
/// AutorisationService), ce que le VOLET 1 ci-dessous éprouve désormais réellement.
/// </summary>
public sealed class SecCert05DesactivationTests
{
    /// <summary>
    /// Le compte est INSCRIT en base, et non seulement construit en mémoire. La version d'origine
    /// se contentait de l'objet : la suite ne pouvait donc éprouver aucune décision d'autorisation,
    /// puisque du point de vue du serveur ce compte n'existait pas.
    /// </summary>
    private static ApplicationUser CreerAgent(PlateformeDbContext dbContext, bool actif)
    {
        var agent = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"{Guid.NewGuid()}@cit.test",
            Email = $"{Guid.NewGuid()}@cit.test",
            NomAffiche = "Agent de certification",
            Nature = NatureUtilisateur.Agent,
            EstActif = actif,
        };
        dbContext.Users.Add(agent);
        dbContext.SaveChanges();
        return agent;
    }

    /// <summary>Produit de l'historique attribuable à un compte : entrées d'historique de ticket
    /// et entrées de journal d'audit, toutes deux porteuses de l'auteur.</summary>
    private static async Task ProduireHistoriqueAsync(PlateformeDbContext dbContext, Guid agentId, Guid ticketId)
    {
        dbContext.HistoriqueTickets.Add(
            EntreeHistorique.Creer(ticketId, "ActionDeCertification", "detail", agentId, DateTime.UtcNow));
        dbContext.JournalAudit.Add(
            EntreeJournalAudit.Creer(agentId, "ActionDeCertification", "Ticket", ticketId, null, "apres", DateTime.UtcNow));
        await dbContext.SaveChangesAsync();
    }

    // ---- T023 : le compte actif produit de l'historique ----

    [Fact]
    public async Task Un_compte_agent_actif_produit_de_l_historique()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var agent = CreerAgent(dbContext, actif: true);
        var ticketId = Guid.NewGuid();

        await ProduireHistoriqueAsync(dbContext, agent.Id, ticketId);

        Assert.True(agent.EstActif);
        Assert.Single(dbContext.HistoriqueTickets.Where(h => h.AuteurId == agent.Id));
        Assert.Single(dbContext.JournalAudit.Where(a => a.AuteurId == agent.Id));
    }

    // ---- T024 : CAS DÉCISIF — les deux volets dans UN SEUL scénario ----

    [Fact]
    public async Task Un_compte_desactive_perd_l_acces_ET_conserve_integralement_son_historique()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var agent = CreerAgent(dbContext, actif: true);
        var ticketId = Guid.NewGuid();
        await ProduireHistoriqueAsync(dbContext, agent.Id, ticketId);

        var historiqueAvant = dbContext.HistoriqueTickets.Count(h => h.AuteurId == agent.Id);
        var auditAvant = dbContext.JournalAudit.Count(a => a.AuteurId == agent.Id);

        // Le compte détient une permission au moment où il est encore actif : sans elle, le refus
        // constaté plus bas pourrait venir de l'absence de droit et non de la désactivation.
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(agent.Id, Permission.TicketCloturer));
        await dbContext.SaveChangesAsync();

        var autorisation = new AutorisationService(dbContext);
        Assert.True(
            await autorisation.EstAutoriseAsync(agent.Id, Permission.TicketCloturer),
            "Témoin : le compte actif doit être autorisé, sinon le test ne prouverait rien.");

        // ── Désactivation : on bascule l'indicateur, on NE SUPPRIME PAS le compte ──
        agent.EstActif = false;
        await dbContext.SaveChangesAsync();

        // VOLET 1 — LA DÉSACTIVATION FERME EFFECTIVEMENT L'ACCÈS.
        //
        // CORRECTION (remédiation P0-ter). Cette assertion se réduisait à `Assert.False(agent.EstActif)` :
        // elle écrivait l'indicateur puis vérifiait qu'il valait ce qu'on venait d'y mettre, sans
        // exercer le moindre chemin d'accès. Un tel test passe quoi qu'il arrive — et c'est
        // précisément pourquoi il a laissé passer le P0-ter, où désactiver un compte n'avait aucun
        // effet sur ses permissions pendant que cette suite certifiait SEC-11.
        //
        // La certification porte désormais sur une DÉCISION SERVEUR réelle, à permission constante :
        // seule l'activité du compte a changé entre les deux appels.
        Assert.False(await autorisation.EstAutoriseAsync(agent.Id, Permission.TicketCloturer));
        await Assert.ThrowsAsync<AccesRefuseException>(
            () => autorisation.ExigerAsync(agent.Id, Permission.TicketCloturer));

        // VOLET 2 — historique intégralement conservé.
        Assert.Equal(historiqueAvant, dbContext.HistoriqueTickets.Count(h => h.AuteurId == agent.Id));
        Assert.Equal(auditAvant, dbContext.JournalAudit.Count(a => a.AuteurId == agent.Id));
        Assert.All(
            dbContext.HistoriqueTickets.Where(h => h.AuteurId == agent.Id),
            h => Assert.Equal(agent.Id, h.AuteurId));
        Assert.All(
            dbContext.JournalAudit.Where(a => a.AuteurId == agent.Id),
            a => Assert.Equal(agent.Id, a.AuteurId));
    }

    /// <summary>
    /// La désactivation ne doit ni anonymiser, ni détacher les traces : l'auteur reste
    /// nommément identifiable après désactivation — c'est ce qui distingue une désactivation
    /// d'une suppression.
    /// </summary>
    [Fact]
    public async Task La_desactivation_n_anonymise_ni_ne_detache_les_traces()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var agent = CreerAgent(dbContext, actif: true);
        await ProduireHistoriqueAsync(dbContext, agent.Id, Guid.NewGuid());

        agent.EstActif = false;

        var traces = dbContext.JournalAudit.Where(a => a.AuteurId == agent.Id).ToList();
        Assert.NotEmpty(traces);
        Assert.All(traces, t => Assert.NotNull(t.AuteurId));
        Assert.All(traces, t => Assert.NotEqual(Guid.Empty, t.AuteurId));
    }

    /// <summary>
    /// T025 — GARDE D'ABSTENTION : l'indicateur d'activité est le SEUL mécanisme mobilisé. Le
    /// compte n'est jamais supprimé, et aucun mécanisme d'invalidation de session n'est
    /// introduit — le CDC n'en parle pas (contracts C11-C05).
    /// </summary>
    [Fact]
    public void La_desactivation_repose_sur_un_indicateur_et_non_sur_une_suppression()
    {
        var proprietes = typeof(ApplicationUser).GetProperties().Select(p => p.Name).ToList();

        Assert.Contains("EstActif", proprietes);
        Assert.DoesNotContain(proprietes, p => p.Contains("Supprim", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(proprietes, p => p.Contains("Anonymis", StringComparison.OrdinalIgnoreCase));
    }
}

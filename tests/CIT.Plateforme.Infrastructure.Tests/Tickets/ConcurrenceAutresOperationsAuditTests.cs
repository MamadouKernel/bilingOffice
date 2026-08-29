using CIT.Plateforme.Domain;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Tickets;

/// <summary>
/// [AUDIT post-implementation-audit, section 5] Tests écrits pendant l'audit pour vérifier
/// empiriquement — sur SQL Server réel — que les autres mutations concurrentes de Ticket
/// (transfert, réaffectation, changement d'état) bénéficient de la même protection par jeton de
/// concurrence optimiste (VersionConcurrence) que la prise en charge (déjà couverte par
/// <see cref="PriseEnChargeConcurrenceTests"/>). ABSENTS de tasks.md — ajoutés à titre de preuve
/// d'audit ; à faire valider par l'équipe avant de les considérer comme faisant partie de la
/// suite permanente.
/// </summary>
public sealed class ConcurrenceAutresOperationsAuditTests
{
    private static async Task<Guid> CreerTicketEnCoursAsync(Guid operateurInitial)
    {
        var maintenant = DateTime.UtcNow;
        await using var setup = BaseDeTestSql.CreerContexte();
        var dossier = Dossier.Creer($"DOS-CA-{Guid.NewGuid():N}", Guid.NewGuid(), maintenant);
        var ticket = Ticket.Creer($"TCK-CA-{Guid.NewGuid():N}", "BillingOffice", "Import", Guid.NewGuid(), dossier.Id, maintenant);
        ticket.PrendreEnCharge(operateurInitial, maintenant);
        setup.Dossiers.Add(dossier);
        setup.Tickets.Add(ticket);
        await setup.SaveChangesAsync();
        return ticket.Id;
    }

    [Fact]
    public async Task Deux_reaffectations_concurrentes_une_seule_reussit()
    {
        var ticketId = await CreerTicketEnCoursAsync(Guid.NewGuid());

        await using var contexteA = BaseDeTestSql.CreerContexte();
        await using var contexteB = BaseDeTestSql.CreerContexte();

        var vuParA = await contexteA.Tickets.SingleAsync(t => t.Id == ticketId);
        var vuParB = await contexteB.Tickets.SingleAsync(t => t.Id == ticketId);

        var operateurCibleA = Guid.NewGuid();
        var operateurCibleB = Guid.NewGuid();

        vuParA.Reaffecter(operateurCibleA, DateTime.UtcNow);
        vuParB.Reaffecter(operateurCibleB, DateTime.UtcNow);

        await contexteA.SaveChangesAsync();

        var uniteB = new UniteDeTravail(contexteB);
        await Assert.ThrowsAsync<ConflitDeConcurrenceException>(() => uniteB.EnregistrerAsync());

        await using var verification = BaseDeTestSql.CreerContexte();
        var final = await verification.Tickets.SingleAsync(t => t.Id == ticketId);
        Assert.Equal(operateurCibleA, final.OperateurAssigneId);
    }

    [Fact]
    public async Task Transfert_concurrent_a_une_reaffectation_sur_le_meme_ticket_un_seul_reussit()
    {
        var operateurInitial = Guid.NewGuid();
        var ticketId = await CreerTicketEnCoursAsync(operateurInitial);

        await using var contexteA = BaseDeTestSql.CreerContexte();
        await using var contexteB = BaseDeTestSql.CreerContexte();

        var vuParA = await contexteA.Tickets.SingleAsync(t => t.Id == ticketId);
        var vuParB = await contexteB.Tickets.SingleAsync(t => t.Id == ticketId);

        var fileCible = Guid.NewGuid();
        var autreOperateur = Guid.NewGuid();

        vuParA.Transferer(fileCible, null, DateTime.UtcNow); // A transfère (repart non affecté).
        vuParB.Reaffecter(autreOperateur, DateTime.UtcNow);  // B réaffecte, sur l'ancienne version.

        await contexteA.SaveChangesAsync();

        var uniteB = new UniteDeTravail(contexteB);
        await Assert.ThrowsAsync<ConflitDeConcurrenceException>(() => uniteB.EnregistrerAsync());

        await using var verification = BaseDeTestSql.CreerContexte();
        var final = await verification.Tickets.SingleAsync(t => t.Id == ticketId);
        // Le transfert de A a bien été appliqué seul ; la réaffectation de B n'a jamais été
        // silencieusement fusionnée ou perdue sans signalement.
        Assert.Equal(fileCible, final.FileCouranteId);
        Assert.Null(final.OperateurAssigneId);
        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, final.Statut);
    }

    [Fact]
    public async Task Cloture_concurrente_a_un_transfert_un_seul_reussit_et_l_etat_final_est_coherent()
    {
        var ticketId = await CreerTicketEnCoursAsync(Guid.NewGuid());

        await using var contexteA = BaseDeTestSql.CreerContexte();
        await using var contexteB = BaseDeTestSql.CreerContexte();

        var vuParA = await contexteA.Tickets.SingleAsync(t => t.Id == ticketId);
        var vuParB = await contexteB.Tickets.SingleAsync(t => t.Id == ticketId);

        vuParA.Cloturer(MotifClotureType.Normale, DateTime.UtcNow);
        vuParB.Transferer(Guid.NewGuid(), null, DateTime.UtcNow);

        await contexteA.SaveChangesAsync();

        var uniteB = new UniteDeTravail(contexteB);
        await Assert.ThrowsAsync<ConflitDeConcurrenceException>(() => uniteB.EnregistrerAsync());

        await using var verification = BaseDeTestSql.CreerContexte();
        var final = await verification.Tickets.SingleAsync(t => t.Id == ticketId);
        // Aucune mise à jour silencieuse : le ticket clôturé par A n'a pas été rouvert/transféré
        // silencieusement par la tentative concurrente de B.
        Assert.Equal(StatutTicket.Cloture, final.Statut);
    }
}
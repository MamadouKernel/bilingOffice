using CIT.Plateforme.Domain.Tickets;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Tickets;

/// <summary>
/// [TEST-SPEC] Exigence CDC: CYC-04, CYC-05 → US3 → scénarios d'acceptation 3, 4 de US3 → Test.
/// Aucun scénario CDC direct dans ce périmètre (recipe-scope-audit.md).
/// </summary>
public sealed class RelancesAbsenceReponseTests
{
    private static Ticket CreerTicketEnAttenteClient()
    {
        var maintenant = DateTime.UtcNow;
        var ticket = Ticket.Creer("TCK-REL-000001", "BillingOffice", "Import", Guid.NewGuid(), Guid.NewGuid(), maintenant);
        ticket.PrendreEnCharge(Guid.NewGuid(), maintenant);
        ticket.MettreEnAttenteClient(maintenant);
        return ticket;
    }

    [Fact]
    public void Non_eligible_tant_que_le_nombre_de_relances_max_n_est_pas_atteint()
    {
        var ticket = CreerTicketEnAttenteClient();
        ticket.EnregistrerRelance(DateTime.UtcNow);
        ticket.EnregistrerRelance(DateTime.UtcNow);

        Assert.False(ticket.EstEligibleASuggestionDeCloture(nombreRelancesMax: 3));
    }

    [Fact]
    public void Eligible_a_une_suggestion_de_cloture_quand_le_nombre_max_de_relances_est_atteint()
    {
        var ticket = CreerTicketEnAttenteClient();
        ticket.EnregistrerRelance(DateTime.UtcNow);
        ticket.EnregistrerRelance(DateTime.UtcNow);
        ticket.EnregistrerRelance(DateTime.UtcNow);

        Assert.True(ticket.EstEligibleASuggestionDeCloture(nombreRelancesMax: 3));
    }

    [Fact]
    public void Jamais_eligible_apres_un_etat_engageant_meme_avec_le_nombre_max_de_relances()
    {
        var ticket = CreerTicketEnAttenteClient();
        ticket.MarquerEtatEngageant(); // paiement confirmé / document officiel produit (module appelant)
        ticket.EnregistrerRelance(DateTime.UtcNow);
        ticket.EnregistrerRelance(DateTime.UtcNow);
        ticket.EnregistrerRelance(DateTime.UtcNow);

        Assert.False(ticket.EstEligibleASuggestionDeCloture(nombreRelancesMax: 3));
    }

    [Fact]
    public void La_suggestion_n_est_jamais_une_cloture_automatique()
    {
        // CYC-04 : « la clôture reste une décision manuelle d'un agent habilité — aucune clôture
        // automatique. » EstEligibleASuggestionDeCloture ne modifie jamais le statut du ticket.
        var ticket = CreerTicketEnAttenteClient();
        ticket.EnregistrerRelance(DateTime.UtcNow);
        ticket.EnregistrerRelance(DateTime.UtcNow);
        ticket.EnregistrerRelance(DateTime.UtcNow);

        _ = ticket.EstEligibleASuggestionDeCloture(nombreRelancesMax: 3);

        Assert.Equal(StatutTicket.EnAttenteClient, ticket.Statut);
    }
}

using CIT.Plateforme.Domain.Tickets;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Tickets;

/// <summary>
/// [TEST-TECH] Décision D2/D12/D13bis de research.md (Feature 003) : extensions additives de
/// <see cref="Ticket"/> (NumeroBl, DeclarationPresentielle, TypeAnnulationImport), sans régression
/// sur Feature 001/002.
/// </summary>
public sealed class TicketExtensionsBillingImportTests
{
    [Fact]
    public void Creer_signature_a_six_arguments_laisse_les_extensions_a_leurs_valeurs_par_defaut()
    {
        var dossierId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var maintenant = DateTime.UtcNow;

        var ticket = Ticket.Creer("TCK-001", "CustomerComplaint", "Reclamation", fileId, dossierId, maintenant);

        Assert.Null(ticket.NumeroBl);
        Assert.False(ticket.DeclarationPresentielle);
        Assert.Null(ticket.TypeAnnulationImport);
    }

    [Fact]
    public void Creer_avec_numeroBl_fourni_initialise_NumeroBl()
    {
        var dossierId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var maintenant = DateTime.UtcNow;

        var ticket = Ticket.Creer("TCK-002", "BillingImport", "Import", fileId, dossierId, maintenant, "ABC123");

        Assert.Equal("ABC123", ticket.NumeroBl);
    }

    [Fact]
    public void RejeterImport_positionne_Annule_et_RejetManifeste_jamais_Cloture()
    {
        var ticket = CreerTicketImport();

        ticket.RejeterImport(DateTime.UtcNow);

        Assert.Equal(StatutTicket.Annule, ticket.Statut);
        Assert.Equal(TypeAnnulationImport.RejetManifeste, ticket.TypeAnnulationImport);
    }

    [Fact]
    public void Rouvrir_refuse_un_ticket_rejete_au_meme_titre_qu_un_ticket_annule_ordinaire()
    {
        var ticket = CreerTicketImport();
        ticket.RejeterImport(DateTime.UtcNow);

        var exception = Assert.Throws<InvalidOperationException>(() => ticket.Rouvrir(DateTime.UtcNow));

        Assert.Contains("CLAR-02", exception.Message);
    }

    [Fact]
    public void Annuler_ordinaire_ne_positionne_jamais_TypeAnnulationImport()
    {
        var ticket = CreerTicketImport();

        ticket.Annuler(DateTime.UtcNow);

        Assert.Equal(StatutTicket.Annule, ticket.Statut);
        Assert.Null(ticket.TypeAnnulationImport);
    }

    [Fact]
    public void DeclarerFinalisationPresentielle_positionne_DeclarationPresentielle_a_vrai()
    {
        var ticket = CreerTicketImport();

        ticket.DeclarerFinalisationPresentielle();

        Assert.True(ticket.DeclarationPresentielle);
    }

    private static Ticket CreerTicketImport() =>
        Ticket.Creer("TCK-003", "BillingImport", "Import", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, "ABC123");
}

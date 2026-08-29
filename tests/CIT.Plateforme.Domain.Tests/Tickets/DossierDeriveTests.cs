using CIT.Plateforme.Domain.Tickets;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Tickets;

/// <summary>
/// [TEST-SPEC] Exigence CDC: SOC-11 → US7 (rattaché ici car dépend de la clôture) → CLAR-01 → Test.
/// </summary>
public sealed class DossierDeriveTests
{
    private static Ticket CreerTicket(string numero, Guid dossierId, DateTime maintenant) =>
        Ticket.Creer(numero, "BillingOffice", "Import", Guid.NewGuid(), dossierId, maintenant);

    [Fact]
    public void Un_dossier_sans_ticket_est_actif()
    {
        Assert.Equal(EtatDossier.Actif, EtatDossierDerive.Calculer([]));
    }

    [Fact]
    public void Un_dossier_dont_un_ticket_est_encore_actif_reste_actif()
    {
        var maintenant = DateTime.UtcNow;
        var dossierId = Guid.NewGuid();
        var ticket1 = CreerTicket("TCK-DOS-01", dossierId, maintenant);
        var ticket2 = CreerTicket("TCK-DOS-02", dossierId, maintenant);
        ticket1.PrendreEnCharge(Guid.NewGuid(), maintenant);
        ticket1.Cloturer(MotifClotureType.Normale, maintenant);
        // ticket2 reste en attente de prise en charge.

        Assert.Equal(EtatDossier.Actif, EtatDossierDerive.Calculer([ticket1, ticket2]));
    }

    [Fact]
    public void Un_dossier_dont_tous_les_tickets_sont_clotures_ou_annules_est_termine()
    {
        var maintenant = DateTime.UtcNow;
        var dossierId = Guid.NewGuid();
        var ticket1 = CreerTicket("TCK-DOS-03", dossierId, maintenant);
        var ticket2 = CreerTicket("TCK-DOS-04", dossierId, maintenant);
        ticket1.PrendreEnCharge(Guid.NewGuid(), maintenant);
        ticket1.Cloturer(MotifClotureType.Normale, maintenant);
        ticket2.Annuler(maintenant);

        Assert.Equal(EtatDossier.Termine, EtatDossierDerive.Calculer([ticket1, ticket2]));
    }

    [Fact]
    public void Un_dossier_termine_redevient_actif_des_qu_un_ticket_est_rouvert()
    {
        var maintenant = DateTime.UtcNow;
        var dossierId = Guid.NewGuid();
        var ticket = CreerTicket("TCK-DOS-05", dossierId, maintenant);
        ticket.PrendreEnCharge(Guid.NewGuid(), maintenant);
        ticket.Cloturer(MotifClotureType.Normale, maintenant);
        Assert.Equal(EtatDossier.Termine, EtatDossierDerive.Calculer([ticket]));

        ticket.Rouvrir(maintenant); // CLAR-01 : réactivation automatique du dossier

        Assert.Equal(EtatDossier.Actif, EtatDossierDerive.Calculer([ticket]));
    }
}

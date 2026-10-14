using CIT.Plateforme.Application.Notifications.Resolveurs;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Notifications;

/// <summary>
/// ENR-02 — le transporteur (client du dossier) doit être notifié quand l'Agent Gate valide son
/// enrôlement, puis quand il est convoqué au Gate pour la remise du badge. Ce résolveur est le
/// maillon qui désigne ce destinataire : la garde ci-dessous vérifie qu'il sert bien ces deux
/// événements, et qu'il ne franchit jamais la frontière vers un événement interne (MSG-02).
///
/// <see cref="ResolveurClientDuDossier.PeutResoudre"/> ne consulte que la liste des types ; les
/// dépôts injectés ne sont pas sollicités ici, d'où les <c>null!</c>.
/// </summary>
public sealed class ResolveurClientEnrolementTests
{
    private static readonly ResolveurClientDuDossier Resolveur = new(null!, null!, null!);

    [Theory]
    [InlineData("EnrolementValide", true)]
    [InlineData("ConvocationDeclenchee", true)]
    [InlineData("BadgeRemisEtDossierCloture", true)]
    [InlineData("AvoirComplementDemande", true)]   // complément attendu sur un dossier d'avoir
    [InlineData("EnrolementValideInterne", false)] // validation interne « 4 yeux » : autre destinataire
    [InlineData("CollaborateurMentionne", false)]   // note interne — jamais notifiée à un client (MSG-02)
    [InlineData("MessagePoste", false)]             // peut porter une note interne : jamais un client
    public void Le_resolveur_client_sert_la_validation_et_la_convocation_Agent_Gate(string evenement, bool servi)
    {
        Assert.Equal(servi, Resolveur.PeutResoudre(evenement));
    }

    /// <summary>
    /// Régression : certains producteurs sérialisent la charge utile en PascalCase
    /// (<c>{"TicketId":"…"}</c>, via JsonSerializer sans politique de nommage). La lecture doit
    /// être INSENSIBLE À LA CASSE, sinon l'événement est publié mais aucun destinataire n'est résolu.
    /// </summary>
    [Theory]
    [InlineData("ticketId")] // camelCase écrit à la main
    [InlineData("TicketId")] // PascalCase produit par JsonSerializer
    public async Task Lit_le_ticket_quelle_que_soit_la_casse_de_la_cle(string cle)
    {
        var id = Guid.NewGuid();
        var chargeUtile = $"{{\"{cle}\":\"{id}\"}}";

        Assert.Equal(id, await Resolveur.ResoudreTicketAsync(chargeUtile));
    }
}

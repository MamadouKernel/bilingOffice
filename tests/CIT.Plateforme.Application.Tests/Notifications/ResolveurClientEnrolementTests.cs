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
    [InlineData("EnrolementValideInterne", false)] // validation interne « 4 yeux » : autre destinataire
    [InlineData("CollaborateurMentionne", false)]   // note interne — jamais notifiée à un client (MSG-02)
    public void Le_resolveur_client_sert_la_validation_et_la_convocation_Agent_Gate(string evenement, bool servi)
    {
        Assert.Equal(servi, Resolveur.PeutResoudre(evenement));
    }
}

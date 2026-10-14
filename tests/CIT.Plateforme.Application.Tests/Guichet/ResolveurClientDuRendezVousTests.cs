using CIT.Plateforme.Application.Guichet;
using CIT.Plateforme.Application.Notifications.Resolveurs;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Guichet;

/// <summary>
/// Feature 010 — le résolveur rendez-vous ne sert QUE les événements de RDV Billing Office, et
/// jamais un autre événement. La résolution des destinataires est déléguée à
/// <see cref="ResolveurClientDuDossier"/> ; seul l'aiguillage par type est vérifié ici (les dépôts
/// ne sont donc pas sollicités, d'où les <c>null!</c>).
/// </summary>
public sealed class ResolveurClientDuRendezVousTests
{
    private static readonly ResolveurClientDuRendezVous Resolveur =
        new(new ResolveurClientDuDossier(null!, null!, null!));

    [Theory]
    [InlineData("RendezVousReserve", true)]
    [InlineData("RendezVousAnnule", true)]
    [InlineData("RendezVousHonore", false)]  // honoré : pas de notification client
    [InlineData("EnrolementValide", false)]  // servi par le résolveur client, pas celui-ci
    [InlineData("MessagePoste", false)]
    public void Ne_sert_que_les_evenements_de_rendez_vous(string evenement, bool servi)
    {
        Assert.Equal(servi, Resolveur.PeutResoudre(evenement));
    }
}

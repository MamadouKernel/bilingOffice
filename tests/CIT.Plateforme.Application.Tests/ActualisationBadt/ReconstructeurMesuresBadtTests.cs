using System;
using System.Collections.Generic;
using System.Linq;
using CIT.Plateforme.Application.ActualisationBadt;
using CIT.Plateforme.Domain.ActualisationBadt;
using CIT.Plateforme.Domain.Audit;
using Xunit;

namespace CIT.Plateforme.Application.Tests.ActualisationBadt;

public class ReconstructeurMesuresBadtTests
{
    private static readonly DateTime Base = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Reconstruire_mappe_les_evenements_d_historique_en_mesures()
    {
        var ticketId = Guid.NewGuid();
        var agent = Guid.NewGuid();
        var decideur = Guid.NewGuid();

        var tc1 = LigneActualisationBadt.Creer(ticketId, "BL1", "TC1");
        var tc2 = LigneActualisationBadt.Creer(ticketId, "BL1", "TC2");
        var lignes = new List<LigneActualisationBadt> { tc1, tc2 };

        var entrees = new List<EntreeHistorique>
        {
            EntreeHistorique.Creer(ticketId, "TicketCree", "numero=TCK-1", Guid.NewGuid(), Base),
            EntreeHistorique.Creer(ticketId, "ConteneursAjoutes", "bl=BL1;conteneurs=TC1,TC2", agent, Base.AddHours(1)),
            EntreeHistorique.Creer(ticketId, "LigneQualifiee", "conteneur=TC1;cause=Terminale", agent, Base.AddHours(2)),
            EntreeHistorique.Creer(ticketId, "LigneDecidee", "conteneur=TC1;accordée;jours=5;nouvelleValidite=2026-10-10", decideur, Base.AddHours(3)),
        };

        var mesures = ReconstructeurMesuresBadt.Reconstruire(ticketId, entrees, lignes);

        Assert.Single(mesures, m => m.Type == TypeMesureBadt.Creation);
        Assert.Equal(2, mesures.Count(m => m.Type == TypeMesureBadt.Depliage));

        var qualif = Assert.Single(mesures, m => m.Type == TypeMesureBadt.Qualification);
        Assert.Equal(tc1.Id, qualif.LigneId);
        Assert.Equal(CauseExpirationBadt.Terminale, qualif.Cause);

        var accord = Assert.Single(mesures, m => m.Type == TypeMesureBadt.Accord);
        Assert.Equal(tc1.Id, accord.LigneId);
        Assert.Equal(5, accord.NombreJoursAccordes);
        Assert.Equal(decideur, accord.ActeurId);
    }

    [Fact]
    public void Reconstruire_ignore_les_entrees_non_pertinentes()
    {
        var ticketId = Guid.NewGuid();
        var entrees = new List<EntreeHistorique>
        {
            EntreeHistorique.Creer(ticketId, "StatutChange", "de=A;vers=B", Guid.NewGuid(), Base),
        };

        Assert.Empty(ReconstructeurMesuresBadt.Reconstruire(ticketId, entrees, []));
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using CIT.Plateforme.Application.ActualisationBadt;
using CIT.Plateforme.Domain.ActualisationBadt;
using Xunit;

namespace CIT.Plateforme.Application.Tests.ActualisationBadt;

public class CalculateurReportingBadtTests
{
    private static readonly DateTime Base = new(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc);

    private static MesureActualisationBadt M(
        Guid? ligneId, TypeMesureBadt type, Guid acteur, DateTime quand,
        CauseExpirationBadt? cause = null, int? jours = null) =>
        MesureActualisationBadt.Creer(Guid.NewGuid(), ligneId, "BL1", "TC1", type, acteur, quand, cause, jours);

    [Fact]
    public void Agreger_calcule_les_indicateurs_cibles()
    {
        var agent = Guid.NewGuid();
        var decideur = Guid.NewGuid();
        var demandeur = Guid.NewGuid();
        var l1 = Guid.NewGuid();
        var l2 = Guid.NewGuid();
        var l3 = Guid.NewGuid();

        var mesures = new List<MesureActualisationBadt>
        {
            M(null, TypeMesureBadt.Creation, demandeur, Base),
            M(null, TypeMesureBadt.Depliage, agent, Base.AddMinutes(5)),
            // L1 : qualif terminale (escalade) puis accord 2h plus tard.
            M(l1, TypeMesureBadt.Qualification, agent, Base.AddHours(1), CauseExpirationBadt.Terminale),
            M(l1, TypeMesureBadt.Accord, decideur, Base.AddHours(3), jours: 7),
            // L2 : qualif non terminale puis rejet agent.
            M(l2, TypeMesureBadt.Qualification, agent, Base.AddMinutes(30), CauseExpirationBadt.NonTerminale),
            M(l2, TypeMesureBadt.RejetAgent, agent, Base.AddMinutes(40)),
            // L3 : qualif terminale (escalade) puis renvoi (pas une décision terminale).
            M(l3, TypeMesureBadt.Qualification, agent, Base.AddHours(1), CauseExpirationBadt.Terminale),
            M(l3, TypeMesureBadt.Renvoi, decideur, Base.AddHours(2)),
        };

        var r = CalculateurReportingBadt.Agreger(mesures);

        Assert.Equal(1, r.TotalDemandes);
        Assert.Equal(2, r.NombreEscalades); // L1 + L3 (qualifs terminales)
        Assert.Equal(1, r.NombreAccords);
        Assert.Equal(0, r.NombreRefus);
        Assert.Equal(1, r.NombreRenvois);
        Assert.Equal(1, r.NombreRejetsAgent);
        // Délai qualif→décision : seule la paire L1 (accord) compte = 2h.
        Assert.Equal(TimeSpan.FromHours(2), r.DelaiMoyenQualificationDecision);

        var ligneDecideur = r.ParActeur.Single(a => a.ActeurId == decideur);
        Assert.Equal(1, ligneDecideur.Accords);
        Assert.Equal(1, ligneDecideur.Renvois);
        Assert.Equal(50d, ligneDecideur.TauxAccord); // 1 accord / 2 décisions
        Assert.Equal(50d, ligneDecideur.TauxRenvoi);

        var ligneAgent = r.ParActeur.Single(a => a.ActeurId == agent);
        Assert.Equal(3, ligneAgent.Qualifications);
        Assert.Equal(1, ligneAgent.Depliages);
        Assert.Equal(1, ligneAgent.RejetsAgent);

        Assert.Single(r.Depliages);
    }

    [Fact]
    public void Agreger_sans_decision_rend_un_delai_moyen_null()
    {
        var mesures = new List<MesureActualisationBadt>
        {
            M(null, TypeMesureBadt.Creation, Guid.NewGuid(), Base),
        };

        var r = CalculateurReportingBadt.Agreger(mesures);

        Assert.Null(r.DelaiMoyenQualificationDecision);
        Assert.Empty(r.ParActeur);
    }
}

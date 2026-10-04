using CIT.Plateforme.Application.Enrolement;
using CIT.Plateforme.Domain.Enrolement;
using CIT.Plateforme.Domain.Tickets;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Enrolement;

public sealed class DoublonsEnrolementTests
{
    private static Ticket Demande(string numero, TypeObjetEnrolement type, SensEnrolement sens, string objet)
    {
        var t = Ticket.Creer(numero, "Enrolement", "Enrolement", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        t.DefinirTypeEnrolement(type, sens);
        t.DefinirInformationsEnrolement(objet, "desc");
        return t;
    }

    [Fact]
    public void Signale_les_demandes_identiques_et_seulement_elles()
    {
        var a = Demande("ENR-11", TypeObjetEnrolement.Camion, SensEnrolement.Ajout, "AA-4578-CI — tracteur Volvo FH");
        var b = Demande("ENR-13", TypeObjetEnrolement.Camion, SensEnrolement.Ajout, "aa-4578-ci  —  tracteur volvo fh");
        var retrait = Demande("ENR-14", TypeObjetEnrolement.Camion, SensEnrolement.Retrait, "AA-4578-CI — tracteur Volvo FH");
        var autre = Demande("ENR-20", TypeObjetEnrolement.Chauffeur, SensEnrolement.Ajout, "KONE Ali");

        var doublons = DoublonsEnrolement.Detecter([a, b, retrait, autre]);

        Assert.Equal(2, doublons.Count);
        Assert.Equal(["ENR-13"], doublons[a.Id]);
        Assert.Equal(["ENR-11"], doublons[b.Id]);
    }
}

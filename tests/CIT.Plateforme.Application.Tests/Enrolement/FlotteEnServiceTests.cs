using CIT.Plateforme.Application.Enrolement;
using CIT.Plateforme.Domain.Enrolement;
using CIT.Plateforme.Domain.Tickets;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Enrolement;

public sealed class FlotteEnServiceTests
{
    private static int _n;

    private static Ticket Enrolement(TypeObjetEnrolement type, SensEnrolement sens, string objet, StatutTicket statut)
    {
        var t = Ticket.Creer($"ENR-{++_n}", "Enrolement", "Enrolement", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        t.DefinirTypeEnrolement(type, sens);
        t.DefinirInformationsEnrolement(objet, "desc");
        if (statut == StatutTicket.Cloture) t.Cloturer(MotifClotureType.Normale, DateTime.UtcNow);
        if (statut == StatutTicket.Annule) t.Annuler(DateTime.UtcNow);
        return t;
    }

    [Fact]
    public void Ne_propose_que_les_objets_badges_et_pas_encore_retires()
    {
        var flotte = FlotteEnService.Calculer(
        [
            Enrolement(TypeObjetEnrolement.Chauffeur, SensEnrolement.Ajout, "KONE Ali", StatutTicket.Cloture),
            Enrolement(TypeObjetEnrolement.Chauffeur, SensEnrolement.Ajout, "DIALLO Sekou", StatutTicket.Cloture),
            Enrolement(TypeObjetEnrolement.Chauffeur, SensEnrolement.Retrait, "diallo sekou ", StatutTicket.EnAttenteDePriseEnCharge),
            Enrolement(TypeObjetEnrolement.Camion, SensEnrolement.Ajout, "AA-123-BC", StatutTicket.Cloture),
            Enrolement(TypeObjetEnrolement.Camion, SensEnrolement.Retrait, "AA-123-BC", StatutTicket.Annule),
            Enrolement(TypeObjetEnrolement.Camion, SensEnrolement.Ajout, "ZZ-999", StatutTicket.EnAttenteDePriseEnCharge),
        ]);

        Assert.Equal(
            [new ObjetFlotte(TypeObjetEnrolement.Chauffeur, "KONE Ali"), new ObjetFlotte(TypeObjetEnrolement.Camion, "AA-123-BC")],
            flotte);
    }
}

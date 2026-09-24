using System;
using CIT.Plateforme.Domain.Enrolement;
using CIT.Plateforme.Domain.Tickets;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Tickets;

/// <summary>
/// LOT 1 — Enrôlement société (chauffeurs + camions, ajout + retrait).
/// Le ticket d'enrôlement porte désormais deux dimensions typées : le TYPE D'OBJET
/// (Chauffeur / Camion) et le SENS (Ajout / Retrait). Ces dimensions restent nulles pour tout
/// ticket non-enrôlement, garantissant la compatibilité ascendante avec l'existant.
/// </summary>
public sealed class TicketTypageEnrolementTests
{
    private static Ticket CreerTicketEnrolement() =>
        Ticket.Creer("ENR-0001", "Enrolement", "Enrolement", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

    [Fact]
    public void UnTicketNeuf_NAAucunTypageEnrolement()
    {
        var ticket = CreerTicketEnrolement();

        Assert.Null(ticket.ObjetEnrolement);
        Assert.Null(ticket.SensEnrolement);
    }

    [Theory]
    [InlineData(TypeObjetEnrolement.Chauffeur, SensEnrolement.Ajout)]
    [InlineData(TypeObjetEnrolement.Chauffeur, SensEnrolement.Retrait)]
    [InlineData(TypeObjetEnrolement.Camion, SensEnrolement.Ajout)]
    [InlineData(TypeObjetEnrolement.Camion, SensEnrolement.Retrait)]
    public void DefinirTypeEnrolement_RenseigneObjetEtSens(TypeObjetEnrolement objet, SensEnrolement sens)
    {
        var ticket = CreerTicketEnrolement();

        ticket.DefinirTypeEnrolement(objet, sens);

        Assert.Equal(objet, ticket.ObjetEnrolement);
        Assert.Equal(sens, ticket.SensEnrolement);
    }

    [Fact]
    public void DefinirTypeEnrolement_EstReassignable_AvantTraitement()
    {
        var ticket = CreerTicketEnrolement();

        ticket.DefinirTypeEnrolement(TypeObjetEnrolement.Chauffeur, SensEnrolement.Ajout);
        ticket.DefinirTypeEnrolement(TypeObjetEnrolement.Camion, SensEnrolement.Retrait);

        Assert.Equal(TypeObjetEnrolement.Camion, ticket.ObjetEnrolement);
        Assert.Equal(SensEnrolement.Retrait, ticket.SensEnrolement);
    }
}

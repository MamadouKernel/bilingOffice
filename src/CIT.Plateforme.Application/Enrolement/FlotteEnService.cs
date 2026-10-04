using CIT.Plateforme.Domain.Enrolement;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Enrolement;

/// <summary>Chauffeur ou camion de la flotte, proposé au retrait d'accès.</summary>
public sealed record ObjetFlotte(TypeObjetEnrolement Type, string Objet);

/// <summary>
/// Flotte en service, déduite des tickets d'enrôlement de la société : les ajouts dont le badge a
/// été remis (ticket clôturé), moins les objets déjà visés par un retrait non annulé. Sert à la
/// liste déroulante du retrait (04/10/2026) — plus de saisie libre sujette aux fautes de frappe.
/// </summary>
public static class FlotteEnService
{
    public static IReadOnlyList<ObjetFlotte> Calculer(IEnumerable<Ticket> ticketsEnrolement)
    {
        var tickets = ticketsEnrolement
            .Where(t => t.ObjetEnrolement is not null && !string.IsNullOrWhiteSpace(t.Objet))
            .ToList();

        var retires = tickets
            .Where(t => t.SensEnrolement == SensEnrolement.Retrait && t.Statut != StatutTicket.Annule)
            .Select(t => Cle(t.ObjetEnrolement!.Value, t.Objet!))
            .ToHashSet();

        return [.. tickets
            .Where(t => t.SensEnrolement == SensEnrolement.Ajout && t.Statut == StatutTicket.Cloture)
            .Select(t => new ObjetFlotte(t.ObjetEnrolement!.Value, t.Objet!.Trim()))
            .Where(o => !retires.Contains(Cle(o.Type, o.Objet)))
            .DistinctBy(o => Cle(o.Type, o.Objet))
            .OrderBy(o => o.Type).ThenBy(o => o.Objet, StringComparer.OrdinalIgnoreCase)];
    }

    private static string Cle(TypeObjetEnrolement type, string objet) => $"{type}|{objet.Trim().ToUpperInvariant()}";
}

using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Enrolement;

/// <summary>
/// Demandes d'enrôlement en double dans la file Agent Gate (revue du 04/10/2026 : trois demandes
/// identiques pour le même camion). Deux tickets sont doublons s'ils visent le même type d'objet,
/// dans le même sens, avec le même objet (casse et espaces ignorés). Signalement seulement : l'agent
/// décide (un doublon légitime reste possible, ex. homonyme).
/// </summary>
public static class DoublonsEnrolement
{
    /// <returns>Pour chaque ticket en double, les numéros des autres tickets du groupe.</returns>
    public static IReadOnlyDictionary<Guid, IReadOnlyList<string>> Detecter(IEnumerable<Ticket> tickets) =>
        tickets
            .Where(t => t.ObjetEnrolement is not null && !string.IsNullOrWhiteSpace(t.Objet))
            .GroupBy(t => (t.ObjetEnrolement, t.SensEnrolement, Objet: Normaliser(t.Objet!)))
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Select(t => (t.Id, Autres: (IReadOnlyList<string>)[.. g.Where(o => o.Id != t.Id).Select(o => o.Numero)])))
            .ToDictionary(x => x.Id, x => x.Autres);

    private static string Normaliser(string objet) =>
        string.Join(' ', objet.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
}

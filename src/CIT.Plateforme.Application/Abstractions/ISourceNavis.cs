using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CIT.Plateforme.Application.Abstractions;

/// <summary>
/// Port de LECTURE de Navis N4 (Universal Query API, REST). L'assistant l'interroge à côté de
/// YABA pour les questions conteneur / navire / disponibilité — jamais de LLM.
///
/// Modèle N4 : on n'écrit AUCUNE requête. Chaque requête est un FILTRE nommé, défini côté N4
/// (colonnes + prédicats), qu'on appelle par son nom avec des paramètres. Ce port est en
/// lecture seule : il n'expose aucune écriture (les API d'écriture N4 — HPU, ICU — sont hors
/// périmètre de l'assistant).
/// </summary>
public interface ISourceNavis
{
    Task<ResultatNavis> InterrogerAsync(
        string filtre, IReadOnlyDictionary<string, string> parametres, CancellationToken ct = default);
}

/// <summary>Une ligne de résultat : colonnes → valeurs. Le schéma dépend du filtre N4 appelé.</summary>
public sealed record LigneNavis(IReadOnlyDictionary<string, string> Colonnes);

/// <summary>
/// Résultat d'une interrogation Navis. <see cref="Disponible"/> = false quand Navis est
/// désactivé, mal configuré, injoignable ou renvoie une erreur : l'assistant dégrade proprement
/// (« information Navis indisponible »), jamais une exception qui remonte à l'usager.
/// </summary>
public sealed record ResultatNavis(bool Disponible, IReadOnlyList<LigneNavis> Lignes, string? Motif = null)
{
    public static ResultatNavis Indisponible(string motif) => new(false, new List<LigneNavis>(), motif);

    public static ResultatNavis Succes(IReadOnlyList<LigneNavis> lignes) => new(true, lignes, null);
}

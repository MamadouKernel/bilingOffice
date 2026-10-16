using CIT.Plateforme.Domain.ActualisationBadt;
using CIT.Plateforme.Domain.Audit;

namespace CIT.Plateforme.Application.ActualisationBadt;

/// <summary>
/// FEATURE 010 (Actualisation BADT) — reconstruction des mesures de reporting d'un ticket à partir
/// de son historique fonctionnel (backfill des demandes antérieures à la mise en service du
/// reporting). Logique pure et testable : prend les entrées d'historique et les lignes actuelles
/// du ticket, rend les mesures à insérer.
///
/// ponytail: parsing best-effort des détails d'historique ("k=v;..."). C'est une reprise one-shot ;
/// une entrée illisible est simplement ignorée plutôt que de bloquer le backfill.
/// </summary>
public static class ReconstructeurMesuresBadt
{
    public static IReadOnlyList<MesureActualisationBadt> Reconstruire(
        Guid ticketId,
        IReadOnlyList<EntreeHistorique> entrees,
        IReadOnlyList<LigneActualisationBadt> lignes)
    {
        // conteneur → ligne pour rattacher les événements qui ne citent que le conteneur.
        var parConteneur = lignes
            .Where(l => l.NumeroConteneur.Length > 0)
            .GroupBy(l => l.NumeroConteneur, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var mesures = new List<MesureActualisationBadt>();

        foreach (var e in entrees.OrderBy(e => e.HorodatageUtc))
        {
            var acteur = e.AuteurId ?? Guid.Empty;
            var champs = Champs(e.Detail);

            switch (e.Evenement)
            {
                case "TicketCree":
                    mesures.Add(MesureActualisationBadt.Creer(ticketId, null, null, null, TypeMesureBadt.Creation, acteur, e.HorodatageUtc));
                    break;

                case "ConteneursAjoutes":
                    var bl = champs.GetValueOrDefault("bl");
                    foreach (var conteneur in (champs.GetValueOrDefault("conteneurs") ?? string.Empty)
                                 .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        mesures.Add(MesureActualisationBadt.Creer(ticketId, null, bl, conteneur, TypeMesureBadt.Depliage, acteur, e.HorodatageUtc));
                    }
                    break;

                case "LigneQualifiee":
                    AjouterLigne(mesures, ticketId, parConteneur, champs.GetValueOrDefault("conteneur"),
                        TypeMesureBadt.Qualification, acteur, e.HorodatageUtc, cause: ParserCause(champs.GetValueOrDefault("cause")));
                    break;

                case "BlQualifie":
                    var blQualif = champs.GetValueOrDefault("bl");
                    var causeBl = ParserCause(champs.GetValueOrDefault("cause"));
                    foreach (var ligne in lignes.Where(l => string.Equals(l.NumeroBl, blQualif, StringComparison.OrdinalIgnoreCase) && l.NumeroConteneur.Length > 0))
                    {
                        mesures.Add(MesureActualisationBadt.Creer(ticketId, ligne.Id, ligne.NumeroBl, ligne.NumeroConteneur,
                            TypeMesureBadt.Qualification, acteur, e.HorodatageUtc, causeBl));
                    }
                    break;

                case "LigneRejeteeAgent":
                    AjouterLigne(mesures, ticketId, parConteneur, champs.GetValueOrDefault("conteneur"),
                        TypeMesureBadt.RejetAgent, acteur, e.HorodatageUtc);
                    break;

                case "LigneRenvoyee":
                    AjouterLigne(mesures, ticketId, parConteneur, champs.GetValueOrDefault("conteneur"),
                        TypeMesureBadt.Renvoi, acteur, e.HorodatageUtc);
                    break;

                case "LigneDecidee":
                    var detail = e.Detail ?? string.Empty;
                    var type = detail.Contains("accord", StringComparison.OrdinalIgnoreCase)
                        ? TypeMesureBadt.Accord
                        : TypeMesureBadt.Refus;
                    int? jours = int.TryParse(champs.GetValueOrDefault("jours"), out var j) ? j : null;
                    AjouterLigne(mesures, ticketId, parConteneur, champs.GetValueOrDefault("conteneur"),
                        type, acteur, e.HorodatageUtc, jours: jours);
                    break;
            }
        }

        return mesures;
    }

    private static void AjouterLigne(
        List<MesureActualisationBadt> mesures, Guid ticketId,
        IReadOnlyDictionary<string, LigneActualisationBadt> parConteneur, string? conteneur,
        TypeMesureBadt type, Guid acteur, DateTime horodatage,
        CauseExpirationBadt? cause = null, int? jours = null)
    {
        if (string.IsNullOrEmpty(conteneur))
        {
            return;
        }

        var ligne = parConteneur.GetValueOrDefault(conteneur);
        mesures.Add(MesureActualisationBadt.Creer(
            ticketId, ligne?.Id, ligne?.NumeroBl, conteneur, type, acteur, horodatage, cause, jours));
    }

    private static CauseExpirationBadt? ParserCause(string? valeur) =>
        Enum.TryParse<CauseExpirationBadt>(valeur, out var c) ? c : null;

    private static Dictionary<string, string> Champs(string? detail)
    {
        var champs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(detail))
        {
            return champs;
        }

        foreach (var segment in detail.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var i = segment.IndexOf('=');
            if (i > 0)
            {
                champs[segment[..i].Trim()] = segment[(i + 1)..].Trim();
            }
        }

        return champs;
    }
}

using System.Text.Json;

namespace CIT.Plateforme.Application.Notifications.Resolveurs;

/// <summary>
/// FEATURE 009 — lecture de la charge utile JSON d'un <c>EvenementSocle</c>.
///
/// Les 21 sites de publication existants écrivent leur charge utile en JSON plat (par exemple
/// <c>{"ticketId":"…","collaborateurId":"…"}</c>). Cet utilitaire la lit SANS modifier aucun
/// producteur (research.md D1, tasks.md invariant I6).
///
/// Toute charge utile illisible retourne <c>null</c> : la distribution d'un événement mal formé
/// ne doit jamais interrompre le traitement des suivants.
/// </summary>
internal static class ResolveurChargeUtile
{
    public static Guid? LireGuid(string? chargeUtile, string propriete)
    {
        if (string.IsNullOrWhiteSpace(chargeUtile))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(chargeUtile);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!document.RootElement.TryGetProperty(propriete, out var valeur))
            {
                return null;
            }

            var texte = valeur.ValueKind == JsonValueKind.String ? valeur.GetString() : null;

            return Guid.TryParse(texte, out var identifiant) && identifiant != Guid.Empty
                ? identifiant
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

using System.Globalization;

namespace CIT.Plateforme.Application.Tarification;

/// <summary>
/// Libellés de la facture proforma dans la langue de l'utilisateur. Partagés par l'aperçu (Web) et
/// le PDF (Infrastructure) : la clé est le libellé français du modèle officiel CIT.
/// </summary>
public static class TexteProforma
{
    private static readonly Dictionary<string, string> Anglais = new(StringComparer.Ordinal)
    {
        ["FACTURE PROFORMA"] = "PROFORMA INVOICE",
        ["Simulation — document non contractuel"] = "Simulation — non-binding document",
        ["N° simulation"] = "Simulation no.",
        ["Client"] = "Customer",
        ["Type client"] = "Customer type",
        ["Trafic"] = "Traffic",
        ["Port de chargement"] = "Port of loading",
        ["Port de déchargement"] = "Port of discharge",
        ["Date d'arrivée navire"] = "Vessel arrival date",
        ["Date d'enlèvement"] = "Pick-up date",
        ["Durée de séjour"] = "Length of stay",
        ["jour(s)"] = "day(s)",
        ["Escale"] = "Port call",
        ["Grille appliquée"] = "Applied tariff grid",
        ["Navire"] = "Vessel",
        ["Voyage"] = "Voyage",
        ["BL / Booking"] = "BL / Booking",
        ["Rubriques"] = "Items",
        ["Montant CFA"] = "Amount CFA",
        ["Total CFA"] = "Total CFA",
        ["Aucune rubrique facturable."] = "No chargeable item.",
        ["Unité(s)"] = "Unit(s)",
        ["Montant Total Hors Taxes Prestations"] = "Total Amount of Services excl. Tax",
        ["Montant Total TVA"] = "Total VAT Amount",
        ["Montant Total à Payer"] = "Total Amount Payable",
        ["Acconage Plein"] = "Full Container Handling",
        ["Relevage Plein"] = "Full Container Lifting",
        ["Branchement Frigo"] = "Reefer Plug-in",
        ["Majoration Lourd"] = "Heavy Surcharge",
        ["Majoration Dangereux"] = "Dangerous Goods Surcharge",
        ["Majoration Hors Gabarit"] = "Out of Gauge Surcharge",
        ["Stationnement"] = "Storage",
    };

    /// <summary>Vrai si l'interface de l'utilisateur est en anglais.</summary>
    public static bool EnAnglais => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en";

    /// <summary>Libellé dans la langue courante (le français s'il n'a pas de traduction).</summary>
    public static string T(string francais) =>
        EnAnglais && Anglais.TryGetValue(francais, out var anglais) ? anglais : francais;

    /// <summary>Montant en toutes lettres dans la langue courante.</summary>
    public static string MontantEnLettres(decimal montant) =>
        EnAnglais ? MontantFrancsCfa.EnLettresAnglais(montant) : MontantFrancsCfa.EnLettres(montant);
}

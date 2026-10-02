namespace CIT.Plateforme.Application.Tarification;

/// <summary>
/// Conversion d'un montant entier en toutes lettres (français), suffixé « francs CFA ».
/// Logique unique partagée par l'aperçu facture (Web) et le générateur PDF (Infrastructure) :
/// un seul endroit à corriger, un seul endroit à tester.
/// </summary>
public static class MontantFrancsCfa
{
    private static readonly string[] Unites =
        ["", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf", "dix",
         "onze", "douze", "treize", "quatorze", "quinze", "seize", "dix-sept", "dix-huit", "dix-neuf"];

    private static readonly string[] Dizaines =
        ["", "", "vingt", "trente", "quarante", "cinquante", "soixante", "soixante", "quatre-vingt", "quatre-vingt"];

    public static string EnLettres(decimal montant)
    {
        var n = (long)Math.Round(montant);
        if (n < 0) n = -n;
        if (n == 0) return "zéro franc CFA";

        var parts = new List<string>();
        long millions = n / 1_000_000;
        long milliers = (n % 1_000_000) / 1000;
        long reste = n % 1000;

        if (millions > 0) parts.Add((millions > 1 ? Tri((int)millions) + " " : "un ") + "million" + (millions > 1 ? "s" : ""));
        if (milliers > 0) parts.Add((milliers > 1 ? Tri((int)milliers) + " " : "") + "mille");
        if (reste > 0) parts.Add(Tri((int)reste));

        return string.Join(" ", parts).Trim() + " francs CFA";
    }

    private static string Tri(int x)
    {
        var s = "";
        int centaines = x / 100, r = x % 100;
        if (centaines > 0)
            s += (centaines > 1 ? Unites[centaines] + " " : "") + "cent" + (centaines > 1 && r == 0 ? "s" : "") + (r != 0 ? " " : "");

        if (r < 20)
        {
            s += Unites[r];
        }
        else
        {
            int di = r / 10, un = r % 10;
            if (di == 7 || di == 9)
                s += Dizaines[di] + "-" + Unites[10 + un];
            else
                s += Dizaines[di] + (un == 1 && di != 8 ? "-et-un" : (un != 0 ? "-" + Unites[un] : "")) + (di == 8 && un == 0 ? "s" : "");
        }

        return s.Trim();
    }
}

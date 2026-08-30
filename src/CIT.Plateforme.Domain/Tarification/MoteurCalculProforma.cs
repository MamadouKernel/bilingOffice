using CIT.Plateforme.Domain.Tarification.Exceptions;

namespace CIT.Plateforme.Domain.Tarification;

/// <summary>
/// Moteur de calcul pur et déterministe de simulation de proforma (SIM-02, Annexe A).
/// Totalement découplé d'EF Core, Blazor, SQL Server ou de l'infrastructure (Principe I).
/// </summary>
public sealed class MoteurCalculProforma
{
    public const string MentionLegaleDefaut =
        "Cette simulation de proforma est fournie à titre purement indicatif sur la base des éléments déclarés. " +
        "Elle ne constitue pas une facture définitive et n'engage pas Côte d'Ivoire Terminal (CIT). " +
        "Les frais réels seront établis lors de la facturation officielle.";

    public ResultatSimulationProforma Calculer(DossierSimulation dossier, VersionGrilleTarifaire grille)
    {
        ArgumentNullException.ThrowIfNull(dossier);
        ArgumentNullException.ThrowIfNull(grille);

        if (dossier.DateEnlevementPrevisionnel < dossier.DateArrivee)
        {
            throw new ArgumentException(
                $"La date d'enlèvement prévisionnel ({dossier.DateEnlevementPrevisionnel}) ne peut pas être antérieure à la date d'arrivée ({dossier.DateArrivee}).",
                nameof(dossier));
        }

        if (dossier.Conteneurs.Count == 0)
        {
            throw new ArgumentException("Le dossier de simulation doit comporter au moins un conteneur.", nameof(dossier));
        }

        var parametres = grille.Parametres;
        var lignesResultat = new List<LigneResultatConteneur>(dossier.Conteneurs.Count);

        foreach (var conteneur in dossier.Conteneurs)
        {
            var ligneTarifaire = grille.ObtenirLigne(dossier.Sens, conteneur.Taille, conteneur.CategorieMarchandise)
                ?? throw new TarifNonTrouveException(dossier.Sens, conteneur.Taille, conteneur.CategorieMarchandise);

            // 1. Acconage & 2. Relevage
            var acconage = ligneTarifaire.Acconage;
            var relevage = ligneTarifaire.Relevage;

            // 3. Branchement frigorifique (Reefer uniquement)
            var branchement = 0m;
            if (conteneur.Type == TypeConteneur.Reefer)
            {
                var joursBranchement = Math.Max(0, dossier.DureeSejourJours - parametres.FranchiseBranchementJours);
                branchement = joursBranchement * ligneTarifaire.TarifJourBranchement;
            }

            // 4. Majoration Lourd (seuil > 15t en 20", > 26t en 40")
            var majorationLourd = 0m;
            var seuilLourd = conteneur.Taille == TailleConteneur.VingtPieds
                ? parametres.SeuilPoidsLourd20Pieds
                : parametres.SeuilPoidsLourd40Pieds;

            if (conteneur.PoidsTonnes > seuilLourd)
            {
                majorationLourd = parametres.TauxMajorationLourd * acconage;
            }

            // 5. Majoration Dangereux
            var majorationDangereux = conteneur.EstDangereux
                ? parametres.TauxMajorationDangereux * acconage
                : 0m;

            // 6. Majoration Hors Gabarit (OOG)
            var majorationOog = conteneur.EstHorsGabarit
                ? parametres.TauxMajorationOog * acconage
                : 0m;

            // 7. Stationnement & Répartition
            var stationnement = 0m;
            var partCit = 0m;
            var partOic = 0m;
            var partPaa = 0m;

            if (parametres.StationnementApplicable(dossier.Sens))
            {
                var franchiseStationnement = parametres.FranchiseStationnementJours(dossier.Sens);

                var joursStationnement = Math.Max(0, dossier.DureeSejourJours - franchiseStationnement);
                if (joursStationnement > 0)
                {
                    var j1 = Math.Min(5, joursStationnement);
                    var j2 = Math.Min(10, Math.Max(0, joursStationnement - 5));
                    var j3 = Math.Max(0, joursStationnement - 15);

                    stationnement = (j1 * parametres.TarifStationnementTranche1Jour) +
                                    (j2 * parametres.TarifStationnementTranche2Jour) +
                                    (j3 * parametres.TarifStationnementTranche3Jour);

                    partCit = stationnement * parametres.QuotePartCit;
                    partOic = stationnement * parametres.QuotePartOic;
                    partPaa = stationnement * parametres.QuotePartPaa;
                }
            }

            lignesResultat.Add(new LigneResultatConteneur(
                conteneur,
                acconage,
                relevage,
                branchement,
                majorationLourd,
                majorationDangereux,
                majorationOog,
                stationnement,
                partCit,
                partOic,
                partPaa));
        }

        return new ResultatSimulationProforma(
            dossier,
            grille.Id,
            grille.NumeroVersion,
            lignesResultat,
            MentionLegaleDefaut);
    }
}

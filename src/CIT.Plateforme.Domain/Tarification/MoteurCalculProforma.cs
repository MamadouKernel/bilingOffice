using CIT.Plateforme.Domain.Tarification.Exceptions;
using CIT.Plateforme.Domain.Tarification.Parametres;
using CIT.Plateforme.Domain.Tarification.Referentiels;

namespace CIT.Plateforme.Domain.Tarification;

/// <summary>
/// Moteur de calcul pur et déterministe de simulation de proforma (SIM-02, Annexe A).
/// Totalement découplé d'EF Core, Blazor, SQL Server ou de l'infrastructure (Principe I).
///
/// CE QUI A CHANGÉ, ET POURQUOI. Le moteur portait trois décisions métier <b>en dur</b> que le
/// CDC déclare pourtant administrables sans développement (SIM-06, RG-SIM-11) :
/// <list type="number">
///   <item>les bornes des tranches de stationnement — <c>Math.Min(5, …)</c>,
///   <c>joursStationnement - 15</c> — qui n'existaient nulle part comme donnée ;</item>
///   <item>le déclenchement du branchement frigorifique, écrit <c>Type == TypeConteneur.Reefer</c>,
///   donc impossible à étendre à un nouveau type sans recompiler ;</item>
///   <item>les trois bénéficiaires de la répartition du stationnement, nommés dans le code.</item>
/// </list>
/// Les trois sont désormais lues dans la grille. Le moteur ne nomme plus aucune tranche, aucun
/// type de conteneur, aucun bénéficiaire : il parcourt ce que le paramétrage lui donne.
/// </summary>
public sealed class MoteurCalculProforma
{
    public const string MentionLegaleDefaut =
        "Cette simulation de proforma est fournie à titre purement indicatif sur la base des éléments déclarés. " +
        "Elle ne constitue pas une facture définitive et n'engage pas Côte d'Ivoire Terminal (CIT). " +
        "Les frais réels seront établis lors de la facturation officielle.";

    public ResultatSimulationProforma Calculer(
        DossierSimulation dossier,
        VersionGrilleTarifaire grille,
        ReferentielsTarifaires referentiels)
    {
        ArgumentNullException.ThrowIfNull(dossier);
        ArgumentNullException.ThrowIfNull(grille);
        ArgumentNullException.ThrowIfNull(referentiels);

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

        var sens = referentiels.Sens(dossier.SensOperationId);
        var lignesResultat = new List<LigneResultatConteneur>(dossier.Conteneurs.Count);

        // RG-SIM-08 — calculé une fois : le stationnement dépend du dossier, pas du conteneur.
        var (stationnement, repartition) = CalculerStationnement(dossier, grille, sens);

        foreach (var conteneur in dossier.Conteneurs)
        {
            var taille = referentiels.Taille(conteneur.TailleConteneurId);
            var type = referentiels.Type(conteneur.TypeConteneurId);
            var categorie = referentiels.Categorie(conteneur.CategorieMarchandiseId);

            var ligneTarifaire = grille.ObtenirLigne(sens.Id, taille.Id, categorie.Id)
                ?? throw new TarifNonTrouveException(sens, taille, categorie);

            // 1. Acconage & 2. Relevage
            var acconage = ligneTarifaire.Acconage;
            var relevage = ligneTarifaire.Relevage;

            // 3. Branchement frigorifique — RG-SIM-05. Le type porte la décision, le moteur la lit.
            var branchement = 0m;
            if (type.DeclencheBranchementFrigorifique)
            {
                var joursBranchement = Math.Max(0, dossier.DureeSejourJours - grille.FranchiseBranchementJours);
                branchement = joursBranchement * ligneTarifaire.TarifJourBranchement;
            }

            // 4. Majoration Lourd — RG-SIM-06. Le seuil est propre à la taille, lu dans la grille.
            // Une taille sans seuil paramétré ne déclenche pas la majoration, plutôt que d'en
            // deviner un.
            var majorationLourd = 0m;
            if (grille.SeuilPoids(taille.Id) is decimal seuilLourd && conteneur.PoidsTonnes > seuilLourd)
            {
                majorationLourd = grille.TauxMajoration(TypeMajoration.Lourd) * acconage;
            }

            // 5. Majoration Dangereux & 6. Majoration Hors Gabarit — RG-SIM-07, cumulables entre
            // elles et avec la majoration lourd.
            var majorationDangereux = conteneur.EstDangereux
                ? grille.TauxMajoration(TypeMajoration.Dangereux) * acconage
                : 0m;

            var majorationOog = conteneur.EstHorsGabarit
                ? grille.TauxMajoration(TypeMajoration.HorsGabarit) * acconage
                : 0m;

            lignesResultat.Add(new LigneResultatConteneur(
                conteneur,
                taille,
                type,
                categorie,
                acconage,
                relevage,
                branchement,
                majorationLourd,
                majorationDangereux,
                majorationOog,
                stationnement,
                repartition));
        }

        return new ResultatSimulationProforma(
            dossier,
            sens,
            grille.Id,
            grille.NumeroVersion,
            lignesResultat,
            MentionLegaleDefaut);
    }

    /// <summary>
    /// RG-SIM-08 — stationnement sur la durée de séjour hors franchise, réparti par tranches à
    /// tarif journalier propre, puis ventilé entre bénéficiaires (RG-SIM-09).
    ///
    /// Le découpage n'est plus déroulé tranche par tranche : chaque tranche sait combien de jours
    /// lui reviennent (<see cref="TrancheStationnement.JoursApplicables"/>). C'est ce qui permet
    /// d'en ajouter une quatrième par paramétrage, sans toucher à ce code.
    /// </summary>
    private static (decimal Montant, IReadOnlyList<PartBeneficiaireStationnement> Repartition) CalculerStationnement(
        DossierSimulation dossier, VersionGrilleTarifaire grille, SensOperation sens)
    {
        if (!grille.StationnementApplicable(sens.Id))
        {
            return (0m, []);
        }

        var joursTaxables = Math.Max(0, dossier.DureeSejourJours - grille.FranchiseStationnementJours(sens.Id));
        if (joursTaxables == 0)
        {
            return (0m, []);
        }

        var montant = grille.TranchesOrdonnees
            .Sum(tranche => tranche.JoursApplicables(joursTaxables) * tranche.TarifJournalier);

        if (montant == 0m)
        {
            return (0m, []);
        }

        var repartition = grille.QuotesParts
            .OrderBy(q => q.Beneficiaire, StringComparer.Ordinal)
            .Select(q => new PartBeneficiaireStationnement(q.Beneficiaire, montant * q.Pourcentage))
            .ToList();

        return (montant, repartition);
    }
}

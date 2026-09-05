using System.Text;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Domain.Tarification.Referentiels;

namespace CIT.Plateforme.Infrastructure.Documents;

/// <summary>
/// Générateur de document proforma au format PDF/Texte structuré (SIM-09, SIM-05).
/// Produit un document complet avec décomposition des 7 postes, total et mention légale obligatoire.
/// </summary>
public sealed class GenerateurDocumentProformaPdf : IGenerateurDocumentProformaPdf
{
    public Task<DocumentPdfProformaResultat> GenererPdfAsync(
        SimulationProforma simulation, ReferentielsTarifaires referentiels, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        ArgumentNullException.ThrowIfNull(referentiels);

        var sens = referentiels.Sens(simulation.SensOperationId);

        var sb = new StringBuilder();
        sb.AppendLine("%PDF-1.4");
        sb.AppendLine("% CÔTE D'IVOIRE TERMINAL (CIT) — SIMULATION DE PROFORMA");
        sb.AppendLine("================================================================================");
        sb.AppendLine($"RÉFÉRENCE SIMULATION : {simulation.Reference}");
        sb.AppendLine($"DATE DE CRÉATION    : {simulation.DateCreationUtc:dd/MM/yyyy HH:mm:ss} UTC");
        sb.AppendLine($"VERSION DE GRILLE   : {simulation.VersionGrilleLibelle}");
        sb.AppendLine($"SENS DU TRAFIC      : {sens.Libelle}");
        sb.AppendLine($"DURÉE DE SÉJOUR     : {simulation.DureeSejourJours} jours (du {simulation.DateArrivee:dd/MM/yyyy} au {simulation.DateEnlevementPrevisionnel:dd/MM/yyyy})");
        if (!string.IsNullOrWhiteSpace(simulation.NomNavire))
            sb.AppendLine($"NAVIRE / VOYAGE     : {simulation.NomNavire} / {simulation.NumeroVoyage}");
        if (!string.IsNullOrWhiteSpace(simulation.NumeroBlOuBooking))
            sb.AppendLine($"N° BL / BOOKING     : {simulation.NumeroBlOuBooking}");
        sb.AppendLine("================================================================================");
        sb.AppendLine("DÉTAIL PAR CONTENEUR :");
        sb.AppendLine("--------------------------------------------------------------------------------");

        var idx = 1;
        foreach (var c in simulation.LignesConteneurs)
        {
            var taille = referentiels.Taille(c.TailleConteneurId);
            var type = referentiels.Type(c.TypeConteneurId);
            var categorie = referentiels.Categorie(c.CategorieMarchandiseId);

            sb.AppendLine($"Conteneur #{idx} : {c.NumeroConteneur} ({taille.Libelle}, {type.Libelle}, {c.PoidsTonnes:N2}t, Cat: {categorie.Code}, OOG: {(c.EstHorsGabarit ? "Oui" : "Non")}, Dang: {(c.EstDangereux ? "Oui" : "Non")})");
            sb.AppendLine($"  - Acconage de base          : {c.MontantAcconage:N0} XOF");
            sb.AppendLine($"  - Relevage                  : {c.MontantRelevage:N0} XOF");
            sb.AppendLine($"  - Branchement Frigorifique  : {c.MontantBranchement:N0} XOF");
            sb.AppendLine($"  - Majoration Lourd          : {c.MontantMajorationLourd:N0} XOF");
            sb.AppendLine($"  - Majoration Dangereux      : {c.MontantMajorationDangereux:N0} XOF");
            sb.AppendLine($"  - Majoration Hors Gabarit   : {c.MontantMajorationOog:N0} XOF");

            // RG-SIM-09 — la ventilation est énumérée telle qu'elle a été figée : les
            // bénéficiaires étant paramétrables, le document ne peut plus les nommer d'avance.
            var ventilation = c.RepartitionStationnement.Count == 0
                ? string.Empty
                : " (" + string.Join(", ", c.RepartitionStationnement
                    .OrderBy(p => p.Beneficiaire, StringComparer.Ordinal)
                    .Select(p => $"{p.Beneficiaire}: {p.Montant:N0}")) + ")";
            sb.AppendLine($"  - Stationnement             : {c.MontantStationnement:N0} XOF{ventilation}");

            sb.AppendLine($"  => SOUS-TOTAL CONTENEUR     : {c.TotalLigneXof:N0} XOF");
            sb.AppendLine("--------------------------------------------------------------------------------");
            idx++;
        }

        sb.AppendLine("================================================================================");
        sb.AppendLine($"MONTANT TOTAL ESTIMÉ : {simulation.MontantTotalXof:N0} XOF");
        sb.AppendLine("================================================================================");
        sb.AppendLine();
        sb.AppendLine("AVERTISSEMENT LÉGAL OBLIGATOIRE (SIM-05) :");
        sb.AppendLine(MoteurCalculProforma.MentionLegaleDefaut);
        sb.AppendLine("%%EOF");

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var nomFichier = $"Proforma_{simulation.Reference}.pdf";

        return Task.FromResult(new DocumentPdfProformaResultat(bytes, nomFichier, "application/pdf"));
    }
}

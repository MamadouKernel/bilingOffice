using System.Text;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Tarification;

namespace CIT.Plateforme.Infrastructure.Documents;

/// <summary>
/// Générateur de document proforma au format PDF/Texte structuré (SIM-09, SIM-05).
/// Produit un document complet avec décomposition des 7 postes, total et mention légale obligatoire.
/// </summary>
public sealed class GenerateurDocumentProformaPdf : IGenerateurDocumentProformaPdf
{
    public Task<DocumentPdfProformaResultat> GenererPdfAsync(SimulationProforma simulation, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(simulation);

        var sb = new StringBuilder();
        sb.AppendLine("%PDF-1.4");
        sb.AppendLine("% CÔTE D'IVOIRE TERMINAL (CIT) — SIMULATION DE PROFORMA");
        sb.AppendLine("================================================================================");
        sb.AppendLine($"RÉFÉRENCE SIMULATION : {simulation.Reference}");
        sb.AppendLine($"DATE DE CRÉATION    : {simulation.DateCreationUtc:dd/MM/yyyy HH:mm:ss} UTC");
        sb.AppendLine($"VERSION DE GRILLE   : {simulation.VersionGrilleLibelle}");
        sb.AppendLine($"SENS DU TRAFIC      : {simulation.Sens}");
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
            sb.AppendLine($"Conteneur #{idx} : {c.NumeroConteneur} ({c.Taille}\", {c.Type}, {c.PoidsTonnes:N2}t, Cat: {c.CategorieMarchandise}, OOG: {(c.EstHorsGabarit ? "Oui" : "Non")}, Dang: {(c.EstDangereux ? "Oui" : "Non")})");
            sb.AppendLine($"  - Acconage de base          : {c.MontantAcconage:N0} XOF");
            sb.AppendLine($"  - Relevage                  : {c.MontantRelevage:N0} XOF");
            sb.AppendLine($"  - Branchement Frigorifique  : {c.MontantBranchement:N0} XOF");
            sb.AppendLine($"  - Majoration Lourd          : {c.MontantMajorationLourd:N0} XOF");
            sb.AppendLine($"  - Majoration Dangereux      : {c.MontantMajorationDangereux:N0} XOF");
            sb.AppendLine($"  - Majoration Hors Gabarit   : {c.MontantMajorationOog:N0} XOF");
            sb.AppendLine($"  - Stationnement             : {c.MontantStationnement:N0} XOF (CIT: {c.PartCit:N0}, OIC: {c.PartOic:N0}, PAA: {c.PartPaa:N0})");
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

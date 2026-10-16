using CIT.Plateforme.Application.ActualisationBadt;

namespace CIT.Plateforme.Application.Abstractions;

/// <summary>
/// FEATURE 010 (Actualisation BADT) — rendu PDF du tableau de bord de reporting. Le formatage
/// concret (QuestPDF) est un détail de présentation laissé à l'Infrastructure (Principe IV) ; la
/// requête Application ne manipule que le DTO déjà agrégé.
/// </summary>
public interface IGenerateurReportingBadtPdf
{
    Task<byte[]> GenererAsync(RapportActualisationBadtDto rapport, DateTime debutUtc, DateTime finUtc, CancellationToken ct = default);
}

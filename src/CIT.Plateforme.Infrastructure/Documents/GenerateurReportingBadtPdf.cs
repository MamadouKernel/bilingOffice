using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.ActualisationBadt;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CIT.Plateforme.Infrastructure.Documents;

/// <summary>
/// FEATURE 010 (Actualisation BADT) — rendu PDF du tableau de bord de reporting (QuestPDF).
/// Reprend le DTO déjà agrégé par la requête ; aucun calcul ici.
/// </summary>
public sealed class GenerateurReportingBadtPdf : IGenerateurReportingBadtPdf
{
    private const string Navy = "#1B3A6B";

    static GenerateurReportingBadtPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenererAsync(RapportActualisationBadtDto r, DateTime debutUtc, DateTime finUtc, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(r);

        var document = Document.Create(conteneur =>
        {
            conteneur.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(t => t.FontSize(9).FontColor("#1a1a1a"));

                page.Header().Column(col =>
                {
                    col.Item().Text("Reporting Actualisation BADT").Bold().FontSize(15).FontColor(Navy);
                    col.Item().Text($"Période du {debutUtc:dd/MM/yyyy} au {finUtc:dd/MM/yyyy} (UTC)").FontSize(8).FontColor("#666");
                    col.Item().PaddingTop(6).BorderBottom(2).BorderColor(Navy);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(12);

                    col.Item().Text("Indicateurs").Bold().FontSize(11).FontColor(Navy);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(1); });
                        void Kpi(string libelle, string valeur)
                        {
                            t.Cell().PaddingVertical(2).Text(libelle);
                            t.Cell().PaddingVertical(2).AlignRight().Text(valeur).SemiBold();
                        }
                        Kpi("Demandes (période)", r.TotalDemandes.ToString());
                        Kpi("BL non dépliés en attente", r.BlNonDeplies.ToString());
                        Kpi("Escalades", r.NombreEscalades.ToString());
                        Kpi("Délai moyen qualif.→décision", r.DelaiMoyen.HasValue ? FormaterDuree(r.DelaiMoyen.Value) : "—");
                        Kpi("Accords", r.NombreAccords.ToString());
                        Kpi("Refus", r.NombreRefus.ToString());
                        Kpi("Renvois", r.NombreRenvois.ToString());
                        Kpi("Rejets agent", r.NombreRejetsAgent.ToString());
                    });

                    col.Item().Text("Par acteur").Bold().FontSize(11).FontColor(Navy);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            for (var i = 0; i < 6; i++) c.RelativeColumn(1);
                        });
                        t.Header(h =>
                        {
                            foreach (var titre in new[] { "Acteur", "Qualif.", "Dépl.", "Rejets", "Accords", "Refus", "Renvois" })
                            {
                                h.Cell().Background("#eef2f7").Padding(3).Text(titre).SemiBold();
                            }
                        });
                        foreach (var a in r.ParActeur)
                        {
                            t.Cell().Padding(3).Text(a.Nom);
                            t.Cell().Padding(3).AlignRight().Text(a.Qualifications.ToString());
                            t.Cell().Padding(3).AlignRight().Text(a.Depliages.ToString());
                            t.Cell().Padding(3).AlignRight().Text(a.RejetsAgent.ToString());
                            t.Cell().Padding(3).AlignRight().Text(a.Accords.ToString());
                            t.Cell().Padding(3).AlignRight().Text(a.Refus.ToString());
                            t.Cell().Padding(3).AlignRight().Text(a.Renvois.ToString());
                        }
                    });
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return Task.FromResult(document.GeneratePdf());
    }

    private static string FormaterDuree(TimeSpan d) =>
        d.TotalDays >= 1 ? $"{(int)d.TotalDays}j {d.Hours}h" : $"{(int)d.TotalHours}h {d.Minutes}min";
}

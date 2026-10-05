using System.Globalization;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Tarification;
using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Domain.Tarification.Referentiels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CIT.Plateforme.Infrastructure.Documents;

/// <summary>
/// Générateur de la facture proforma au format PDF réel (SIM-09, SIM-05), au design officiel CIT
/// (en-tête, méta dossier, rubriques agrégées, total, montant en toutes lettres, mention légale).
/// Rend exactement les montants figés à la simulation — aucun recalcul.
/// </summary>
public sealed class GenerateurDocumentProformaPdf : IGenerateurDocumentProformaPdf
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");
    private const string Navy = "#1B3A6B";
    private const string Gold = "#C9A84C";

    static GenerateurDocumentProformaPdf()
    {
        // Licence Community (gratuite) — requise avant toute génération.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<DocumentPdfProformaResultat> GenererPdfAsync(
        SimulationProforma simulation, ReferentielsTarifaires referentiels, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        ArgumentNullException.ThrowIfNull(referentiels);

        var sens = referentiels.Sens(simulation.SensOperationId);
        var trafic = sens.Libelle.StartsWith("Export", StringComparison.OrdinalIgnoreCase) ? "Export" : "Import";
        var rubriques = ConstruireRubriques(simulation, referentiels, sens.Libelle);
        var total = simulation.MontantTotalXof;

        var document = Document.Create(conteneur =>
        {
            conteneur.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(t => t.FontSize(9).FontColor("#1a1a1a"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("CÔTE D'IVOIRE TERMINAL").Bold().FontSize(14).FontColor(Navy);
                            c.Item().Text("ABIDJAN").FontSize(9).FontColor(Gold).LetterSpacing(0.2f);
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Text(TexteProforma.T("FACTURE PROFORMA")).Bold().FontSize(14);
                            c.Item().AlignRight().Text(TexteProforma.T("Simulation — document non contractuel")).FontSize(8).FontColor("#666");
                            c.Item().AlignRight().Text($"N° {simulation.Reference}").FontSize(9).FontColor("#333");
                        });
                    });
                    col.Item().PaddingTop(6).BorderBottom(2).BorderColor(Navy);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(10);

                    // Méta — deux colonnes clé/valeur
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            Meta(c, TexteProforma.T("Client"), Ou(simulation.ClientAFacturer));
                            Meta(c, TexteProforma.T("Type client"), Ou(TexteProforma.T(simulation.TypeClientLibelle ?? "")));
                            Meta(c, TexteProforma.T("Trafic"), trafic);
                            Meta(c, TexteProforma.T("Port de chargement"), Ou(simulation.PortChargement));
                            Meta(c, TexteProforma.T("Port de déchargement"), Ou(simulation.PortDechargement));
                        });
                        row.ConstantItem(20);
                        row.RelativeItem().Column(c =>
                        {
                            Meta(c, TexteProforma.T("Date d'arrivée navire"), simulation.DateArrivee.ToString("dd/MM/yyyy", Fr));
                            Meta(c, TexteProforma.T("Date d'enlèvement"), simulation.DateEnlevementPrevisionnel.ToString("dd/MM/yyyy", Fr));
                            Meta(c, TexteProforma.T("Durée de séjour"), $"{simulation.DureeSejourJours} {TexteProforma.T("jour(s)")}");
                            Meta(c, TexteProforma.T("Escale"), Ou(simulation.Escale));
                            Meta(c, TexteProforma.T("Grille appliquée"), Ou(simulation.VersionGrilleLibelle));
                        });
                    });

                    // Escale : navire / voyage / BL
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });
                        t.Header(h =>
                        {
                            EnteteEscale(h, TexteProforma.T("Navire"));
                            EnteteEscale(h, TexteProforma.T("Voyage"));
                            EnteteEscale(h, "BL / Booking");
                        });
                        t.Cell().Border(0.5f).BorderColor("#cfd6e2").Padding(4).Text(Ou(simulation.NomNavire));
                        t.Cell().Border(0.5f).BorderColor("#cfd6e2").Padding(4).Text(Ou(simulation.NumeroVoyage));
                        t.Cell().Border(0.5f).BorderColor("#cfd6e2").Padding(4).Text(Ou(simulation.NumeroBlOuBooking));
                    });

                    // Rubriques
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(4); c.RelativeColumn(1.4f); c.RelativeColumn(1.4f); });
                        t.Header(h =>
                        {
                            h.Cell().Background(Navy).Padding(5).Text(TexteProforma.T("Rubriques")).FontColor("#fff").Bold();
                            h.Cell().Background(Navy).Padding(5).AlignRight().Text(TexteProforma.T("Montant CFA")).FontColor("#fff").Bold();
                            h.Cell().Background(Navy).Padding(5).AlignRight().Text(TexteProforma.T("Total CFA")).FontColor("#fff").Bold();
                        });

                        if (rubriques.Count == 0)
                        {
                            t.Cell().ColumnSpan(3).Padding(8).Text(TexteProforma.T("Aucune rubrique facturable.")).Italic().FontColor("#999");
                        }
                        foreach (var r in rubriques)
                        {
                            t.Cell().BorderBottom(0.5f).BorderColor("#e5e5e5").Padding(5).Column(c =>
                            {
                                c.Item().Text(r.Presta).Bold().FontColor("#111");
                                c.Item().Text($"{r.Qte} Unité(s)  *  {Fmt(r.Pu)} XOF").FontSize(8).FontColor("#555");
                            });
                            t.Cell().BorderBottom(0.5f).BorderColor("#e5e5e5").Padding(5).AlignRight().Text(Fmt(r.Total));
                            t.Cell().BorderBottom(0.5f).BorderColor("#e5e5e5").Padding(5).AlignRight().Text(Fmt(r.Total)).Bold().FontColor(Navy);
                        }
                    });

                    // Totaux
                    col.Item().AlignRight().Width(260).Column(c =>
                    {
                        LigneTotal(c, TexteProforma.T("Montant Total Hors Taxes Prestations"), $"{Fmt(total)} CFA");
                        LigneTotal(c, TexteProforma.T("Montant Total TVA"), "0 CFA");
                        c.Item().PaddingTop(4).BorderTop(2).BorderColor(Navy).PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Text(TexteProforma.T("Montant Total à Payer")).Bold().FontColor(Navy).FontSize(11);
                            row.ConstantItem(120).AlignRight().Text($"{Fmt(total)} CFA").Bold().FontColor(Navy).FontSize(11);
                        });
                    });

                    if (total > 0)
                    {
                        col.Item().Background("#fafafa").Border(0.5f).BorderColor("#eee").Padding(6)
                            .Text(TexteProforma.MontantEnLettres(total).ToUpperInvariant()).Italic().FontSize(8.5f).FontColor("#333");
                    }

                    col.Item().PaddingTop(6).BorderTop(0.5f).BorderColor("#ddd").PaddingTop(6)
                        .Text(TexteProforma.T(MoteurCalculProforma.MentionLegaleDefaut)).FontSize(7.5f).FontColor("#555");
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Côte d'Ivoire Terminal — ").FontSize(7).FontColor("#999");
                    x.Span($"{simulation.Reference} — {simulation.DateCreationUtc:dd/MM/yyyy}").FontSize(7).FontColor("#999");
                });
            });
        });

        var bytes = document.GeneratePdf();
        var nomFichier = $"Proforma_{simulation.Reference}.pdf";
        return Task.FromResult(new DocumentPdfProformaResultat(bytes, nomFichier, "application/pdf"));
    }

    private static void Meta(ColumnDescriptor c, string cle, string valeur) =>
        c.Item().BorderBottom(0.5f).BorderColor("#ddd").PaddingVertical(1).Row(row =>
        {
            row.RelativeItem().Text(cle).Bold().FontColor("#333").FontSize(8.5f);
            row.RelativeItem().AlignRight().Text(valeur).FontSize(8.5f);
        });

    private static void EnteteEscale(TableCellDescriptor h, string titre) =>
        h.Cell().Background("#f0f3f8").Border(0.5f).BorderColor("#cfd6e2").Padding(4)
            .Text(titre).FontColor(Navy).Bold().FontSize(8.5f);

    private static void LigneTotal(ColumnDescriptor c, string libelle, string valeur) =>
        c.Item().Row(row =>
        {
            row.RelativeItem().Text(libelle).FontSize(9);
            row.ConstantItem(120).AlignRight().Text(valeur).FontSize(9);
        });

    private static string Ou(string? v) => string.IsNullOrWhiteSpace(v) ? "—" : v;

    private static string Fmt(decimal n) => Math.Round(n).ToString("N0", Fr);

    private sealed record Rubrique(string Presta, int Qte, decimal Pu, decimal Total, int Ordre);

    private static IReadOnlyList<Rubrique> ConstruireRubriques(
        SimulationProforma simulation, ReferentielsTarifaires referentiels, string sensLibelle)
    {
        var groupes = new Dictionary<string, (string Presta, int Qte, decimal Pu, decimal Total, int Ordre)>();

        void Ajouter(string lib, string tailleCode, decimal pu, int ordre)
        {
            if (pu <= 0) return;
            var cle = $"{lib}|{tailleCode}|{pu}";
            var presta = $"{sensLibelle} - {lib} {tailleCode}";
            if (groupes.TryGetValue(cle, out var g))
                groupes[cle] = (presta, g.Qte + 1, pu, g.Total + pu, ordre);
            else
                groupes[cle] = (presta, 1, pu, pu, ordre);
        }

        foreach (var l in simulation.LignesConteneurs)
        {
            var t = referentiels.Taille(l.TailleConteneurId).Code;
            Ajouter(TexteProforma.T("Acconage Plein"), t, l.MontantAcconage, 1);
            Ajouter(TexteProforma.T("Relevage Plein"), t, l.MontantRelevage, 2);
            Ajouter(TexteProforma.T("Branchement Frigo"), t, l.MontantBranchement, 3);
            Ajouter(TexteProforma.T("Majoration Lourd"), t, l.MontantMajorationLourd, 4);
            Ajouter(TexteProforma.T("Majoration Dangereux"), t, l.MontantMajorationDangereux, 5);
            Ajouter(TexteProforma.T("Majoration Hors Gabarit"), t, l.MontantMajorationOog, 6);
            foreach (var part in l.RepartitionStationnement)
                Ajouter($"{TexteProforma.T("Stationnement")} {part.Beneficiaire}", t, part.Montant, 7);
        }

        return groupes.Values
            .OrderBy(g => g.Ordre)
            .ThenBy(g => g.Presta, StringComparer.Ordinal)
            .Select(g => new Rubrique(g.Presta, g.Qte, g.Pu, g.Total, g.Ordre))
            .ToList();
    }
}

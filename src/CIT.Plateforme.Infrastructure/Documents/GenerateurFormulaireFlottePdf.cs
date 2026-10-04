using System.Globalization;
using CIT.Plateforme.Application.Enrolement;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CIT.Plateforme.Infrastructure.Documents;

/// <summary>
/// Formulaires officiels CIT-CIV-CG-F-0001 (ajout / retrait de camions) et F-0002 (enregistrement /
/// retrait de chauffeurs), rendus à l'identique du modèle papier, blancs remplis par la demande
/// en ligne, avec un bloc de signature électronique horodaté (compte YABA ayant soumis).
/// </summary>
public sealed class GenerateurFormulaireFlottePdf : IGenerateurFormulaireFlotte
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    public byte[] Generer(DemandeFormulaireFlotte demande)
    {
        demande.Valider();
        QuestPDF.Settings.License = LicenseType.Community;

        var camion = demande.Type is TypeFormulaireFlotte.AjoutCamion or TypeFormulaireFlotte.RetraitCamion;
        var ajout = demande.Type is TypeFormulaireFlotte.AjoutCamion or TypeFormulaireFlotte.AjoutChauffeur;
        var objet = demande.Type switch
        {
            TypeFormulaireFlotte.AjoutCamion => "une demande d'ajout de camion(s) pour le compte de notre compagnie.",
            TypeFormulaireFlotte.RetraitCamion => "une demande de retrait de camions enregistrés au préalable sous notre société.",
            TypeFormulaireFlotte.AjoutChauffeur => "une demande d'enrôlement de nos chauffeurs pour le compte de notre compagnie.",
            _ => "une demande de retrait de chauffeurs au sein de notre compagnie.",
        };
        var intituleListe = camion ? "Ci-dessous la liste des immatriculations concernées :" : "Les chauffeurs à considérer sont :";
        var nb = !ajout ? null : camion
            ? "NB : Pour tout ajout de camions merci de fournir une copie de la carte grise du camion."
            : "NB : Pour tout ajout de chauffeurs merci de fournir une copie de son permis de conduire et prévoir une présence physique du chauffeur pour l'établissement de sa carte.";
        var date = demande.DateUtc.ToString("dd/MM/yyyy", Fr);

        return Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontSize(11).FontColor("#1a1a1a"));

            page.Header().Row(r =>
            {
                r.RelativeItem().Text("Côte d'Ivoire Terminal").Bold().FontColor("#0b3a5b");
                r.RelativeItem().AlignRight().Text(FormulairesFlotte.Reference(demande.Type)).FontSize(9).FontColor("#555");
            });

            page.Content().PaddingVertical(20).Column(c =>
            {
                c.Spacing(14);
                c.Item().AlignCenter().Text(FormulairesFlotte.Titre(demande.Type)).Bold().FontSize(15);
                c.Item().Text(t =>
                {
                    t.Span("Nous soussignés, Mme/M ");
                    t.Span(demande.SignataireNom).Bold();
                    t.Span(",");
                });
                c.Item().Text(t =>
                {
                    t.Span("Agissant en qualité de ");
                    t.Span(demande.SignataireQualite).Bold();
                    t.Span(" pour le compte de la société ");
                    t.Span(demande.SocieteNom).Bold();
                    t.Span(" dont le siège est à ");
                    t.Span(demande.SocieteSiege).Bold();
                    t.Span($", avons l'honneur de solliciter {objet}");
                });
                c.Item().Text(intituleListe);
                c.Item().PaddingLeft(15).Column(liste =>
                {
                    for (var i = 0; i < demande.Objets.Count; i++)
                    {
                        liste.Item().PaddingBottom(4).Text($"{i + 1}. {demande.Objets[i]}");
                    }
                });
                if (nb is not null)
                {
                    c.Item().PaddingTop(6).Text(nb).Italic().FontSize(10);
                }
                c.Item().PaddingTop(20).AlignRight().Column(s =>
                {
                    s.Item().Text($"Fait à {demande.SocieteSiege}, le {date}");
                    s.Item().Text(t => { t.Span("Le signataire : "); t.Span($"{demande.SignataireNom} ({demande.SignataireQualite})").Bold(); });
                });
                c.Item().PaddingTop(16).Border(0.5f).BorderColor("#9aa7b2").Padding(8).Text(
                    $"Formulaire établi et signé électroniquement sur la plateforme YABA par le compte {demande.CompteSignataire}, " +
                    $"le {demande.DateUtc.ToString("dd/MM/yyyy à HH:mm", Fr)} (UTC). Il tient lieu du formulaire papier signé.")
                    .FontSize(8).FontColor("#555");
            });

            page.Footer().AlignCenter().Text(t =>
            {
                t.Span("YABA — Côte d'Ivoire Terminal · ").FontSize(8).FontColor("#777");
                t.Span(FormulairesFlotte.Reference(demande.Type)).FontSize(8).FontColor("#777");
            });
        })).GeneratePdf();
    }
}

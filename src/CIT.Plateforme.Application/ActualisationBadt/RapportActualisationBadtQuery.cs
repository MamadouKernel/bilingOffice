using ClosedXML.Excel;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.ActualisationBadt;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.ActualisationBadt;

/// <summary>Une ligne du tableau « par acteur » du reporting BADT (nom résolu).</summary>
public sealed record LigneActeurBadtDto(
    Guid ActeurId,
    string Nom,
    int Qualifications,
    int Depliages,
    int RejetsAgent,
    int Accords,
    int Refus,
    int Renvois,
    double TauxAccord,
    double TauxRefus,
    double TauxRenvoi);

/// <summary>Une trace de dépliage affichée (nom de l'acteur résolu).</summary>
public sealed record TraceDepliageDto(string ActeurNom, string NumeroBl, string NumeroConteneur, DateTime HorodatageUtc);

/// <summary>Tableau de bord complet du reporting « Actualisation BADT » sur une période.</summary>
public sealed record RapportActualisationBadtDto(
    int TotalDemandes,
    int BlNonDeplies,
    int NombreEscalades,
    int NombreAccords,
    int NombreRefus,
    int NombreRenvois,
    int NombreRejetsAgent,
    TimeSpan? DelaiMoyen,
    IReadOnlyList<LigneActeurBadtDto> ParActeur,
    IReadOnlyList<TraceDepliageDto> Depliages,
    IReadOnlyList<VolumePeriodeBadt> VolumeParJour);

/// <summary>Une action individuelle d'un acteur (drill-down du tableau « par acteur »).</summary>
public sealed record DetailMesureBadtDto(
    TypeMesureBadt Type,
    string NumeroBl,
    string NumeroConteneur,
    CauseExpirationBadt? Cause,
    int? NombreJoursAccordes,
    DateTime HorodatageUtc);

/// <summary>
/// FEATURE 010 (Actualisation BADT) — reporting (approche B). Lit la table de mesures sur la période
/// et l'état vivant des BL non dépliés, puis résout les noms d'acteurs. Lecture seule, réservée aux
/// rôles BADT (instruire / décider) et à la supervision.
/// </summary>
public sealed class RapportActualisationBadtQuery(
    IMesureActualisationBadtRepository mesures,
    ILigneActualisationBadtRepository lignes,
    IUtilisateurInfoRepository utilisateurs,
    IGenerateurReportingBadtPdf generateurPdf,
    IAutorisationService autorisation)
{
    public async Task<RapportActualisationBadtDto> ExecuterAsync(
        Guid demandeurId, DateTime debutUtc, DateTime finUtc, CancellationToken ct = default)
    {
        await ExigerAccesReportingAsync(demandeurId, ct).ConfigureAwait(false);

        var lignesMesure = await mesures.ListerEntreAsync(debutUtc, finUtc, ct).ConfigureAwait(false);
        var blNonDeplies = await lignes.CompterBlNonDepliesEnAttenteAsync(ct).ConfigureAwait(false);

        var resultat = CalculateurReportingBadt.Agreger(lignesMesure);

        var acteurIds = resultat.ParActeur.Select(a => a.ActeurId)
            .Concat(resultat.Depliages.Select(d => d.ActeurId))
            .Distinct();
        var noms = await utilisateurs.ObtenirInfosAsync(acteurIds, ct).ConfigureAwait(false);
        string Nom(Guid id) => noms.TryGetValue(id, out var info) ? (info.NomAffiche ?? info.Email) : string.Empty;

        return new RapportActualisationBadtDto(
            resultat.TotalDemandes,
            blNonDeplies,
            resultat.NombreEscalades,
            resultat.NombreAccords,
            resultat.NombreRefus,
            resultat.NombreRenvois,
            resultat.NombreRejetsAgent,
            resultat.DelaiMoyenQualificationDecision,
            resultat.ParActeur
                .Select(a => new LigneActeurBadtDto(
                    a.ActeurId, Nom(a.ActeurId), a.Qualifications, a.Depliages, a.RejetsAgent,
                    a.Accords, a.Refus, a.Renvois, a.TauxAccord, a.TauxRefus, a.TauxRenvoi))
                .ToList(),
            resultat.Depliages
                .Select(d => new TraceDepliageDto(Nom(d.ActeurId), d.NumeroBl, d.NumeroConteneur, d.HorodatageUtc))
                .ToList(),
            resultat.VolumeParJour);
    }

    /// <summary>Drill-down : les actions individuelles d'un acteur sur la période (requalif., décisions, dépliages…).</summary>
    public async Task<IReadOnlyList<DetailMesureBadtDto>> DetailActeurAsync(
        Guid demandeurId, Guid acteurId, DateTime debutUtc, DateTime finUtc, CancellationToken ct = default)
    {
        await ExigerAccesReportingAsync(demandeurId, ct).ConfigureAwait(false);

        return (await mesures.ListerEntreAsync(debutUtc, finUtc, ct).ConfigureAwait(false))
            .Where(m => m.ActeurId == acteurId)
            .OrderByDescending(m => m.HorodatageUtc)
            .Select(m => new DetailMesureBadtDto(m.Type, m.NumeroBl, m.NumeroConteneur, m.Cause, m.NombreJoursAccordes, m.HorodatageUtc))
            .ToList();
    }

    /// <summary>Export XLSX du tableau de bord (KPI, par acteur, dépliages, volume par jour).</summary>
    public async Task<byte[]> ExporterXlsxAsync(Guid demandeurId, DateTime debutUtc, DateTime finUtc, CancellationToken ct = default)
    {
        var r = await ExecuterAsync(demandeurId, debutUtc, finUtc, ct).ConfigureAwait(false);

        using var classeur = new XLWorkbook();

        var kpi = classeur.Worksheets.Add("Indicateurs");
        kpi.Cell(1, 1).Value = "Indicateur";
        kpi.Cell(1, 2).Value = "Valeur";
        kpi.Row(1).Style.Font.Bold = true;
        var lignesKpi = new (string, string)[]
        {
            ("Demandes (période)", r.TotalDemandes.ToString()),
            ("BL non dépliés en attente", r.BlNonDeplies.ToString()),
            ("Escalades", r.NombreEscalades.ToString()),
            ("Délai moyen qualif.→décision", r.DelaiMoyen?.ToString() ?? "—"),
            ("Accords", r.NombreAccords.ToString()),
            ("Refus", r.NombreRefus.ToString()),
            ("Renvois", r.NombreRenvois.ToString()),
            ("Rejets agent", r.NombreRejetsAgent.ToString()),
        };
        for (var i = 0; i < lignesKpi.Length; i++)
        {
            kpi.Cell(i + 2, 1).Value = lignesKpi[i].Item1;
            kpi.Cell(i + 2, 2).Value = lignesKpi[i].Item2;
        }
        kpi.Columns().AdjustToContents();

        var acteurs = classeur.Worksheets.Add("Par acteur");
        string[] entetesActeur = ["Acteur", "Qualif.", "Dépliages", "Rejets", "Accords", "Refus", "Renvois", "Taux accord %", "Taux refus %", "Taux renvoi %"];
        for (var i = 0; i < entetesActeur.Length; i++) acteurs.Cell(1, i + 1).Value = entetesActeur[i];
        acteurs.Row(1).Style.Font.Bold = true;
        var ligneA = 2;
        foreach (var a in r.ParActeur)
        {
            acteurs.Cell(ligneA, 1).Value = a.Nom;
            acteurs.Cell(ligneA, 2).Value = a.Qualifications;
            acteurs.Cell(ligneA, 3).Value = a.Depliages;
            acteurs.Cell(ligneA, 4).Value = a.RejetsAgent;
            acteurs.Cell(ligneA, 5).Value = a.Accords;
            acteurs.Cell(ligneA, 6).Value = a.Refus;
            acteurs.Cell(ligneA, 7).Value = a.Renvois;
            acteurs.Cell(ligneA, 8).Value = Math.Round(a.TauxAccord, 1);
            acteurs.Cell(ligneA, 9).Value = Math.Round(a.TauxRefus, 1);
            acteurs.Cell(ligneA, 10).Value = Math.Round(a.TauxRenvoi, 1);
            ligneA++;
        }
        acteurs.Columns().AdjustToContents();

        var depliages = classeur.Worksheets.Add("Dépliages");
        string[] entetesDep = ["BL", "Conteneur", "Agent", "Date (UTC)"];
        for (var i = 0; i < entetesDep.Length; i++) depliages.Cell(1, i + 1).Value = entetesDep[i];
        depliages.Row(1).Style.Font.Bold = true;
        var ligneD = 2;
        foreach (var d in r.Depliages)
        {
            depliages.Cell(ligneD, 1).Value = d.NumeroBl;
            depliages.Cell(ligneD, 2).Value = d.NumeroConteneur;
            depliages.Cell(ligneD, 3).Value = d.ActeurNom;
            depliages.Cell(ligneD, 4).Value = d.HorodatageUtc.ToString("yyyy-MM-dd HH:mm");
            ligneD++;
        }
        depliages.Columns().AdjustToContents();

        var volume = classeur.Worksheets.Add("Volume par jour");
        volume.Cell(1, 1).Value = "Jour";
        volume.Cell(1, 2).Value = "Demandes";
        volume.Row(1).Style.Font.Bold = true;
        var ligneV = 2;
        foreach (var v in r.VolumeParJour)
        {
            volume.Cell(ligneV, 1).Value = v.Jour.ToString("yyyy-MM-dd");
            volume.Cell(ligneV, 2).Value = v.Nombre;
            ligneV++;
        }
        volume.Columns().AdjustToContents();

        using var flux = new MemoryStream();
        classeur.SaveAs(flux);
        return flux.ToArray();
    }

    /// <summary>Export PDF du tableau de bord (rendu délégué à la couche Infrastructure via QuestPDF).</summary>
    public async Task<byte[]> ExporterPdfAsync(Guid demandeurId, DateTime debutUtc, DateTime finUtc, CancellationToken ct = default)
    {
        var r = await ExecuterAsync(demandeurId, debutUtc, finUtc, ct).ConfigureAwait(false);
        return await generateurPdf.GenererAsync(r, debutUtc, finUtc, ct).ConfigureAwait(false);
    }

    private async Task ExigerAccesReportingAsync(Guid demandeurId, CancellationToken ct)
    {
        var instruire = await autorisation.EstAutoriseAsync(demandeurId, Permission.ActualisationBadtInstruire, null, ct).ConfigureAwait(false);
        var decider = await autorisation.EstAutoriseAsync(demandeurId, Permission.ActualisationBadtDecider, null, ct).ConfigureAwait(false);
        var supervision = await autorisation.EstAutoriseAsync(demandeurId, Permission.SupervisionConsulter, null, ct).ConfigureAwait(false);
        if (!instruire && !decider && !supervision)
        {
            throw new AccesRefuseException("Accès au reporting BADT réservé aux rôles BADT et à la supervision.");
        }
    }
}

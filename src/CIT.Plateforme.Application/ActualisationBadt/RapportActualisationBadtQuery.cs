using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.ActualisationBadt;

/// <summary>Une ligne du tableau « par acteur » du reporting BADT (nom résolu).</summary>
public sealed record LigneActeurBadtDto(
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
    IReadOnlyList<TraceDepliageDto> Depliages);

/// <summary>
/// FEATURE 010 (Actualisation BADT) — reporting (approche B). Lit la table de mesures sur la période
/// et l'état vivant des BL non dépliés, puis résout les noms d'acteurs. Lecture seule, réservée aux
/// rôles BADT (instruire / décider) et à la supervision.
/// </summary>
public sealed class RapportActualisationBadtQuery(
    IMesureActualisationBadtRepository mesures,
    ILigneActualisationBadtRepository lignes,
    IUtilisateurInfoRepository utilisateurs,
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
                    Nom(a.ActeurId), a.Qualifications, a.Depliages, a.RejetsAgent,
                    a.Accords, a.Refus, a.Renvois, a.TauxAccord, a.TauxRefus, a.TauxRenvoi))
                .ToList(),
            resultat.Depliages
                .Select(d => new TraceDepliageDto(Nom(d.ActeurId), d.NumeroBl, d.NumeroConteneur, d.HorodatageUtc))
                .ToList());
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

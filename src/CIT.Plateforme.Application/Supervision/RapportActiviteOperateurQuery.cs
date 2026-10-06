using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Supervision;

/// <summary>Une ligne du rapport d'activité opérateur, nom résolu.</summary>
public sealed record LigneActiviteOperateurVue(
    Guid OperateurId,
    string Nom,
    int TicketsPrisEnCharge,
    int TicketsTraites,
    TimeSpan DelaiMoyenPriseEnCharge,
    TimeSpan DelaiMoyenTraitement,
    DateTime? DerniereActiviteUtc);

/// <summary>
/// §9.9.3 — rapport d'activité opérateur sur une période, BORNÉ AU PÉRIMÈTRE du superviseur
/// (SEC-02/03) et réservé à <see cref="Permission.SupervisionConsulter"/>. Finalité : pilotage
/// opérationnel (§9.8), non notation individuelle. Lecture seule.
/// </summary>
public sealed class RapportActiviteOperateurQuery(
    IPassageEnFileRepository passages,
    IFileDeTraitementRepository files,
    IPerimetreSupervisionRepository perimetres,
    IUtilisateurInfoRepository utilisateurs,
    IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<LigneActiviteOperateurVue>> ExecuterAsync(
        Guid superviseurId, DateTime debutUtc, DateTime finUtc, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(superviseurId, Permission.SupervisionConsulter, ct: ct).ConfigureAwait(false);

        var perimetre = await perimetres.ObtenirParSuperviseurAsync(superviseurId, ct).ConfigureAwait(false);
        if (perimetre is null)
        {
            return [];
        }

        var filesDuPerimetre = (await files.ListerAsync(ct).ConfigureAwait(false))
            .Where(f => perimetre.CouvreLaFile(f.Id))
            .Select(f => f.Id)
            .ToList();
        if (filesDuPerimetre.Count == 0)
        {
            return [];
        }

        var passagesPeriode = await passages
            .ListerParFilesEtPeriodeAsync(filesDuPerimetre, debutUtc, finUtc, ct).ConfigureAwait(false);

        var activites = CalculateurActiviteOperateur.Calculer(passagesPeriode, debutUtc, finUtc);
        if (activites.Count == 0)
        {
            return [];
        }

        var infos = await utilisateurs
            .ObtenirInfosAsync(activites.Select(a => a.OperateurId), ct).ConfigureAwait(false);

        return activites.Select(a =>
        {
            var info = infos.GetValueOrDefault(a.OperateurId);
            return new LigneActiviteOperateurVue(
                a.OperateurId,
                info?.NomAffiche ?? info?.Email ?? string.Empty,
                a.TicketsPrisEnCharge,
                a.TicketsTraites,
                a.DelaiMoyenPriseEnCharge,
                a.DelaiMoyenTraitement,
                a.DerniereActiviteUtc);
        }).ToList();
    }
}

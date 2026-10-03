using CIT.Plateforme.Application.Abstractions;

namespace CIT.Plateforme.Application.Enrolement;

/// <summary>
/// Rappel avant échéance des documents déclarés à l'enrôlement de flotte — permis, assurance,
/// visite technique (décision du 03/10/2026). UN rappel par document : une fois émis, il est
/// marqué et ne repart plus. Le préavis est celui des badges (<c>DelaiRappelExpirationJours</c>),
/// <see cref="PreavisParDefautJours"/> jours tant qu'il n'est pas administré.
/// </summary>
public sealed class RappelerEcheancesDocumentsCommand(
    IEcheanceDocumentEnrolementRepository echeances,
    IJournalisationService journalisation,
    IHorloge horloge,
    IParametresEnrolementProvider parametres,
    IUniteDeTravail uniteDeTravail)
{
    public const int PreavisParDefautJours = 30;

    public async Task<int> ExecuterAsync(CancellationToken ct = default)
    {
        var aujourdhui = DateOnly.FromDateTime(horloge.MaintenantUtc);
        var seuil = aujourdhui.AddDays(parametres.ObtenirDelaiRappelExpirationJours() ?? PreavisParDefautJours);

        var aRappeler = await echeances.ListerARappelerAsync(seuil, ct).ConfigureAwait(false);
        foreach (var echeance in aRappeler)
        {
            var chargeUtile = $"{{\"ticketId\":\"{echeance.TicketId}\",\"document\":\"{echeance.Document}\",\"dateExpiration\":\"{echeance.DateExpiration:yyyy-MM-dd}\"}}";
            await journalisation.PublierEvenementAsync("RappelEcheanceDocument", chargeUtile, ct).ConfigureAwait(false);
            await journalisation.ConsignerHistoriqueAsync(
                echeance.TicketId, "RappelEcheanceDocument", $"{echeance.Document} expire le {echeance.DateExpiration:dd/MM/yyyy}", null, ct).ConfigureAwait(false);
            echeance.MarquerRappelEmis(horloge.MaintenantUtc);
        }

        if (aRappeler.Count > 0)
        {
            await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        }

        return aRappeler.Count;
    }
}

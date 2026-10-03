using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Enrolement;

/// <summary>
/// Contrat C13-08 (ENR-05) : Détection proactive et rappel d'expiration des badges transporteurs/chauffeurs.
/// Émet l'événement RappelExpirationBadge vers F009 sans aucune valeur métier en dur.
/// </summary>
public sealed class RappelerExpirationBadgesCommand(
    IDossierRepository dossiers,
    ITicketRepository tickets,
    IJournalisationService journalisation,
    IHorloge horloge,
    IParametresEnrolementProvider parametresProvider,
    IUniteDeTravail uniteDeTravail,
    IBadgeEnrolementRepository? badges = null)
{
    public async Task<int> ExecuterAsync(CancellationToken ct = default)
    {
        var dureeValidite = parametresProvider.ObtenirDureeValiditeBadgeJours();
        var delaiRappel = parametresProvider.ObtenirDelaiRappelExpirationJours();

        if (!dureeValidite.HasValue || !delaiRappel.HasValue)
        {
            throw new InvalidOperationException("La configuration de durée de validité et de délai de rappel des badges est requise.");
        }

        var dureeValiditeJours = dureeValidite.Value;
        var delaiRappelJours = delaiRappel.Value;

        var tousLesDossiers = await dossiers.ListerTousAsync(ct).ConfigureAwait(false);
        var maintenant = horloge.MaintenantUtc;
        var dateSeuilRappel = maintenant.AddDays(delaiRappelJours);
        var nbRappels = 0;

        foreach (var dossier in tousLesDossiers)
        {
            var ticketsDossier = await tickets.ListerParDossierAsync(dossier.Id, ct).ConfigureAwait(false);
            var dernierTicketEnrolement = ticketsDossier
                .Where(t => t.Module == "Enrolement" && t.Statut == StatutTicket.Cloture && t.DateClotureUtc.HasValue)
                .OrderByDescending(t => t.DateClotureUtc)
                .FirstOrDefault();

            if (dernierTicketEnrolement is null) continue;

            // Badge enregistré (depuis le 03/10/2026) : sa vraie date d'expiration. Badges plus
            // anciens, remis sans numéro : durée de validité théorique depuis la clôture.
            var badge = badges is null ? null : await badges.ObtenirParTicketAsync(dernierTicketEnrolement.Id, ct).ConfigureAwait(false);
            var dateExpiration = badge?.DateExpirationUtc
                ?? dernierTicketEnrolement.DateClotureUtc!.Value.AddDays(dureeValiditeJours);

            // Si le badge expire dans la fenêtre de préavis et n'est pas encore totalement expiré depuis plus de 30 jours
            if (dateExpiration <= dateSeuilRappel && dateExpiration >= maintenant.AddDays(-30))
            {
                var payload = $"{{\"dossierId\":\"{dossier.Id}\",\"clientId\":\"{dossier.ClientId}\",\"dateExpirationUtc\":\"{dateExpiration:O}\"}}";
                await journalisation.PublierEvenementAsync("RappelExpirationBadge", payload, ct).ConfigureAwait(false);
                nbRappels++;
            }
        }

        if (nbRappels > 0)
        {
            await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        }

        return nbRappels;
    }
}

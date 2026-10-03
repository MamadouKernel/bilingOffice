using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Enrolement;

/// <summary>
/// Contrat C8-09 — Consulter "Mes enrôlements". CDC : support de ENR-02 (US2).
/// <para>
/// research.md D9/D11 : <c>clientId</c> est toujours dérivé de l'appelant (<c>appelantId</c>),
/// jamais un paramètre libre transmis par le client — anti-IDOR par construction.
/// </para>
/// </summary>
public sealed class ListerMesEnrolementsQuery(
    IDossierRepository dossiers,
    ITicketRepository tickets,
    IUtilisateurSocieteRepository? rattachements = null)
{
    public async Task<IReadOnlyList<Ticket>> ExecuterAsync(Guid appelantId, CancellationToken ct = default)
    {
        var dossiersDuClient = (await dossiers.ListerParClientAsync(appelantId, ct).ConfigureAwait(false)).ToList();

        // Dossiers de FLOTTE (enrôlement société) : ils appartiennent à la société (ClientId =
        // société, AuteurId = représentant). Sans ce complément, une entreprise de transport ne
        // retrouvait aucun de ses enrôlements. Visibilité identique au reste de l'application
        // (ENT-03) : administrateur ou droit « toute la société » => tout ; sinon ses propres dossiers.
        if (rattachements is not null
            && await rattachements.ObtenirParUtilisateurAsync(appelantId, ct).ConfigureAwait(false) is { EstActif: true } rattachement)
        {
            var voitToutLaSociete = rattachement.EstAdministrateur
                || rattachement.DroitConsultation == CIT.Plateforme.Domain.Comptes.DroitConsultation.ToutesDemandesSociete;
            var deLaSociete = await dossiers.ListerParClientAsync(rattachement.SocieteId, ct).ConfigureAwait(false);
            dossiersDuClient.AddRange(deLaSociete.Where(d => voitToutLaSociete || d.AuteurId == appelantId));
        }

        if (dossiersDuClient.Count == 0)
        {
            return [];
        }

        var dossierIds = dossiersDuClient.Select(d => d.Id).Distinct();
        var ticketsDesDossiers = await tickets.ListerParDossiersAsync(dossierIds, ct).ConfigureAwait(false);

        return [.. ticketsDesDossiers.Where(t => t.Module == "Enrolement")];
    }
}

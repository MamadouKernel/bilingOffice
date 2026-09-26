using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Comptes;

namespace CIT.Plateforme.Application.Notifications.Resolveurs;

/// <summary>
/// FEATURE 009 (T050) — résout les destinataires CLIENT d'un événement de ticket (research.md D11).
///
/// Retourne le propriétaire du dossier, PLUS les collaborateurs actifs de sa société dont le
/// <see cref="DroitConsultation"/> couvre les demandes de la société (spec.md §13, ligne
/// « Société cliente »). Le droit existant de F002 est LU, jamais réinterprété ni élargi
/// (moindre privilège, Constitution Principe VII).
///
/// Aucune notification n'est produite pour les types d'événements INTERNES : les notes internes
/// ne franchissent jamais la frontière vers un compte client (MSG-02, spec.md §14 règle 3).
/// </summary>
public sealed class ResolveurClientDuDossier(
    ITicketRepository tickets,
    IDossierRepository dossiers,
    IUtilisateurSocieteRepository utilisateursSociete) : IResolveurDestinataires
{
    /// <summary>
    /// Événements de niveau CDC-EXIGENCE notifiant le client (spec.md §10.2), plus les types
    /// paramétrables du même parcours. La présence d'un type ici n'entraîne AUCUNE notification :
    /// seule une RegleDiffusion active en produit une (spec.md §10).
    ///
    /// « MessagePoste » et « CollaborateurMentionne » sont VOLONTAIREMENT ABSENTS : ils peuvent
    /// porter une note interne, qui ne doit jamais notifier un client (MSG-02).
    /// </summary>
    private static readonly HashSet<string> TypesClient = new(StringComparer.Ordinal)
    {
        "TicketCree",            // SOC-01 / IMP-03 — CDC-EXIGENCE, canaux nommés
        "ComplementDemande",     // IMP-05 — CDC-EXIGENCE
        "FactureEditee",         // IMP-11 — CDC-EXIGENCE
        "TicketTransfere",       // TRF-05 — CDC-EXIGENCE
        "RelanceEmise",          // CYC-04 (mécanisme F001) + COM-06 (canal, Could)
        "TicketIncidentResolu",  // Support IT — le déclarant est informé de la résolution de son incident
    };

    public bool PeutResoudre(string typeEvenement) => TypesClient.Contains(typeEvenement);

    public async Task<IReadOnlyList<Guid>> ResoudreAsync(string typeEvenement, string chargeUtile, CancellationToken ct = default)
    {
        if (!PeutResoudre(typeEvenement))
        {
            return [];
        }

        var ticketId = ResolveurChargeUtile.LireGuid(chargeUtile, "ticketId");
        if (ticketId is null)
        {
            return [];
        }

        var ticket = await tickets.ObtenirParIdAsync(ticketId.Value, ct).ConfigureAwait(false);
        if (ticket is null)
        {
            return [];
        }

        var dossier = await dossiers.ObtenirParIdAsync(ticket.DossierId, ct).ConfigureAwait(false);
        if (dossier is null)
        {
            return [];
        }

        var destinataires = new List<Guid> { dossier.ClientId };

        // Cloisonnement inter-sociétés (CPT-04, SEC-01) : le rattachement du propriétaire du
        // dossier détermine la société, et SEULS ses collaborateurs habilités sont ajoutés.
        var rattachement = await utilisateursSociete.ObtenirParUtilisateurAsync(dossier.ClientId, ct).ConfigureAwait(false);
        if (rattachement is not null)
        {
            var collaborateurs = await utilisateursSociete.ListerParSocieteAsync(rattachement.SocieteId, ct).ConfigureAwait(false);

            destinataires.AddRange(collaborateurs
                .Where(c => c.EstActif)
                // Le droit existant est lu tel quel. Un administrateur de société voit toujours
                // l'ensemble des demandes de sa société (CLAR-02, F002).
                .Where(c => c.EstAdministrateur || c.DroitConsultation == DroitConsultation.ToutesDemandesSociete)
                .Select(c => c.UtilisateurId));
        }

        return destinataires.Distinct().ToList();
    }

    public async Task<Guid?> ResoudreTicketAsync(string chargeUtile, CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        return ResolveurChargeUtile.LireGuid(chargeUtile, "ticketId");
    }
}

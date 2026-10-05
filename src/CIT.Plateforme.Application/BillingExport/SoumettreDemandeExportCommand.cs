using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingExport;

/// <summary>
/// Contrat C5-01 — Soumettre une demande EXPORT multi-bookings, avec contrôle d'unicité par booking (UNI-04, CLAR-BOOK-01).
/// CDC : EXP-01, UNI-04.
/// </summary>
public sealed record PieceJointeExport(string NomFichier, string TypeContenu, byte[] Contenu);

public sealed record SoumettreDemandeExportRequete(
    Guid SocieteId,
    Guid AuteurId,
    IReadOnlyList<string> NumerosBooking,
    string? Marchandises = null,
    string? PortDestination = null,
    IReadOnlyList<PieceJointeExport>? Documents = null,
    string? ReferenceSimulationProforma = null);

/// <summary>
/// UNI-04, CPT-04 : un booking refusé (déjà actif) ne révèle <see cref="TicketExistantId"/> que si le
/// ticket actif appartient à la **même société** que l'appelant (cloisonnement absolu) — sinon message générique.
/// </summary>
public sealed record BookingRefuseInfo(string NumeroBooking, Guid? TicketExistantId);

/// <summary>
/// research.md D4 : chaque booking est traité indépendamment — un dossier est créé ;
/// <see cref="BookingRefuses"/> ne bloque jamais les autres bookings valides de la même soumission.
/// </summary>
public sealed record SoumettreDemandeExportResultat(
    Dossier Dossier,
    IReadOnlyList<Ticket> TicketsCrees,
    IReadOnlyList<BookingRefuseInfo> BookingRefuses);

public sealed class SoumettreDemandeExportCommand(
    ITicketRepository tickets,
    IDossierRepository dossiers,
    ISocieteClienteRepository societes,
    IUtilisateurSocieteRepository utilisateursSociete,
    IFileDeTraitementRepository files,
    IServiceNumerotation numerotation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public const string Module = "BillingExport";
    public const string TypeDemande = "EXPORT";
    public const string FileBackOfficeExport = "Back Office Export";

    public async Task<SoumettreDemandeExportResultat> ExecuterAsync(SoumettreDemandeExportRequete requete, CancellationToken ct = default)
    {
        var maintenant = horloge.MaintenantUtc;

        // ENT-02 (Feature 002) : contrôle du droit de soumission individuel pour la société
        var rattachement = await utilisateursSociete.ObtenirParUtilisateurAsync(requete.AuteurId, ct).ConfigureAwait(false);
        if (rattachement is null || rattachement.SocieteId != requete.SocieteId || !rattachement.EstActif || !rattachement.DroitSoumission)
        {
            throw new AccesRefuseException($"L'utilisateur {requete.AuteurId} n'est pas habilité à soumettre pour la société {requete.SocieteId} (ENT-02).");
        }

        // Snapshot du type de client à la création du dossier (CPT-06)
        var societe = await societes.ObtenirParIdAsync(requete.SocieteId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Société {requete.SocieteId} introuvable.");

        // Une entreprise de transport n'a pas de parcours Billing Office : elle enrôle sa flotte.
        // Refus serveur, quel que soit ce qu'affiche le menu (qui masque déjà ces écrans).
        if (societe.EstTransporteur)
        {
            throw new AccesRefuseException($"La société {requete.SocieteId} est un transporteur : aucune demande Billing Office.");
        }

        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var fileBackOffice = toutesLesFiles.SingleOrDefault(f => f.Module == Module && f.Nom == FileBackOfficeExport)
            ?? throw new InvalidOperationException($"File « {FileBackOfficeExport} » (module {Module}) non paramétrée.");

        var numeroDossier = await numerotation.ProchainNumeroDossierAsync(ct).ConfigureAwait(false);
        var dossier = Dossier.Creer(
            numeroDossier,
            requete.SocieteId,
            maintenant,
            requete.AuteurId,
            societe.Profil,
            requete.ReferenceSimulationProforma);
        await dossiers.AjouterAsync(dossier, ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        var ticketsCrees = new List<Ticket>();
        var bookingRefuses = new List<BookingRefuseInfo>();

        foreach (var numeroBookingBrut in requete.NumerosBooking)
        {
            // CLAR-BOOK-01 : nettoyage des espaces externes uniquement, casse saisie préservée
            var numeroBooking = numeroBookingBrut.Trim();
            if (string.IsNullOrWhiteSpace(numeroBooking))
            {
                continue;
            }

            var actif = await tickets.ObtenirTicketActifParNumeroBookingAsync(numeroBooking, ct).ConfigureAwait(false);
            if (actif is not null)
            {
                bookingRefuses.Add(await CreerRefusAsync(numeroBooking, actif, requete.SocieteId, ct).ConfigureAwait(false));
                continue;
            }

            var numeroTicket = await numerotation.ProchainNumeroTicketAsync(Module, TypeDemande, ct).ConfigureAwait(false);
            var ticket = Ticket.Creer(numeroTicket, Module, TypeDemande, fileBackOffice.Id, dossier.Id, maintenant, numeroBl: null, numeroBooking: numeroBooking);
            await tickets.AjouterAsync(ticket, ct).ConfigureAwait(false);

            try
            {
                await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
            }
            catch (BookingDejaActifException)
            {
                // Concurrence SGBD : l'index unique filtré a bloqué l'insertion
                bookingRefuses.Add(new BookingRefuseInfo(numeroBooking, null));
                continue;
            }

            ticketsCrees.Add(ticket);
            await journalisation.ConsignerHistoriqueAsync(ticket.Id, "TicketExportCree", $"numeroBooking={numeroBooking}", requete.AuteurId, ct).ConfigureAwait(false);
            await journalisation.ConsignerAuditAsync(requete.AuteurId, "TicketExportCree", nameof(Ticket), ticket.Id, null, numeroBooking, ct).ConfigureAwait(false);
            await journalisation.PublierEvenementAsync("TicketCree", $"{{\"ticketId\":\"{ticket.Id}\",\"module\":\"{Module}\"}}", ct).ConfigureAwait(false);
            await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        }

        return new SoumettreDemandeExportResultat(dossier, ticketsCrees, bookingRefuses);
    }

    private async Task<BookingRefuseInfo> CreerRefusAsync(string numeroBooking, Ticket ticketActif, Guid societeAppelant, CancellationToken ct)
    {
        var dossierExistant = await dossiers.ObtenirParIdAsync(ticketActif.DossierId, ct).ConfigureAwait(false);
        var accesAutorise = dossierExistant is not null && dossierExistant.ClientId == societeAppelant;
        return new BookingRefuseInfo(numeroBooking, accesAutorise ? ticketActif.Id : null);
    }
}

using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingImport;

/// <summary>
/// Contrat C3-09/C3-13 — Éditer et joindre la facture, puis orienter selon le type de client. CDC :
/// IMP-11, IMP-20, IMP-21, IMP-30. research.md D6 : la divergence de parcours (Normal → Validation
/// Paiement ; En Compte → Contrôle Documentaire direct ; Deposit → Comptabilité Client) est une
/// simple sélection de file paramétrée, jamais un second moteur de workflow.
/// </summary>
public sealed class EditerFactureCommand(
    ITicketRepository tickets,
    IDossierRepository dossiers,
    IFileDeTraitementRepository files,
    DeposerDocumentCommand deposerDocument,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(
        Guid agentId, Guid ticketId, string nomFichier, string typeContenu, Stream contenu, long tailleOctets,
        ReglesDepotDocument reglesEtape, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await autorisation.ExigerAsync(agentId, Permission.BillingImportFacturationEditer, ticket.FileCouranteId, ct).ConfigureAwait(false);
        await global::CIT.Plateforme.Application.Securite.GardeAgentAffecte.ExigerAsync(autorisation, agentId, ticket, ct).ConfigureAwait(false);
        ticket.ExigerPriseEnChargePar(agentId);

        // Parcours TO BE (05/10/2026) : la facture définitive suit la proforma validée par le client
        // (paramétrable : « Proforma obligatoire » dans les paramètres de la plateforme).
        if (!GestionProformaCommand.FacturePermise(ticket))
        {
            throw new ProformaNonValideeException(ticket.Numero);
        }

        await deposerDocument.DeposerPourAgentAsync(agentId, ticketId, nomFichier, typeContenu, contenu, tailleOctets, reglesEtape, ct).ConfigureAwait(false);

        var dossier = await dossiers.ObtenirParIdAsync(ticket.DossierId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Dossier {ticket.DossierId} introuvable.");
        var typeClient = dossier.TypeClient
            ?? throw new InvalidOperationException($"Dossier {dossier.Id} sans instantané de type client.");

        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var nomFileCible = typeClient switch
        {
            TypeClient.Normal => NomsFiles.ValidationPaiement,
            TypeClient.EnCompte => NomsFiles.ControleDocumentaire, // IMP-21 : transition directe, pas de paiement.
            TypeClient.Deposit => NomsFiles.ComptabiliteClient,
            _ => throw new InvalidOperationException($"EditerFactureCommand ne s'applique pas au type {typeClient} (IES suit un circuit distinct)."),
        };
        var fileCible = toutesLesFiles.SingleOrDefault(f => f.Module == SoumettreDemandeImportCommand.Module && f.Nom == nomFileCible)
            ?? throw new InvalidOperationException($"File « {nomFileCible} » (module {SoumettreDemandeImportCommand.Module}) non paramétrée.");

        var maintenant = horloge.MaintenantUtc;
        // CYC-01 / CYC-05 (remédiation P1-01) : une facture est un document officiel, le ticket devient donc
        // engageant. `MarquerEtatEngageant` n'avait AUCUN appelant en production : le drapeau
        // restait faux à jamais, ce qui privait d'effet la fermeture de l'annulation directe
        // (RG-TCK-07) comme la garde sur la suggestion de clôture (RG-TCK-11).
        ticket.MarquerEtatEngageant();

        ticket.Transferer(fileCible.Id, destinataireId: null, maintenant);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "FactureEditee", nomFichier, agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "FactureEditee", nameof(Ticket), ticket.Id, null, nomFichier, ct).ConfigureAwait(false);

        // FEATURE 009 (T126, research.md D7) : IMP-11 — « édite la facture et la joint au
        // ticket ; LE CLIENT EST NOTIFIÉ ». Sans cet événement, ce Must du CDC serait
        // inatteignable par le moteur de notification (Constitution Principe XVI). Ligne
        // strictement calquée sur TransfererTicketCommand (TRF-05) ; le canal effectif relève du
        // paramétrage de F009, jamais de cette commande.
        await journalisation.PublierEvenementAsync(
            "FactureEditee", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

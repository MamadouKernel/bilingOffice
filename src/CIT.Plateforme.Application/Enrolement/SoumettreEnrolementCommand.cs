using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Enrolement;

/// <summary>
/// Contrat C8-01 — Soumettre un enrôlement. CDC : ENR-01.
/// <para>
/// research.md D1 : le Transporteur/Chauffeur est une personne physique unique (CLAR-ENR-01) —
/// <see cref="Dossier.ClientId"/> = <c>appelantId</c> directement, <see cref="Dossier.AuteurId"/>
/// reste <c>null</c> (aucune société, aucune distinction auteur/propriétaire nécessaire).
/// </para>
/// </summary>
public sealed record SoumettreEnrolementRequete(string Objet, string Description);

public sealed record SoumettreEnrolementResultat(Guid TicketId, string NumeroTicket);

public sealed class SoumettreEnrolementCommand(
    ITicketRepository tickets,
    IDossierRepository dossiers,
    IFileDeTraitementRepository files,
    IServiceNumerotation numerotation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IAutorisationService autorisation,
    IUniteDeTravail uniteDeTravail)
{
    public const string Module = "Enrolement";
    public const string TypeDemande = "Enrolement";
    public const string FileAgentGate = "Agent Gate";

    public async Task<SoumettreEnrolementResultat> ExecuterAsync(Guid appelantId, SoumettreEnrolementRequete requete, CancellationToken ct = default)
    {
        // A/C. Autorisation globale — vérifiée côté serveur avant toute écriture.
        var aLeDroit = await autorisation.EstAutoriseAsync(appelantId, Permission.EnrolementSoumettre, null, ct).ConfigureAwait(false);
        if (!aLeDroit)
        {
            throw new AccesRefuseException($"L'utilisateur {appelantId} ne possède pas la permission de soumettre un enrôlement.");
        }

        // D. Validation du contenu — défense en profondeur avant toute persistance (patron REC-REM-02/F006).
        if (string.IsNullOrWhiteSpace(requete.Objet))
        {
            throw new ArgumentException("L'objet de l'enrôlement est obligatoire (ENR-01).", nameof(requete));
        }

        if (string.IsNullOrWhiteSpace(requete.Description))
        {
            throw new ArgumentException("La description de l'enrôlement est obligatoire (ENR-01).", nameof(requete));
        }

        // F. Résolution de la file "Agent Gate" — donnée paramétrée, jamais un identifiant codé en dur.
        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var fileAgentGate = toutesLesFiles.SingleOrDefault(f => f.Module == Module && f.Nom == FileAgentGate)
            ?? throw new InvalidOperationException($"File « {FileAgentGate} » (module {Module}) non paramétrée.");

        var maintenant = horloge.MaintenantUtc;

        // B. Dossier propriétaire = appelant lui-même (personne physique unique, D1) — aucune
        // donnée d'identité transmise par le client n'est utilisée ici.
        var numeroDossier = await numerotation.ProchainNumeroDossierAsync(ct).ConfigureAwait(false);
        var dossier = Dossier.Creer(numeroDossier, appelantId, maintenant);
        await dossiers.AjouterAsync(dossier, ct).ConfigureAwait(false);

        var numeroTicket = await numerotation.ProchainNumeroTicketAsync(Module, TypeDemande, ct).ConfigureAwait(false);
        var ticket = Ticket.Creer(numeroTicket, Module, TypeDemande, fileAgentGate.Id, dossier.Id, maintenant);
        ticket.DefinirInformationsEnrolement(requete.Objet, requete.Description);
        await tickets.AjouterAsync(ticket, ct).ConfigureAwait(false);

        // G. Audit.
        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "TicketCree", "Enrôlement soumis", appelantId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(appelantId, "TicketCree", nameof(Ticket), ticket.Id, null, ticket.Numero, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("TicketCree", $"{{\"ticketId\":\"{ticket.Id}\",\"module\":\"{Module}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return new SoumettreEnrolementResultat(ticket.Id, ticket.Numero);
    }
}

using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Documents;

/// <summary>Levée quand le fichier déposé ne respecte pas les règles de l'étape (SOC-07).</summary>
public sealed class DepotDocumentRefuseException(string message) : Exception(message);

/// <summary>
/// Contrat C-11 — Déposer un document. CDC : SOC-07, SEC-07.
/// <para>
/// REMÉDIATION AUDIT (post-implementation-audit.md §6) : avant correction, cette commande ne
/// vérifiait aucune habilitation ni cloisonnement — n'importe quel <c>deposantId</c> pouvait
/// déposer un document sur n'importe quel ticket. Deux points d'entrée distincts sont désormais
/// exposés, à l'image de <see cref="TelechargerDocumentQuery"/> (même asymétrie résolue) : un
/// agent doit être habilité (<see cref="Permission.DocumentDeposer"/>) sur la file courante du
/// ticket ; un client doit en être propriétaire (<see cref="CloisonnementService"/>).
/// </para>
/// </summary>
public sealed class DeposerDocumentCommand(
    IDocumentRepository documents,
    ITicketRepository tickets,
    IStockageDocuments stockage,
    IAnalyseAntivirus antivirus,
    IAutorisationService autorisation,
    CloisonnementService cloisonnement,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    /// <summary>Dépôt par un agent habilité sur la file courante du ticket.</summary>
    public async Task<Document> DeposerPourAgentAsync(
        Guid agentId, Guid ticketId, string nomFichier, string typeContenu, Stream contenu, long tailleOctets,
        ReglesDepotDocument reglesEtape, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await autorisation.ExigerAsync(agentId, Permission.DocumentDeposer, ticket.FileCouranteId, ct).ConfigureAwait(false);

        return await DeposerValideAsync(agentId, ticketId, nomFichier, typeContenu, contenu, tailleOctets, reglesEtape, ct).ConfigureAwait(false);
    }

    /// <summary>Dépôt par le client propriétaire du ticket.</summary>
    public async Task<Document> DeposerPourClientAsync(
        Guid clientId, Guid ticketId, string nomFichier, string typeContenu, Stream contenu, long tailleOctets,
        ReglesDepotDocument reglesEtape, CancellationToken ct = default)
    {
        await cloisonnement.ExigerProprieteClientAsync(clientId, ticketId, ct).ConfigureAwait(false);

        return await DeposerValideAsync(clientId, ticketId, nomFichier, typeContenu, contenu, tailleOctets, reglesEtape, ct).ConfigureAwait(false);
    }

    private async Task<Document> DeposerValideAsync(
        Guid deposantId, Guid ticketId, string nomFichier, string typeContenu, Stream contenu, long tailleOctets,
        ReglesDepotDocument reglesEtape, CancellationToken ct)
    {
        // SOC-07 : contrôle à l'upload, message d'erreur explicite en cas de non-conformité.
        if (!reglesEtape.Valide(nomFichier, tailleOctets, out var erreur))
        {
            throw new DepotDocumentRefuseException(erreur!);
        }

        var cle = await stockage.EnregistrerAsync(contenu, nomFichier, ct).ConfigureAwait(false);
        var document = Document.Deposer(ticketId, deposantId, nomFichier, typeContenu, tailleOctets, cle, horloge.MaintenantUtc);

        // SEC-07 : analyse antivirale avant que le document soit accessible.
        contenu.Position = 0;
        var resultat = await antivirus.AnalyserAsync(contenu, ct).ConfigureAwait(false);
        document.MarquerResultatAntivirus(resultat);

        await documents.AjouterAsync(document, ct).ConfigureAwait(false);

        await journalisation.ConsignerHistoriqueAsync(ticketId, "DocumentDepose", nomFichier, deposantId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(deposantId, "DocumentDepose", nameof(Document), document.Id, null, nomFichier, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        return document;
    }
}

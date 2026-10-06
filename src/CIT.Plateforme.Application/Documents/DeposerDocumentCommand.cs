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
        => await DeposerPourAgentAsync(agentId, ticketId, nomFichier, typeContenu, contenu, tailleOctets, reglesEtape, null, ct).ConfigureAwait(false);

    /// <summary>Dépôt par un agent habilité sur la file courante du ticket, avec catégorie
    /// (IMP-01, parcours Billing Import).</summary>
    public async Task<Document> DeposerPourAgentAsync(
        Guid agentId, Guid ticketId, string nomFichier, string typeContenu, Stream contenu, long tailleOctets,
        ReglesDepotDocument reglesEtape, CategorieDocumentBillingImport? categorie, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await autorisation.ExigerAsync(agentId, Permission.DocumentDeposer, ticket.FileCouranteId, ct).ConfigureAwait(false);

        return await DeposerValideAsync(agentId, ticketId, nomFichier, typeContenu, contenu, tailleOctets, reglesEtape, categorie, ct).ConfigureAwait(false);
    }

    /// <summary>Dépôt par le client propriétaire du ticket.</summary>
    public async Task<Document> DeposerPourClientAsync(
        Guid clientId, Guid ticketId, string nomFichier, string typeContenu, Stream contenu, long tailleOctets,
        ReglesDepotDocument reglesEtape, CancellationToken ct = default)
        => await DeposerPourClientAsync(clientId, ticketId, nomFichier, typeContenu, contenu, tailleOctets, reglesEtape, null, ct).ConfigureAwait(false);

    /// <summary>Dépôt par le client propriétaire du ticket, avec catégorie (IMP-01, parcours
    /// Billing Import : « déclaration douane, copie du BL, autres pièces exigées »).</summary>
    public async Task<Document> DeposerPourClientAsync(
        Guid clientId, Guid ticketId, string nomFichier, string typeContenu, Stream contenu, long tailleOctets,
        ReglesDepotDocument reglesEtape, CategorieDocumentBillingImport? categorie, CancellationToken ct = default)
    {
        await cloisonnement.ExigerProprieteClientAsync(clientId, ticketId, ct).ConfigureAwait(false);

        return await DeposerValideAsync(clientId, ticketId, nomFichier, typeContenu, contenu, tailleOctets, reglesEtape, categorie, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Écart 06/10 #1 — « document commun à plusieurs BL/bookings » : dépôt d'une pièce unique
    /// rattachée à plusieurs tickets de la demande du client. Le contenu est lu, stocké et analysé
    /// UNE SEULE fois (pas de dépôt multiple) ; un <see cref="Document"/> est créé par ticket,
    /// partageant la même clé de stockage et le même résultat antivirus. Le cloisonnement est
    /// exigé sur CHAQUE ticket AVANT tout upload : si un seul ticket n'appartient pas au client,
    /// rien n'est stocké.
    /// </summary>
    public async Task<IReadOnlyList<Document>> DeposerDocumentCommunPourClientAsync(
        Guid clientId, IReadOnlyList<Guid> ticketIds, string nomFichier, string typeContenu, Stream contenu, long tailleOctets,
        ReglesDepotDocument reglesEtape, CategorieDocumentBillingImport? categorie = null, CancellationToken ct = default)
    {
        if (ticketIds is null || ticketIds.Count == 0)
        {
            throw new ArgumentException("Au moins un ticket doit être sélectionné pour un document commun.", nameof(ticketIds));
        }

        // Cloisonnement d'abord, sur tous les tickets : aucun octet n'est stocké si l'un d'eux
        // n'appartient pas au client (sinon un document d'autrui porterait la même clé).
        foreach (var ticketId in ticketIds)
        {
            await cloisonnement.ExigerProprieteClientAsync(clientId, ticketId, ct).ConfigureAwait(false);
        }

        if (!reglesEtape.Valide(nomFichier, tailleOctets, out var erreur))
        {
            throw new DepotDocumentRefuseException(erreur!);
        }

        // Un seul upload physique + une seule analyse antivirus, quel que soit le nombre de tickets.
        await using var rejouable = await FluxRejouable
            .CreerAsync(contenu, reglesEtape.TailleMaxOctets, ct).ConfigureAwait(false);
        var cle = await stockage.EnregistrerAsync(rejouable.Flux, nomFichier, ct).ConfigureAwait(false);
        rejouable.Rembobiner();
        var resultat = await antivirus.AnalyserAsync(rejouable.Flux, ct).ConfigureAwait(false);

        var maintenant = horloge.MaintenantUtc;
        var crees = new List<Document>(ticketIds.Count);
        foreach (var ticketId in ticketIds)
        {
            var document = Document.Deposer(ticketId, clientId, nomFichier, typeContenu, tailleOctets, cle, maintenant, categorie);
            document.MarquerResultatAntivirus(resultat);
            await documents.AjouterAsync(document, ct).ConfigureAwait(false);
            await journalisation.ConsignerHistoriqueAsync(ticketId, "DocumentDepose", nomFichier, clientId, ct).ConfigureAwait(false);
            await journalisation.ConsignerAuditAsync(clientId, "DocumentDepose", nameof(Document), document.Id, null, nomFichier, ct).ConfigureAwait(false);
            crees.Add(document);
        }

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        return crees;
    }

    private async Task<Document> DeposerValideAsync(
        Guid deposantId, Guid ticketId, string nomFichier, string typeContenu, Stream contenu, long tailleOctets,
        ReglesDepotDocument reglesEtape, CategorieDocumentBillingImport? categorie, CancellationToken ct)
    {
        // SOC-07 : contrôle à l'upload, message d'erreur explicite en cas de non-conformité.
        if (!reglesEtape.Valide(nomFichier, tailleOctets, out var erreur))
        {
            throw new DepotDocumentRefuseException(erreur!);
        }

        // REMÉDIATION RECETTE UAT (D-11a) : le contenu est lu DEUX fois — stockage puis antivirus.
        // Le flux d'un InputFile Blazor est à passe unique ; le repositionner y lève
        // NotSupportedException. FluxRejouable ne recopie que si la source ne sait pas rembobiner,
        // et plafonne cette recopie par la taille maximale de l'étape.
        await using var rejouable = await FluxRejouable
            .CreerAsync(contenu, reglesEtape.TailleMaxOctets, ct).ConfigureAwait(false);

        var cle = await stockage.EnregistrerAsync(rejouable.Flux, nomFichier, ct).ConfigureAwait(false);
        var document = Document.Deposer(ticketId, deposantId, nomFichier, typeContenu, tailleOctets, cle, horloge.MaintenantUtc, categorie);

        // SEC-07 : analyse antivirale avant que le document soit accessible.
        rejouable.Rembobiner();
        var resultat = await antivirus.AnalyserAsync(rejouable.Flux, ct).ConfigureAwait(false);
        document.MarquerResultatAntivirus(resultat);

        await documents.AjouterAsync(document, ct).ConfigureAwait(false);

        await journalisation.ConsignerHistoriqueAsync(ticketId, "DocumentDepose", nomFichier, deposantId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(deposantId, "DocumentDepose", nameof(Document), document.Id, null, nomFichier, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        return document;
    }
}

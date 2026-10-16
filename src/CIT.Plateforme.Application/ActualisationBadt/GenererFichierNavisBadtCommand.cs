using System.Text;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.ActualisationBadt;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.ActualisationBadt;

/// <summary>Fichier Navis prêt à intégrer : nom proposé et octets (encodage Latin1 préservé).</summary>
public sealed record ResultatFichierNavisBadt(string NomFichier, byte[] Contenu);

/// <summary>
/// FEATURE 010 (Actualisation BADT) — génère le fichier d'actualisation Navis à partir de l'extraction
/// N4 déposée par l'agent : la plateforme injecte automatiquement la colonne T (nouvelle date de
/// validité) des conteneurs ACCORDÉS, puis renvoie le fichier à intégrer dans Navis.
/// </summary>
public sealed class GenererFichierNavisBadtCommand(
    ILigneActualisationBadtRepository lignes,
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<ResultatFichierNavisBadt> ExecuterAsync(Guid agentId, Guid ticketId, byte[] extractionNavis, CancellationToken ct = default)
    {
        if (extractionNavis is null || extractionNavis.Length == 0)
        {
            throw new ArgumentException("L'extraction Navis est vide.", nameof(extractionNavis));
        }

        var ticket = await QualifierLigneBadtCommand.ChargerTicketBadtAsync(tickets, ticketId, ct).ConfigureAwait(false);

        if (!await autorisation.EstAutoriseAsync(agentId, Permission.ActualisationBadtInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false))
        {
            throw new AccesRefuseException("Permission d'instruction des demandes d'actualisation BADT requise.");
        }

        var toutesLignes = await lignes.ListerParTicketAsync(ticket.Id, ct).ConfigureAwait(false);
        var datesParConteneur = toutesLignes
            .Where(l => l.Decision == DecisionLigneBadt.Accordee && l.NouvelleDateValidite is not null)
            .ToDictionary(l => l.NumeroConteneur, l => l.NouvelleDateValidite!.Value);

        if (datesParConteneur.Count == 0)
        {
            throw new InvalidOperationException("Aucun conteneur accordé : il n'y a rien à actualiser.");
        }

        // Latin1 : chaque octet 0-255 correspond à un code point identique, donc le round-trip préserve
        // à l'octet près tout le contenu d'origine ; seules les dates (ASCII) de la colonne T changent.
        var contenu = Encoding.Latin1.GetString(extractionNavis);
        var genere = GenerateurFichierNavisBadt.Injecter(contenu, datesParConteneur);
        var octets = Encoding.Latin1.GetBytes(genere);

        var nomFichier = $"actualisation-badt-{ticket.Numero}-{horloge.MaintenantUtc:yyyyMMdd-HHmm}.csv";

        await journalisation.ConsignerAuditAsync(agentId, "ActualisationBadt.GenerationFichierNavis", "Ticket",
            ticket.Id, null, $"conteneurs={datesParConteneur.Count}", ct).ConfigureAwait(false);
        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "FichierNavisGenere",
            $"conteneurs={datesParConteneur.Count}", agentId, ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return new ResultatFichierNavisBadt(nomFichier, octets);
    }
}

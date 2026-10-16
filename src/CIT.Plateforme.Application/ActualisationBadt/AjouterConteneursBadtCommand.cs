using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.ActualisationBadt;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.ActualisationBadt;

/// <summary>
/// FEATURE 010 (Actualisation BADT) — écart 2 : DÉPLIAGE d'un BL par l'agent (Support Client). Le
/// client ne saisit que le(s) BL ; l'agent ajoute manuellement les conteneurs sur la fiche (N4 n'est
/// pas interrogeable en direct, donc aucune lecture Navis). Le premier conteneur consomme la ligne
/// BL-seul encore vide ; les suivants créent de nouvelles lignes.
/// </summary>
public sealed class AjouterConteneursBadtCommand(
    ILigneActualisationBadtRepository lignes,
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IMesureActualisationBadtRepository mesures,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid agentId, Guid ticketId, string numeroBl, IReadOnlyList<string> conteneurs, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(numeroBl))
        {
            throw new ArgumentException("Le numéro de BL est obligatoire.", nameof(numeroBl));
        }

        var aAjouter = (conteneurs ?? [])
            .Select(c => c?.Trim() ?? string.Empty)
            .Where(c => c.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (aAjouter.Count == 0)
        {
            throw new ArgumentException("Au moins un numéro de conteneur est requis.", nameof(conteneurs));
        }

        var ticket = await QualifierLigneBadtCommand.ChargerTicketBadtAsync(tickets, ticketId, ct).ConfigureAwait(false);

        if (!await autorisation.EstAutoriseAsync(agentId, Permission.ActualisationBadtInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false))
        {
            throw new AccesRefuseException("Permission de qualification des demandes d'actualisation BADT requise.");
        }

        var blNormalise = numeroBl.Trim();
        var existantes = await lignes.ListerParTicketAsync(ticketId, ct).ConfigureAwait(false);

        // Conteneurs déjà présents sur ce BL : on n'ajoute pas de doublon.
        var dejaPresents = existantes
            .Where(l => string.Equals(l.NumeroBl, blNormalise, StringComparison.OrdinalIgnoreCase) && l.NumeroConteneur.Length > 0)
            .Select(l => l.NumeroConteneur)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Lignes BL-seul encore vides, à combler en priorité.
        var placeholders = new Queue<LigneActualisationBadt>(existantes
            .Where(l => string.Equals(l.NumeroBl, blNormalise, StringComparison.OrdinalIgnoreCase)
                        && l.NumeroConteneur.Length == 0
                        && l.Decision == DecisionLigneBadt.EnAttente));

        var maintenant = horloge.MaintenantUtc;
        foreach (var conteneur in aAjouter.Where(c => !dejaPresents.Contains(c)))
        {
            if (placeholders.Count > 0)
            {
                placeholders.Dequeue().RenseignerConteneur(conteneur);
            }
            else
            {
                await lignes.AjouterAsync(LigneActualisationBadt.Creer(ticketId, blNormalise, conteneur), ct).ConfigureAwait(false);
            }

            // Reporting (approche B) : trace du dépliage — qui a ajouté quel conteneur, quand.
            await mesures.AjouterAsync(MesureActualisationBadt.Creer(
                ticketId, null, blNormalise, conteneur, TypeMesureBadt.Depliage, agentId, maintenant), ct).ConfigureAwait(false);
        }

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "ConteneursAjoutes",
            $"bl={blNormalise};conteneurs={string.Join(",", aAjouter)}", agentId, ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

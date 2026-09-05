using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Administration;

/// <summary>Un seuil de fermeture de l'annulation directe, tel que présenté à l'administrateur.</summary>
public sealed record EtapeBloquanteAnnulationVue(
    Guid Id, string Module, string? TypeDemande, Guid FileId, string FileNom, string Motif, bool EstActif);

/// <summary>
/// CYC-01 / RG-TCK-07 — « l'annulation directe est fermée selon un <b>seuil paramétrable par
/// workflow</b> ».
///
/// REMÉDIATION P1-01. Ce paramétrage n'existait pas : la fermeture reposait sur un booléen que
/// l'appelant devait calculer, et que le seul appelant fixait à <c>false</c>. Déclarer une étape
/// bloquante devient ici une donnée d'administration, jamais un développement.
///
/// Aucun seuil n'est livré par défaut : le CDC ne nomme aucune étape bloquante précise, et en
/// inventer une reviendrait à décider à la place du métier. Tant que le référentiel est vide,
/// seule la survenue d'un état engageant (paiement confirmé, document officiel produit) ferme
/// l'annulation — même convention que les motifs de transfert.
/// </summary>
public sealed class GererEtapesBloquantesAnnulationCommand(
    IEtapeBloquanteAnnulationRepository etapes,
    IFileDeTraitementRepository files,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<IReadOnlyList<EtapeBloquanteAnnulationVue>> ListerAsync(
        Guid appelantId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        var toutes = await etapes.ListerToutesAsync(ct).ConfigureAwait(false);
        var lesFiles = (await files.ListerAsync(ct).ConfigureAwait(false)).ToDictionary(f => f.Id, f => f.Nom);

        return [.. toutes.Select(e => new EtapeBloquanteAnnulationVue(
            e.Id, e.Module, e.TypeDemande, e.FileId,
            lesFiles.TryGetValue(e.FileId, out var nom) ? nom : "File inconnue",
            e.Motif, e.EstActif))];
    }

    public async Task<Guid> DefinirAsync(
        Guid appelantId, string module, string? typeDemande, Guid fileId, string motif, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        var existantes = await etapes.ListerToutesAsync(ct).ConfigureAwait(false);
        var normalise = string.IsNullOrWhiteSpace(typeDemande) ? null : typeDemande.Trim();

        if (existantes.Any(e => string.Equals(e.Module, module.Trim(), StringComparison.OrdinalIgnoreCase)
                             && string.Equals(e.TypeDemande, normalise, StringComparison.OrdinalIgnoreCase)
                             && e.FileId == fileId))
        {
            throw new InvalidOperationException(
                "Cette file est déjà déclarée bloquante pour ce workflow.");
        }

        var etape = EtapeBloquanteAnnulation.Definir(module, typeDemande, fileId, motif);
        await etapes.AjouterAsync(etape, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            appelantId, "EtapeBloquanteAnnulationDefinie", nameof(EtapeBloquanteAnnulation), etape.Id,
            null, $"{etape.Module}/{etape.TypeDemande ?? "*"}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        return etape.Id;
    }

    public async Task BasculerActivationAsync(Guid appelantId, Guid etapeId, bool actif, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        var etape = await etapes.ObtenirParIdAsync(etapeId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Étape bloquante {etapeId} introuvable.");

        var avant = etape.EstActif;
        if (actif)
        {
            etape.Activer();
        }
        else
        {
            etape.Desactiver();
        }

        await journalisation.ConsignerAuditAsync(
            appelantId, "EtapeBloquanteAnnulationBasculee", nameof(EtapeBloquanteAnnulation), etape.Id,
            avant.ToString(), etape.EstActif.ToString(), ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

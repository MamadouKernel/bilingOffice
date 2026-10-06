using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Administration;

/// <summary>Un niveau de priorité tel que présenté à l'administrateur fonctionnel.</summary>
public sealed record NiveauPrioriteVue(Guid Id, string Libelle, int Rang);

/// <summary>
/// CDC § 17.3 / PRI-01 — le référentiel des niveaux de priorité est administrable sans
/// développement. Aucun niveau n'est livré par défaut (Principe VI) : le CDC n'en nomme aucun et
/// en inventer reviendrait à décider à la place du métier. Même convention que les motifs de
/// transfert ou les pièces exigées.
/// </summary>
public sealed class GererNiveauxPrioriteCommand(
    IPrioriteRepository priorites,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<Guid> DefinirAsync(
        Guid appelantId, string libelle, int rang, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(libelle))
        {
            throw new ArgumentException("Le libellé du niveau de priorité est obligatoire.", nameof(libelle));
        }

        var niveau = NiveauPriorite.Definir(libelle.Trim(), rang);
        await priorites.AjouterNiveauAsync(niveau, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            appelantId, "NiveauPrioriteDefini", nameof(NiveauPriorite), niveau.Id, null, niveau.Libelle, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        return niveau.Id;
    }

    public async Task ModifierAsync(
        Guid appelantId, Guid niveauId, string libelle, int rang, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(libelle))
        {
            throw new ArgumentException("Le libellé du niveau de priorité est obligatoire.", nameof(libelle));
        }

        var niveau = await priorites.ObtenirNiveauAsync(niveauId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Niveau de priorité {niveauId} introuvable.");

        var avant = $"{niveau.Libelle} (rang {niveau.Rang})";
        niveau.Modifier(libelle.Trim(), rang);

        await journalisation.ConsignerAuditAsync(
            appelantId, "NiveauPrioriteModifie", nameof(NiveauPriorite), niveau.Id, avant, $"{niveau.Libelle} (rang {niveau.Rang})", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>CDC § 17.3 — lecture du référentiel des niveaux de priorité.</summary>
public sealed class ListerNiveauxPrioriteQuery(
    IPrioriteRepository priorites,
    IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<NiveauPrioriteVue>> ExecuterAsync(Guid administrateurId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        var niveaux = await priorites.ListerNiveauxAsync(ct).ConfigureAwait(false);

        return niveaux
            .OrderBy(n => n.Rang)
            .ThenBy(n => n.Libelle, StringComparer.CurrentCulture)
            .Select(n => new NiveauPrioriteVue(n.Id, n.Libelle, n.Rang))
            .ToList();
    }
}

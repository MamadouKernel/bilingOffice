using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.SuiviTemps;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.SuiviTemps;

namespace CIT.Plateforme.Application.Administration;

/// <summary>Un jour férié tel que présenté à l'administrateur.</summary>
public sealed record JourFerieVue(Guid Id, DateOnly Date, string Libelle, bool EstActif);

/// <summary>
/// SLA-04 — référentiel des jours chômés exclus du calcul des délais SLA, administrable sans
/// développement. L'habilitation exigée est celle du SLA (<see cref="Permission.AdministrationSla"/>) :
/// le calendrier ouvré et ses fériés relèvent du même paramétrage. Toute modification rafraîchit le
/// cache <see cref="JoursFeriesCourants"/> utilisé par le calcul.
/// </summary>
public sealed class GererJoursFeriesCommand(
    IJourFerieRepository joursFeries,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<Guid> DefinirAsync(Guid appelantId, DateOnly date, string libelle, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationSla, ct: ct).ConfigureAwait(false);

        if (await joursFeries.DateExisteAsync(date, ct).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Un jour férié au {date:dd/MM/yyyy} existe déjà.");
        }

        var jour = JourFerie.Definir(date, libelle);
        await joursFeries.AjouterAsync(jour, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            appelantId, "JourFerieDefini", nameof(JourFerie), jour.Id, null, $"{date:yyyy-MM-dd} : {jour.Libelle}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        await RafraichirCacheAsync(ct).ConfigureAwait(false);
        return jour.Id;
    }

    public async Task ModifierAsync(Guid appelantId, Guid jourId, string libelle, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationSla, ct: ct).ConfigureAwait(false);

        var jour = await joursFeries.ObtenirParIdAsync(jourId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Jour férié {jourId} introuvable.");

        var avant = jour.Libelle;
        jour.Modifier(libelle);

        await journalisation.ConsignerAuditAsync(
            appelantId, "JourFerieModifie", nameof(JourFerie), jour.Id, avant, jour.Libelle, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Désactiver plutôt que supprimer : un SLA déjà calculé a pu exclure cette date.</summary>
    public async Task DefinirActivationAsync(Guid appelantId, Guid jourId, bool actif, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationSla, ct: ct).ConfigureAwait(false);

        var jour = await joursFeries.ObtenirParIdAsync(jourId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Jour férié {jourId} introuvable.");

        var avant = jour.EstActif ? "actif" : "inactif";
        if (actif)
        {
            jour.Activer();
        }
        else
        {
            jour.Desactiver();
        }

        await journalisation.ConsignerAuditAsync(
            appelantId, actif ? "JourFerieActive" : "JourFerieDesactive", nameof(JourFerie), jour.Id,
            avant, actif ? "actif" : "inactif", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        await RafraichirCacheAsync(ct).ConfigureAwait(false);
    }

    private async Task RafraichirCacheAsync(CancellationToken ct)
    {
        var actifs = await joursFeries.ListerAsync(activesUniquement: true, ct).ConfigureAwait(false);
        JoursFeriesCourants.Remplacer(actifs.Select(j => j.Date));
    }
}

/// <summary>SLA-04 — lecture du référentiel des jours fériés pour l'écran d'administration.</summary>
public sealed class ListerJoursFeriesQuery(
    IJourFerieRepository joursFeries,
    IAutorisationService autorisation)
{
    /// <summary>Écran de paramétrage : voit AUSSI les jours désactivés, sans quoi ils seraient
    /// irrécupérables.</summary>
    public async Task<IReadOnlyList<JourFerieVue>> ExecuterAsync(Guid administrateurId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationSla, ct: ct).ConfigureAwait(false);

        var tous = await joursFeries.ListerAsync(activesUniquement: false, ct).ConfigureAwait(false);
        return tous
            .OrderBy(j => j.Date)
            .Select(j => new JourFerieVue(j.Id, j.Date, j.Libelle, j.EstActif))
            .ToList();
    }
}

/// <summary>SLA-04 — charge au démarrage les jours fériés actifs dans le cache lu par le calcul SLA.</summary>
public sealed class ChargerJoursFeries(IJourFerieRepository joursFeries)
{
    public async Task ExecuterAsync(CancellationToken ct = default)
    {
        var actifs = await joursFeries.ListerAsync(activesUniquement: true, ct).ConfigureAwait(false);
        JoursFeriesCourants.Remplacer(actifs.Select(j => j.Date));
    }
}

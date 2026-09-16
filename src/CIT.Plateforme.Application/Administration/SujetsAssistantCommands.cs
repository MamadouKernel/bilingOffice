using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Assistant;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Administration;

/// <summary>Un sujet de l'assistant tel que présenté à l'administrateur.</summary>
public sealed record SujetAssistantVue(Guid Id, string MotsCles, string Reponse, bool EstActif, int Ordre);

/// <summary>
/// IA-04 — administration de la base de connaissances de l'assistant simulé (§16, §17.3). Ajouter
/// un sujet ou corriger une réponse ne demande plus de recompilation ni de redéploiement.
/// </summary>
public sealed class GererSujetsAssistantCommand(
    ISujetAssistantRepository sujets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<Guid> DefinirAsync(
        Guid appelantId, string motsCles, string reponse, int ordre, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationAssistant, ct: ct).ConfigureAwait(false);

        var sujet = SujetAssistant.Definir(motsCles, reponse, ordre);
        await sujets.AjouterAsync(sujet, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            appelantId, "SujetAssistantDefini", nameof(SujetAssistant), sujet.Id, null, sujet.MotsCles, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        return sujet.Id;
    }

    public async Task ModifierAsync(
        Guid appelantId, Guid sujetId, string motsCles, string reponse, int ordre, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationAssistant, ct: ct).ConfigureAwait(false);

        var sujet = await sujets.ObtenirParIdAsync(sujetId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Sujet {sujetId} introuvable.");

        var avant = sujet.MotsCles;
        sujet.Modifier(motsCles, reponse, ordre);

        await journalisation.ConsignerAuditAsync(
            appelantId, "SujetAssistantModifie", nameof(SujetAssistant), sujet.Id, avant, sujet.MotsCles, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    public async Task DefinirActivationAsync(Guid appelantId, Guid sujetId, bool actif, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationAssistant, ct: ct).ConfigureAwait(false);

        var sujet = await sujets.ObtenirParIdAsync(sujetId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Sujet {sujetId} introuvable.");

        var avant = sujet.EstActif ? "actif" : "inactif";
        if (actif)
        {
            sujet.Activer();
        }
        else
        {
            sujet.Desactiver();
        }

        await journalisation.ConsignerAuditAsync(
            appelantId, actif ? "SujetAssistantActive" : "SujetAssistantDesactive", nameof(SujetAssistant), sujet.Id,
            avant, actif ? "actif" : "inactif", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>§17.3 — lecture du référentiel des sujets de l'assistant, écran de paramétrage
/// (voit aussi les sujets désactivés).</summary>
public sealed class ListerSujetsAssistantQuery(
    ISujetAssistantRepository sujets,
    IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<SujetAssistantVue>> ExecuterAsync(Guid administrateurId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationAssistant, ct: ct).ConfigureAwait(false);

        var tous = await sujets.ListerAsync(activesUniquement: false, ct).ConfigureAwait(false);

        return tous
            .OrderBy(s => s.Ordre)
            .Select(s => new SujetAssistantVue(s.Id, s.MotsCles, s.Reponse, s.EstActif, s.Ordre))
            .ToList();
    }
}

using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Enrolement;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Administration;

/// <summary>Un champ du formulaire d'enrôlement tel que présenté à l'administrateur et au chauffeur/transporteur.
/// <para>LOT 4 : <see cref="TypeObjet"/> nul = champ applicable à tous les types ; sinon restreint à un type.</para></summary>
public sealed record ChampFormulaireEnrolementVue(Guid Id, string Code, string Libelle, bool EstObligatoire, bool EstActif, int Ordre, TypeObjetEnrolement? TypeObjet);

/// <summary>
/// ENR-01 — « la liste des informations et pièces est paramétrable » : les champs structurés du
/// formulaire d'enrôlement sont administrables sans développement.
///
/// Aucun champ n'est livré par défaut : le CDC n'en nomme aucun, et en inventer reviendrait à
/// décider à la place du métier. Tant que le référentiel est vide, la soumission reste celle
/// d'aujourd'hui (objet/description libres) — même convention que les motifs de transfert.
/// </summary>
public sealed class GererChampsFormulaireEnrolementCommand(
    IChampFormulaireEnrolementRepository champs,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<Guid> DefinirAsync(
        Guid appelantId, string code, string libelle, bool estObligatoire, int ordre, TypeObjetEnrolement? typeObjet = null, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationChampsEnrolement, ct: ct).ConfigureAwait(false);

        if (await champs.CodeExisteAsync(code.Trim(), ct).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Un champ portant le code « {code.Trim()} » existe déjà.");
        }

        var champ = ChampFormulaireEnrolement.Definir(code, libelle, estObligatoire, ordre, typeObjet);
        await champs.AjouterAsync(champ, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            appelantId, "ChampFormulaireEnrolementDefini", nameof(ChampFormulaireEnrolement), champ.Id, null, champ.Code, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        return champ.Id;
    }

    public async Task ModifierAsync(
        Guid appelantId, Guid champId, string libelle, bool estObligatoire, int ordre, TypeObjetEnrolement? typeObjet = null, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationChampsEnrolement, ct: ct).ConfigureAwait(false);

        var champ = await champs.ObtenirParIdAsync(champId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Champ {champId} introuvable.");

        var avant = champ.Libelle;
        champ.Modifier(libelle, estObligatoire, ordre, typeObjet);

        await journalisation.ConsignerAuditAsync(
            appelantId, "ChampFormulaireEnrolementModifie", nameof(ChampFormulaireEnrolement), champ.Id,
            avant, champ.Libelle, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Désactiver plutôt que supprimer : des soumissions déjà tracées peuvent référencer ce
    /// code, et le journal d'audit est append-only (SEC-05).</summary>
    public async Task DefinirActivationAsync(Guid appelantId, Guid champId, bool actif, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationChampsEnrolement, ct: ct).ConfigureAwait(false);

        var champ = await champs.ObtenirParIdAsync(champId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Champ {champId} introuvable.");

        var avant = champ.EstActif ? "actif" : "inactif";
        if (actif)
        {
            champ.Activer();
        }
        else
        {
            champ.Desactiver();
        }

        await journalisation.ConsignerAuditAsync(
            appelantId, actif ? "ChampFormulaireEnrolementActive" : "ChampFormulaireEnrolementDesactive", nameof(ChampFormulaireEnrolement), champ.Id,
            avant, actif ? "actif" : "inactif", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>ENR-01 — lecture du référentiel des champs du formulaire d'enrôlement.</summary>
public sealed class ListerChampsFormulaireEnrolementQuery(
    IChampFormulaireEnrolementRepository champs,
    IAutorisationService autorisation)
{
    /// <summary>Écran de paramétrage : voit AUSSI les champs désactivés, sans quoi ils seraient
    /// irrécupérables.</summary>
    public async Task<IReadOnlyList<ChampFormulaireEnrolementVue>> ExecuterAsync(Guid administrateurId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationChampsEnrolement, ct: ct).ConfigureAwait(false);

        return await ListerInterneAsync(activesUniquement: false, pourType: null, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Variante destinée au formulaire de soumission : aucune habilitation particulière n'est
    /// exigée — un chauffeur/transporteur non encore enrôlé n'a par construction aucune habilitation
    /// applicative, seulement la permission de soumettre (ENR-01, vérifiée par la commande).
    /// </summary>
    /// <param name="pourType">LOT 4 — quand il est renseigné, ne retourne que les champs applicables
    /// à ce type d'objet (les champs globaux + ceux du type). Nul = tous les champs actifs
    /// (comportement historique).</param>
    public Task<IReadOnlyList<ChampFormulaireEnrolementVue>> ListerPourSoumissionAsync(TypeObjetEnrolement? pourType = null, CancellationToken ct = default) =>
        ListerInterneAsync(activesUniquement: true, pourType, ct);

    private async Task<IReadOnlyList<ChampFormulaireEnrolementVue>> ListerInterneAsync(bool activesUniquement, TypeObjetEnrolement? pourType, CancellationToken ct)
    {
        var tous = await champs.ListerAsync(activesUniquement, ct).ConfigureAwait(false);

        return tous
            .Where(c => pourType is null || c.EstApplicableA(pourType.Value))
            .OrderBy(c => c.Ordre)
            .ThenBy(c => c.Libelle, StringComparer.CurrentCulture)
            .Select(c => new ChampFormulaireEnrolementVue(c.Id, c.Code, c.Libelle, c.EstObligatoire, c.EstActif, c.Ordre, c.TypeObjet))
            .ToList();
    }
}

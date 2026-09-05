using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.SuiviTemps;

namespace CIT.Plateforme.Application.Administration;

/// <summary>SLA-01 — les axes proposés à l'écran, désignés par leur nom et non par un
/// identifiant technique.</summary>
public sealed record AxesSlaDisponibles(
    IReadOnlyList<(Guid Id, string Nom)> Files,
    IReadOnlyList<(Guid Id, string Libelle)> Priorites);

/// <summary>Contrat C-05 — Gérer les paramètres SLA et le calendrier ouvré. CDC : SLA-01, SLA-02,
/// SLA-05, TPS-05. Aucune valeur par défaut métier n'est imposée (PC-12, Principe XIII).</summary>
public sealed class GererParametresSlaCommand(
    IParametreSlaRepository parametres,
    IFileDeTraitementRepository files,
    IPrioriteRepository priorites,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    /// <summary>
    /// SLA-01 / SLA-05 — files et priorités administrées, pour que l'écran fasse CHOISIR une file
    /// plutôt que saisir un GUID. L'habilitation exigée est celle du SLA : administrer un SLA par
    /// file ne doit pas supposer d'être aussi administrateur des files.
    /// </summary>
    public async Task<AxesSlaDisponibles> ListerAxesAsync(Guid administrateurId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationSla, ct: ct).ConfigureAwait(false);

        var toutesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var tousNiveaux = await priorites.ListerNiveauxAsync(ct).ConfigureAwait(false);

        return new AxesSlaDisponibles(
            toutesFiles.Select(f => (f.Id, f.Nom)).ToList(),
            tousNiveaux.Select(n => (n.Id, n.Libelle)).ToList());
    }

    /// <summary>
    /// SLA-01 / SLA-02 — <paramref name="fileId"/> (l'étape), <paramref name="prioriteId"/> et
    /// <paramref name="seuilEscalade"/> sont optionnels et par défaut absents : un paramétrage qui
    /// ne les précise pas s'applique largement, exactement comme avant l'ouverture de ces axes.
    /// Aucun appelant existant n'est donc à modifier, et aucune valeur métier n'est inventée.
    /// </summary>
    public async Task<Guid> DefinirAsync(
        Guid administrateurId, string module, string typeDemande, TimeSpan dureeCible, double seuilInformation, double seuilAlerte,
        Guid? fileId = null, Guid? prioriteId = null, double seuilEscalade = 0, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationSla, ct: ct).ConfigureAwait(false);

        var parametre = ParametreSlaAdministrable.Definir(module, typeDemande, dureeCible, seuilInformation, seuilAlerte);
        parametre.Cibler(fileId, prioriteId);
        parametre.FixerSeuilEscalade(seuilEscalade);
        await parametres.AjouterAsync(parametre, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            administrateurId, "ParametreSlaDefini", nameof(ParametreSlaAdministrable), parametre.Id,
            null, $"{module}/{typeDemande}={dureeCible} (file={fileId?.ToString() ?? "toutes"}, priorité={prioriteId?.ToString() ?? "toutes"}, escalade={seuilEscalade})", ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return parametre.Id;
    }

    /// <summary>SLA-05 — lecture pour l'écran d'administration.</summary>
    public async Task<IReadOnlyList<ParametreSlaAdministrable>> ListerAsync(Guid administrateurId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationSla, ct: ct).ConfigureAwait(false);
        return await parametres.ListerAsync(ct).ConfigureAwait(false);
    }

    /// <summary>SLA-05 — retrait d'un paramétrage. La suppression est tracée : un SLA retiré
    /// change le comportement de la surveillance, cela doit se voir dans le journal d'audit.</summary>
    public async Task SupprimerAsync(Guid administrateurId, Guid parametreId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationSla, ct: ct).ConfigureAwait(false);

        var parametre = await parametres.ObtenirAsync(parametreId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Paramètre SLA {parametreId} introuvable.");

        await parametres.SupprimerAsync(parametre, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(
            administrateurId, "ParametreSlaSupprime", nameof(ParametreSlaAdministrable), parametreId,
            $"{parametre.Module}/{parametre.TypeDemande}={parametre.DureeCible}", null, ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// REMÉDIATION AUDIT (post-implementation-audit.md §3.B) : variante administrant, en plus de
    /// la cible et des seuils, un calendrier ouvré réel (heures + jours de la semaine). Aucune
    /// valeur n'est fixée par défaut par le socle (PC-12 reste un paramètre) : heures et jours
    /// sont fournis par l'appelant (administrateur fonctionnel ou test).
    /// </summary>
    public async Task<Guid> DefinirAvecCalendrierAsync(
        Guid administrateurId, string module, string typeDemande, TimeSpan dureeCible, double seuilInformation, double seuilAlerte,
        TimeOnly heureDebut, TimeOnly heureFin, IReadOnlyCollection<DayOfWeek> joursOuvres, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationSla, ct: ct).ConfigureAwait(false);

        var parametre = ParametreSlaAdministrable.Definir(module, typeDemande, dureeCible, seuilInformation, seuilAlerte);
        parametre.AppliquerCalendrierOuvre(heureDebut, heureFin, joursOuvres);
        await parametres.AjouterAsync(parametre, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            administrateurId, "ParametreSlaDefini", nameof(ParametreSlaAdministrable), parametre.Id,
            null, $"{module}/{typeDemande}={dureeCible} (calendrier ouvré {heureDebut}-{heureFin})", ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return parametre.Id;
    }
}

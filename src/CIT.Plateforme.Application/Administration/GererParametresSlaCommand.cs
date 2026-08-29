using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.SuiviTemps;

namespace CIT.Plateforme.Application.Administration;

/// <summary>Contrat C-05 — Gérer les paramètres SLA et le calendrier ouvré. CDC : SLA-05, TPS-05.
/// Aucune valeur par défaut métier n'est imposée (PC-12, Principe XIII).</summary>
public sealed class GererParametresSlaCommand(
    IParametreSlaRepository parametres,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task DefinirAsync(
        Guid administrateurId, string module, string typeDemande, TimeSpan dureeCible, double seuilInformation, double seuilAlerte, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationSla, ct: ct).ConfigureAwait(false);

        var parametre = ParametreSlaAdministrable.Definir(module, typeDemande, dureeCible, seuilInformation, seuilAlerte);
        await parametres.AjouterAsync(parametre, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            administrateurId, "ParametreSlaDefini", nameof(ParametreSlaAdministrable), parametre.Id,
            null, $"{module}/{typeDemande}={dureeCible}", ct).ConfigureAwait(false);
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

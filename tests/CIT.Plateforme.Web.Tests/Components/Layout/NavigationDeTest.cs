using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Web.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace CIT.Plateforme.Web.Tests.Components.Layout;

/// <summary>
/// REMÉDIATION RECETTE UAT (D-05/D-15) — SUPPORT DES TESTS DE MENU.
///
/// <c>NavMenu</c> ne décide plus lui-même de ce qu'il affiche : la liste des liens visibles est
/// établie par <see cref="ConstruireNavigationQuery"/> à partir des habilitations réelles. Les
/// tests de rendu doivent donc déclarer <b>ce que l'utilisateur détient</b>, là où ils se
/// contentaient auparavant de déclarer son espace fonctionnel.
///
/// C'est le fond du défaut corrigé : l'espace seul ne disait rien des droits, et le menu affichait
/// tout — jusqu'à 16 liens sur 22 menant à un refus.
/// </summary>
public static class NavigationDeTest
{
    /// <summary>
    /// Enregistre une construction de navigation adossée à des habilitations explicites et, au
    /// besoin, à un référentiel de files.
    /// </summary>
    public static void Enregistrer(
        IServiceCollection services,
        Guid utilisateurId,
        string[] permissions,
        FileDeTraitement[]? files = null)
    {
        var habilitations = permissions
            .Select(p => HabilitationUtilisateur.Attribuer(utilisateurId, p))
            .ToList();

        // Enregistre le TYPE (et non une instance) pour que la resolution par scope du composant
        // fonctionne comme en production.
        services.AddSingleton<IHabilitationRepository>(new DepotHabilitationsFige(habilitations));
        services.AddSingleton<IFileDeTraitementRepository>(new DepotFilesFige(files ?? []));
        services.AddScoped<ConstruireNavigationQuery>();
        // Le menu passe par le cache court (checklist de mise en ligne, point 16).
        services.AddMemoryCache();
        services.AddSingleton<CIT.Plateforme.Web.Services.MenuEnCache>();

        // i18n LOT 1 — NavMenu / HeaderBar résolvent leurs libellés via la source de traduction
        // partagée (@inject IStringLocalizer<SharedResources>). Le harnais doit donc l'enregistrer,
        // comme en production. Les tests de menu portent sur les ROUTES : la langue est sans effet
        // sur elles (routes FR == routes EN), ce que NavigationI18nTests vérifie par ailleurs.
        services.AddSingleton<IStringLocalizer<SharedResources>, SharedResourcesLocalizer>();
    }

    /// <summary>Le projet de tests Web n'embarque pas de bibliothèque de doublures : deux stubs
    /// explicites suffisent, et se lisent mieux qu'une configuration de mock.</summary>
    private sealed class DepotHabilitationsFige(IReadOnlyList<HabilitationUtilisateur> habilitations)
        : IHabilitationRepository
    {
        public Task AjouterAsync(HabilitationUtilisateur habilitation, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<HabilitationUtilisateur>> ListerParUtilisateurAsync(
            Guid utilisateurId, CancellationToken ct = default) => Task.FromResult(habilitations);

        public Task RetirerAsync(Guid utilisateurId, string permissionCode, Guid? fileId = null,
            CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class DepotFilesFige(IReadOnlyList<FileDeTraitement> files) : IFileDeTraitementRepository
    {
        public Task AjouterAsync(FileDeTraitement file, CancellationToken ct = default) => Task.CompletedTask;

        public Task<FileDeTraitement?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(files.FirstOrDefault(f => f.Id == id));

        public Task<IReadOnlyList<FileDeTraitement>> ListerAsync(CancellationToken ct = default) =>
            Task.FromResult(files);
    }

    /// <summary>Toutes les permissions de la plateforme — pour un test qui n'éprouve pas le filtrage.</summary>
    public static string[] ToutesLesPermissions() => Permission.Tous.ToArray();
}

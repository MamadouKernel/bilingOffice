using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>
/// Lecture des sociétés clientes actives, pour que l'interface propose une SÉLECTION de sociétés
/// désignées par leurs informations déclarées au lieu de faire saisir un identifiant technique.
/// Soumise à la même habilitation que la modification de type (<c>Permission.CompteTypeModifier</c>)
/// afin qu'un utilisateur non habilité ne puisse pas énumérer le portefeuille clients.
/// </summary>
/// <param name="Nom">Nom seul, pour la liste de sélection.</param>
/// <param name="Rccm">RCCM, affiché en confirmation sous la sélection (peut être absent).</param>
public sealed record SocieteSelectionnable(Guid Id, string Nom, string? Rccm, TypeClient Profil);

public sealed class ListerSocietesActivesQuery(
    ISocieteClienteRepository societes,
    IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<SocieteSelectionnable>> ExecuterAsync(Guid appelantId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.CompteTypeModifier, ct: ct).ConfigureAwait(false);

        var actives = await societes.ListerParEtatAsync(EtatSociete.Active, ct).ConfigureAwait(false);

        return actives
            .Select(s => new SocieteSelectionnable(s.Id, s.NomAffichable, s.RccmAffichable, s.Profil))
            .OrderBy(s => s.Nom, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }
}

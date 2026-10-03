using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Incidents;
using CIT.Plateforme.Domain.Notifications;
using CIT.Plateforme.Domain.Referentiels;
using CIT.Plateforme.Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>
/// Garantit une ligne par valeur d'énumération dans chaque référentiel métier. STRICTEMENT
/// ADDITIF (comme <see cref="ProvisionnementTypesClient"/>) : une valeur présente n'est jamais
/// réécrite, l'administration a pu en changer le libellé, l'ordre ou l'activation. La migration
/// qui crée les tables y insère déjà ces valeurs (les clés étrangères l'exigent) ; ce
/// provisionnement couvre les valeurs ajoutées plus tard au code.
/// </summary>
public sealed class ProvisionnementReferentiels(PlateformeDbContext contexte, ILogger<ProvisionnementReferentiels> journal)
{
    public async Task ExecuterAsync(CancellationToken ct = default)
    {
        var ajouts =
            await CompleterAsync(contexte.GravitesIncident, c => new ReferentielGraviteIncident(c.Code, c.Fr, c.En, c.Ordre), ValeursInitiales.Gravites, ct).ConfigureAwait(false)
            + await CompleterAsync(contexte.MotifsCloture, c => new ReferentielMotifCloture(c.Code, c.Fr, c.En, c.Ordre), ValeursInitiales.MotifsCloture, ct).ConfigureAwait(false)
            + await CompleterAsync(contexte.CategoriesDocument, c => new ReferentielCategorieDocument(c.Code, c.Fr, c.En, c.Ordre), ValeursInitiales.CategoriesDocument, ct).ConfigureAwait(false)
            + await CompleterAsync(contexte.CanauxNotification, c => new ReferentielCanalNotification(c.Code, c.Fr, c.En, c.Ordre), ValeursInitiales.Canaux, ct).ConfigureAwait(false);

        if (ajouts == 0)
        {
            return;
        }

        await contexte.SaveChangesAsync(ct).ConfigureAwait(false);
        journal.LogInformation("Référentiels métier : {Nombre} valeur(s) initialisée(s).", ajouts);
    }

    private static async Task<int> CompleterAsync<TReferentiel, TCode>(
        DbSet<TReferentiel> table,
        Func<ValeurInitiale<TCode>, TReferentiel> creer,
        IReadOnlyList<ValeurInitiale<TCode>> initiales,
        CancellationToken ct)
        where TReferentiel : ValeurReferentielle<TCode>
        where TCode : struct, Enum
    {
        var presents = await table.Select(r => r.Code).ToListAsync(ct).ConfigureAwait(false);
        var manquants = initiales.Where(i => !presents.Contains(i.Code)).ToList();
        table.AddRange(manquants.Select(creer));
        return manquants.Count;
    }
}

/// <summary>Valeur de départ d'un référentiel (libellés par défaut, modifiables ensuite).</summary>
public sealed record ValeurInitiale<TCode>(TCode Code, string Fr, string En, int Ordre)
    where TCode : struct, Enum;

/// <summary>Libellés par défaut des référentiels — source unique pour la migration et le provisionnement.</summary>
public static class ValeursInitiales
{
    public static readonly IReadOnlyList<ValeurInitiale<GraviteIncident>> Gravites =
    [
        new(GraviteIncident.Mineure, "Mineure", "Minor", 10),
        new(GraviteIncident.Majeure, "Majeure", "Major", 20),
        new(GraviteIncident.Critique, "Critique", "Critical", 30),
    ];

    public static readonly IReadOnlyList<ValeurInitiale<MotifClotureType>> MotifsCloture =
    [
        new(MotifClotureType.Normale, "Clôture normale", "Normal closure", 10),
        new(MotifClotureType.AbsenceReponse, "Absence de réponse du client", "No response from customer", 20),
    ];

    public static readonly IReadOnlyList<ValeurInitiale<CategorieDocumentBillingImport>> CategoriesDocument =
    [
        new(CategorieDocumentBillingImport.DeclarationDouane, "Déclaration en douane", "Customs declaration", 10),
        new(CategorieDocumentBillingImport.CopieDuBl, "Copie du BL", "Copy of the B/L", 20),
        new(CategorieDocumentBillingImport.PreuvePaiement, "Preuve de paiement", "Proof of payment", 25),
        new(CategorieDocumentBillingImport.AutresPieces, "Autres pièces", "Other documents", 30),
    ];

    public static readonly IReadOnlyList<ValeurInitiale<CanalNotification>> Canaux =
    [
        new(CanalNotification.InApp, "Centre de notifications", "Notification centre", 10),
        new(CanalNotification.Email, "E-mail", "Email", 20),
    ];
}

/// <summary>Lecture des référentiels métier : un aiguillage par domaine vers sa table.</summary>
public sealed class ReferentielsRepository(PlateformeDbContext contexte) : Application.Administration.IReferentielsRepository
{
    public async Task<IReadOnlyList<IValeurReferentielle>> ListerAsync(
        Application.Administration.DomaineReferentiel domaine, CancellationToken ct = default) =>
        domaine switch
        {
            Application.Administration.DomaineReferentiel.GraviteIncident => await ListerAsync(contexte.GravitesIncident, ct).ConfigureAwait(false),
            Application.Administration.DomaineReferentiel.MotifCloture => await ListerAsync(contexte.MotifsCloture, ct).ConfigureAwait(false),
            Application.Administration.DomaineReferentiel.CategorieDocument => await ListerAsync(contexte.CategoriesDocument, ct).ConfigureAwait(false),
            Application.Administration.DomaineReferentiel.CanalNotification => await ListerAsync(contexte.CanauxNotification, ct).ConfigureAwait(false),
            _ => throw new ArgumentOutOfRangeException(nameof(domaine), domaine, "Référentiel inconnu."),
        };

    private static async Task<IReadOnlyList<IValeurReferentielle>> ListerAsync<T>(DbSet<T> table, CancellationToken ct)
        where T : class, IValeurReferentielle =>
        await table.ToListAsync(ct).ConfigureAwait(false);
}

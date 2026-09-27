using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Domain.Comptes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Persistence;

public sealed class ParametreTypeClientRepository(PlateformeDbContext contexte) : IParametreTypeClientRepository
{
    public async Task<IReadOnlyList<ParametreTypeClient>> ListerAsync(CancellationToken ct = default) =>
        await contexte.TypesClient.ToListAsync(ct).ConfigureAwait(false);

    public async Task<ParametreTypeClient?> ObtenirAsync(TypeClient type, CancellationToken ct = default)
    {
        return await contexte.TypesClient.FirstOrDefaultAsync(p => p.Code == type, ct).ConfigureAwait(false);
    }
}

/// <summary>
/// Garantit une ligne de paramétrage par valeur de <see cref="TypeClient"/>. STRICTEMENT ADDITIF,
/// comme <see cref="ProvisionnementLibellesStatut"/> : un type déjà présent n'est jamais réécrit,
/// car l'administration a pu en modifier le libellé ou l'activation.
/// </summary>
public sealed class ProvisionnementTypesClient(PlateformeDbContext contexte, ILogger<ProvisionnementTypesClient> journal)
{
    private static readonly (TypeClient Type, string Fr, string En, string DescFr, string DescEn, int Ordre)[] Initiaux =
    [
        (TypeClient.Normal, "Normal (paiement à l'acte)", "Standard (pay per transaction)",
            "Paiement exigé à chaque dossier. Bon d'enlèvement délivré dès confirmation du règlement.",
            "Payment required for each file. Release order issued once payment is confirmed.", 10),
        (TypeClient.EnCompte, "En compte (facturation différée)", "Account (deferred billing)",
            "Facturation périodique sur un compte ouvert chez CIT, sans paiement à chaque dossier.",
            "Periodic billing on an account opened with CIT, without payment for each file.", 20),
        (TypeClient.Deposit, "Deposit (dépôt de garantie)", "Deposit (guarantee deposit)",
            "Les opérations sont imputées sur un dépôt préalable, lettré par la comptabilité.",
            "Operations are charged against a prior deposit, matched by accounting.", 30),
        (TypeClient.IES, "IES", "IES",
            "Circuit dédié IES : instruction du manifeste et contrôle documentaire spécifiques.",
            "Dedicated IES circuit: specific manifest review and document check.", 40),
    ];

    public async Task ExecuterAsync(CancellationToken ct = default)
    {
        var presents = await contexte.TypesClient.Select(p => p.Code).ToListAsync(ct).ConfigureAwait(false);
        var manquants = Initiaux.Where(i => !presents.Contains(i.Type)).ToList();
        if (manquants.Count == 0)
        {
            return;
        }

        foreach (var i in manquants)
        {
            contexte.TypesClient.Add(ParametreTypeClient.Creer(i.Type, i.Fr, i.En, i.DescFr, i.DescEn, i.Ordre));
        }

        await contexte.SaveChangesAsync(ct).ConfigureAwait(false);
        journal.LogInformation("Référentiel des types de client : {Nombre} type(s) initialisé(s).", manquants.Count);
    }
}

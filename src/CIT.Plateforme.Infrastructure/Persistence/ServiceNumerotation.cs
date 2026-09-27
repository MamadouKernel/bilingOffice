using CIT.Plateforme.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>
/// Implémentation du service de numérotation (T008, decision D6) : séquences SQL Server
/// transactionnellement sûres, sans contention de table, préfixe paramétrable par module/type.
/// <para>
/// ÉCART DOCUMENTÉ vis-à-vis de research.md D6 : D6 envisage une séquence SQL distincte par
/// couple (module, type). Ces couples n'étant pas fixés à l'avance (paramétrables — Principe VI),
/// deux séquences globales (ticket, dossier) sont utilisées ici pour garantir l'unicité
/// transactionnelle exigée par SOC-01/SOC-09 ; le préfixe humainement lisible encode le
/// module/type. Une partition physique par (module, type) reste possible sans rupture de contrat
/// public si le besoin est confirmé — voir tasks.md § Contrôle final, « écarts éventuels ».
/// </para>
/// </summary>
public sealed class ServiceNumerotation(PlateformeDbContext dbContext) : IServiceNumerotation
{
    public const string SequenceTicket = "seq_ticket_numero";
    public const string SequenceDossier = "seq_dossier_numero";

    public async Task<string> ProchainNumeroTicketAsync(string module, string typeDemande, CancellationToken ct = default)
    {
        var valeur = await ProchaineValeurAsync(SequenceTicket, ct).ConfigureAwait(false);
        var prefixeModule = Prefixe(module);
        var prefixeType = Prefixe(typeDemande);

        // Module et type confondus (ex. Enrôlement/Enrôlement) : un seul préfixe, sans quoi le
        // numéro affiché devient « ENR-ENR-000006 ». L'unicité reste portée par la séquence.
        return prefixeModule == prefixeType
            ? $"{prefixeModule}-{valeur:D6}"
            : $"{prefixeModule}-{prefixeType}-{valeur:D6}";
    }

    public async Task<string> ProchainNumeroDossierAsync(CancellationToken ct = default)
    {
        var valeur = await ProchaineValeurAsync(SequenceDossier, ct).ConfigureAwait(false);
        return $"DOS-{valeur:D6}";
    }

    private async Task<long> ProchaineValeurAsync(string sequence, CancellationToken ct)
    {
        // Exécution ADO.NET directe (pas de composition LINQ) : « NEXT VALUE FOR » est interdit
        // par SQL Server dans toute sous-requête, et EF Core peut composer SqlQueryRaw + un
        // opérateur LINQ (SingleAsync) en sous-requête. `sequence` provient uniquement des
        // constantes internes SequenceTicket/SequenceDossier (jamais d'entrée utilisateur) — un
        // identifiant d'objet SQL ne peut de toute façon pas être paramétré en T-SQL.
        var connexion = dbContext.Database.GetDbConnection();
        var ouverteIci = connexion.State != System.Data.ConnectionState.Open;
        if (ouverteIci)
        {
            await connexion.OpenAsync(ct).ConfigureAwait(false);
        }

        try
        {
            await using var commande = connexion.CreateCommand();
            commande.CommandText = $"SELECT NEXT VALUE FOR {sequence}";
            if (dbContext.Database.CurrentTransaction is { } transaction)
            {
                commande.Transaction = transaction.GetDbTransaction();
            }

            var resultat = await commande.ExecuteScalarAsync(ct).ConfigureAwait(false);
            return Convert.ToInt64(resultat);
        }
        finally
        {
            if (ouverteIci)
            {
                await connexion.CloseAsync().ConfigureAwait(false);
            }
        }
    }

    private static string Prefixe(string valeur)
    {
        var nettoye = new string([.. valeur.Where(char.IsLetterOrDigit)]).ToUpperInvariant();
        return nettoye.Length <= 3 ? nettoye.PadRight(3, 'X') : nettoye[..3];
    }
}

using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.BillingExport;
using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Notifications.Resolveurs;

/// <summary>
/// Notifie les AGENTS habilités à traiter la file où une demande vient d'arriver (événement
/// <see cref="TypeArrivee"/>, émis à la soumission). Les destinataires sont les utilisateurs
/// détenant la permission de traitement de cette file — liés à la file OU habilités globalement
/// (<see cref="IHabilitationRepository.ListerUtilisateursAvecPermissionAsync"/> couvre les deux).
/// Un agent d'une autre file n'est jamais ciblé (cloisonnement). La table ci-dessous couvre les
/// files d'ARRIVÉE d'une demande (noms uniques) ; les transferts entre files sont hors périmètre ici.
/// </summary>
public sealed class ResolveurAgentsDeLaFile(
    ITicketRepository tickets,
    IFileDeTraitementRepository files,
    IHabilitationRepository habilitations) : IResolveurDestinataires
{
    public const string TypeArrivee = "TicketArriveeEnFile";

    public bool PeutResoudre(string typeEvenement) => typeEvenement == TypeArrivee;

    public async Task<IReadOnlyList<Guid>> ResoudreAsync(string typeEvenement, string chargeUtile, CancellationToken ct = default)
    {
        if (!PeutResoudre(typeEvenement) || ResolveurChargeUtile.LireGuid(chargeUtile, "ticketId") is not Guid ticketId)
        {
            return [];
        }

        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false);
        if (ticket is null)
        {
            return [];
        }

        var file = (await files.ListerAsync(ct).ConfigureAwait(false)).FirstOrDefault(f => f.Id == ticket.FileCouranteId);
        if (file is null || !PermissionsParNomFile.TryGetValue(file.Nom, out var permissions))
        {
            return [];
        }

        var destinataires = new HashSet<Guid>();
        foreach (var permission in permissions)
        {
            foreach (var id in await habilitations.ListerUtilisateursAvecPermissionAsync(permission, ticket.FileCouranteId, ct).ConfigureAwait(false))
            {
                destinataires.Add(id);
            }
        }

        return [.. destinataires];
    }

    public Task<Guid?> ResoudreTicketAsync(string chargeUtile, CancellationToken ct = default) =>
        Task.FromResult(ResolveurChargeUtile.LireGuid(chargeUtile, "ticketId"));

    /// <summary>Permission de traitement par file d'arrivée (nom unique) — ciblage « agents de la file ».
    /// Public : réutilisé par l'annuaire des agents par rubrique (supervision), même définition d'« agent d'une file ».</summary>
    public static readonly IReadOnlyDictionary<string, string[]> PermissionsParNomFile = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        [SoumettreDemandeImportCommand.FilePiloteManifeste] = [Permission.BillingImportManifesteInstruire],
        [NomsFilesExport.BackOfficeExport] = [Permission.BillingExportBackOfficeInstruire],
        ["Agent Gate"] = [Permission.EnrolementInstruire],
        ["Support Opérationnel"] = [Permission.ComplaintInstruire],
    };
}

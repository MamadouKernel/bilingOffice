using CIT.Plateforme.Domain.Notifications;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Guichet;

/// <summary>
/// FEATURE 010 (Guichet) — règles et modèles de notification de départ pour les rendez-vous au
/// Billing Office (confirmation, annulation), dans TOUS les environnements.
///
/// Vit dans la feature Guichet, PAS dans la couche Notifications : l'invariant I4 (spec.md §23)
/// interdit que le provisionnement du moteur de notifications connaisse le vocabulaire du Guichet.
/// Même contrat que <c>ProvisionnementNotifications</c> : STRICTEMENT ADDITIF, approvisionnement
/// PAR TYPE d'événement — un type déjà paramétré n'est jamais retouché. Textes de départ non
/// contractuels (CLAR-NOT-05), modifiables dans l'administration des notifications.
/// </summary>
public sealed class ProvisionnementRendezVous(
    PlateformeDbContext contexte,
    ILogger<ProvisionnementRendezVous> journal)
{
    private static readonly (string Type, CanalNotification Canal, bool Obligatoire, string Objet, string ObjetEn)[] Regles =
    [
        ("RendezVousReserve", CanalNotification.InApp, false, "Votre rendez-vous au Billing Office est confirmé", "Your Billing Office appointment is confirmed"),
        ("RendezVousReserve", CanalNotification.Email, false, "Votre rendez-vous au Billing Office est confirmé", "Your Billing Office appointment is confirmed"),
        ("RendezVousAnnule", CanalNotification.InApp, false, "Votre rendez-vous au Billing Office est annulé", "Your Billing Office appointment has been cancelled"),
        ("RendezVousAnnule", CanalNotification.Email, false, "Votre rendez-vous au Billing Office est annulé", "Your Billing Office appointment has been cancelled"),
    ];

    public async Task ExecuterAsync(CancellationToken ct = default)
    {
        var typesParametres = (await contexte.ReglesDiffusion.Select(r => r.TypeEvenement).Distinct().ToListAsync(ct).ConfigureAwait(false))
            .ToHashSet(StringComparer.Ordinal);
        var aCreer = Regles.Where(r => !typesParametres.Contains(r.Type)).ToList();
        if (aCreer.Count == 0)
        {
            return;
        }

        foreach (var (type, canal, obligatoire, objet, objetEn) in aCreer)
        {
            contexte.ReglesDiffusion.Add(RegleDiffusion.Creer(type, canal, obligatoire));

            if (!await contexte.ModelesNotification.AnyAsync(m => m.TypeEvenement == type && m.Canal == canal, ct).ConfigureAwait(false))
            {
                var modele = ModeleNotification.Creer(type, canal, objet, Corps(type));
                modele.DefinirAnglais(objetEn, CorpsAnglais(type));
                contexte.ModelesNotification.Add(modele);
            }
        }

        await contexte.SaveChangesAsync(ct).ConfigureAwait(false);
        journal.LogInformation("{Nombre} règle(s) de diffusion rendez-vous approvisionnée(s).", aCreer.Count);
    }

    private static string Corps(string type) => type switch
    {
        "RendezVousReserve" => "Votre rendez-vous au Billing Office est confirmé.\n\nRetrouvez la date, l'heure et le dossier concerné dans « Mes rendez-vous ». Présentez-vous muni de vos pièces justificatives.",
        "RendezVousAnnule" => "Votre rendez-vous au Billing Office a été annulé.\n\nVous pouvez en réserver un nouveau à tout moment depuis « Mes rendez-vous ».",
        _ => string.Empty,
    };

    private static string CorpsAnglais(string type) => type switch
    {
        "RendezVousReserve" => "Your appointment at the Billing Office is confirmed.\n\nFind the date, time and related file in \"My appointments\". Please come with your supporting documents.",
        "RendezVousAnnule" => "Your appointment at the Billing Office has been cancelled.\n\nYou can book a new one at any time from \"My appointments\".",
        _ => string.Empty,
    };
}

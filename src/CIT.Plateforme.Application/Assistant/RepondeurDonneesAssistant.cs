using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Guichet;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Assistant;

/// <summary>
/// RÉPONSES DE L'ASSISTANT À PARTIR DES DONNÉES DE L'USAGER — « où en est mon dossier ? »,
/// « qu'attend-on de moi ? », « quand est mon rendez-vous ? ». Déterministe et local : aucune
/// donnée ne quitte la plateforme, aucune réponse n'est inventée.
///
/// CLOISONNEMENT : exactement celui des écrans client (ObtenirDashboardClientQuery) — la société
/// de l'usager, restreinte à ses propres demandes si son droit de consultation l'exige. Un numéro
/// hors de ce périmètre reçoit la même réponse qu'un numéro inexistant : l'assistant ne révèle
/// jamais qu'un dossier existe ailleurs.
///
/// LECTURE SEULE : l'assistant informe et oriente, il n'agit jamais sur un dossier.
///
/// Renvoie <c>null</c> quand la question ne porte pas sur les données de l'usager : l'assistant
/// généraliste (<see cref="IAssistantConversationnel"/>) prend alors le relais.
/// </summary>
public sealed partial class RepondeurDonneesAssistant(
    IUtilisateurSocieteRepository utilisateursSociete,
    IDossierRepository dossiers,
    ITicketRepository tickets,
    IFileDeTraitementRepository files,
    ILibelleStatutRepository libellesStatut,
    ListerMesRendezVousQuery rendezVous,
    IHorloge horloge)
{
    private const int NombreMaxListe = 5;

    public async Task<ReponseAssistant?> RepondreAsync(Guid utilisateurId, string question, CancellationToken ct = default)
    {
        var texte = Normaliser(question);
        var numeros = ReferencesCitees(question);
        var intention = numeros.Count > 0 ? Intention.EtatDossier
            : Contient(texte, "rendez", "rdv") ? Intention.RendezVous
            : Contient(texte, "attente", "complement", "attend", "a faire", "action") ? Intention.ActionsAttendues
            : Contient(texte, "mes dossiers", "mes demandes", "ou en sont", "ou en est", "mon dossier", "ma demande", "mes tickets", "combien", "suivi") ? Intention.Synthese
            : Intention.Aucune;

        if (intention == Intention.Aucune)
        {
            return null;
        }

        if (intention == Intention.RendezVous)
        {
            return await RepondreRendezVousAsync(utilisateurId, ct).ConfigureAwait(false);
        }

        var perimetre = await PerimetreAsync(utilisateurId, ct).ConfigureAwait(false);
        if (perimetre is null)
        {
            // Compte sans société (agent CIT) : ces questions portent sur des dossiers clients.
            return null;
        }

        return intention switch
        {
            Intention.EtatDossier => await RepondreEtatAsync(perimetre, numeros, ct).ConfigureAwait(false),
            Intention.ActionsAttendues => RepondreActionsAttendues(perimetre),
            _ => RepondreSynthese(perimetre),
        };
    }

    private enum Intention { Aucune, EtatDossier, ActionsAttendues, Synthese, RendezVous }

    /// <summary>Tickets visibles par l'usager : même règle que le tableau de bord client.</summary>
    private sealed record Perimetre(IReadOnlyList<Ticket> Tickets, IReadOnlyDictionary<Guid, string> NumerosDossier);

    private async Task<Perimetre?> PerimetreAsync(Guid utilisateurId, CancellationToken ct)
    {
        var rattachement = await utilisateursSociete.ObtenirParUtilisateurAsync(utilisateurId, ct).ConfigureAwait(false);
        if (rattachement is null || !rattachement.EstActif)
        {
            return null;
        }

        var dossiersVisibles = await dossiers.ListerParSocieteAsync(rattachement.SocieteId, ct).ConfigureAwait(false);
        if (rattachement.DroitConsultation == DroitConsultation.PropresDemandesUniquement && !rattachement.EstAdministrateur)
        {
            dossiersVisibles = [.. dossiersVisibles.Where(d => d.AuteurId == utilisateurId)];
        }

        var ticketsVisibles = await tickets.ListerParDossiersAsync(dossiersVisibles.Select(d => d.Id), ct).ConfigureAwait(false);
        return new Perimetre(ticketsVisibles, dossiersVisibles.ToDictionary(d => d.Id, d => d.Numero));
    }

    private async Task<ReponseAssistant> RepondreEtatAsync(Perimetre perimetre, IReadOnlyList<string> numeros, CancellationToken ct)
    {
        var libelles = await LibellesAsync(ct).ConfigureAwait(false);
        var filesParId = (await files.ListerAsync(ct).ConfigureAwait(false)).ToDictionary(f => f.Id, f => f.Nom);
        var reponse = new StringBuilder();

        foreach (var numero in numeros)
        {
            var trouves = perimetre.Tickets.Where(t => Correspond(t, numero, perimetre)).ToList();
            if (trouves.Count == 0)
            {
                // Même message qu'un numéro inexistant : rien n'est révélé hors du périmètre.
                reponse.AppendLine(CultureInfo.InvariantCulture,
                    $"Je ne trouve aucun dossier « {numero} » parmi ceux que vous pouvez consulter. Vérifiez le numéro, ou ouvrez « Mes demandes ».");
                continue;
            }

            foreach (var ticket in trouves)
            {
                reponse.AppendLine(DecrireTicket(ticket, libelles, filesParId));
            }
        }

        return new ReponseAssistant(reponse.ToString().TrimEnd(), EscaladeRecommandee: false);
    }

    private ReponseAssistant RepondreActionsAttendues(Perimetre perimetre)
    {
        var attendus = perimetre.Tickets
            .Where(t => t.Statut == StatutTicket.EnAttenteClient)
            .OrderBy(t => t.DateDebutAttenteClienteUtc ?? t.DateDerniereActiviteUtc)
            .ToList();

        if (attendus.Count == 0)
        {
            return new ReponseAssistant("Bonne nouvelle : aucun de vos dossiers n'attend d'action de votre part.", false);
        }

        var texte = new StringBuilder().Append(CultureInfo.InvariantCulture,
            $"{attendus.Count} dossier(s) attendent une réponse de votre part :");
        foreach (var t in attendus.Take(NombreMaxListe))
        {
            texte.AppendLine().Append(CultureInfo.InvariantCulture,
                $"• {Reference(t)} — complément demandé le {Date(t.DateDebutAttenteClienteUtc ?? t.DateDerniereActiviteUtc)}. Ouvrez le dossier : /tickets/{t.Id}");
        }

        AjouterSuite(texte, attendus.Count);
        texte.AppendLine().Append("Tant que vous n'avez pas répondu, le délai de traitement CIT est suspendu.");
        return new ReponseAssistant(texte.ToString(), false);
    }

    private ReponseAssistant RepondreSynthese(Perimetre perimetre)
    {
        var enCours = perimetre.Tickets.Count(t => t.Statut is StatutTicket.EnAttenteDePriseEnCharge or StatutTicket.EnCours);
        var attendus = perimetre.Tickets.Count(t => t.Statut == StatutTicket.EnAttenteClient);
        var termines = perimetre.Tickets.Count(t => t.Statut is StatutTicket.Cloture or StatutTicket.Annule);

        var texte = new StringBuilder().Append(CultureInfo.InvariantCulture,
            $"Vous avez {enCours} dossier(s) en cours de traitement chez CIT, {attendus} en attente de votre réponse et {termines} terminé(s).");

        var recents = perimetre.Tickets.OrderByDescending(t => t.DateDerniereActiviteUtc).Take(NombreMaxListe).ToList();
        if (recents.Count > 0)
        {
            texte.AppendLine().Append("Dernières activités :");
            foreach (var t in recents)
            {
                texte.AppendLine().Append(CultureInfo.InvariantCulture,
                    $"• {Reference(t)} — mis à jour le {Date(t.DateDerniereActiviteUtc)}. /tickets/{t.Id}");
            }
        }

        if (attendus > 0)
        {
            texte.AppendLine().Append("Demandez-moi « qu'attend-on de moi ? » pour voir les dossiers à compléter.");
        }

        return new ReponseAssistant(texte.ToString(), false);
    }

    private async Task<ReponseAssistant> RepondreRendezVousAsync(Guid utilisateurId, CancellationToken ct)
    {
        var aujourdHui = DateOnly.FromDateTime(horloge.MaintenantUtc);
        var prochains = (await rendezVous.ExecuterAsync(utilisateurId, ct).ConfigureAwait(false))
            .Where(r => r.Statut == "Reserve" && r.Date >= aujourdHui)
            .OrderBy(r => r.Date).ThenBy(r => r.HeureDebut)
            .ToList();

        if (prochains.Count == 0)
        {
            return new ReponseAssistant(
                "Vous n'avez aucun rendez-vous à venir. Pour en réserver un, ouvrez « Mes rendez-vous ».", false);
        }

        var premier = prochains[0];
        var texte = new StringBuilder().Append(CultureInfo.InvariantCulture,
            $"Votre prochain rendez-vous au Billing Office : le {premier.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)} de {premier.HeureDebut:hh\\:mm} à {premier.HeureFin:hh\\:mm}, pour le dossier {premier.NumeroTicket}.");
        if (prochains.Count > 1)
        {
            texte.AppendLine().Append(CultureInfo.InvariantCulture, $"Vous avez {prochains.Count - 1} autre(s) rendez-vous à venir dans « Mes rendez-vous ».");
        }

        return new ReponseAssistant(texte.ToString(), false);
    }

    private static string DecrireTicket(Ticket t, IReadOnlyDictionary<string, string> libelles, IReadOnlyDictionary<Guid, string> filesParId)
    {
        var statut = libelles.TryGetValue(t.Statut.ToString(), out var l) ? l : t.Statut.ToString();
        var etape = filesParId.TryGetValue(t.FileCouranteId, out var nomFile) ? $", à l'étape « {nomFile} »" : string.Empty;
        var suite = t.Statut switch
        {
            StatutTicket.EnAttenteClient => "CIT attend un complément de votre part : ouvrez le dossier pour répondre.",
            StatutTicket.EnAttenteDePriseEnCharge => "Il sera pris en charge par un agent CIT. Aucune action n'est attendue de votre part.",
            StatutTicket.EnCours => "Un agent CIT le traite. Aucune action n'est attendue de votre part.",
            StatutTicket.Cloture => "Ce dossier est terminé.",
            _ => "Ce dossier a été annulé.",
        };

        return $"{Reference(t)} : « {statut} »{etape}, dernière activité le {Date(t.DateDerniereActiviteUtc)}. {suite} Ouvrez le dossier : /tickets/{t.Id}";
    }

    private async Task<IReadOnlyDictionary<string, string>> LibellesAsync(CancellationToken ct) =>
        (await libellesStatut.ListerAsync(ct).ConfigureAwait(false)).ToDictionary(l => l.Code, l => l.LibelleFr);

    private static bool Correspond(Ticket t, string numero, Perimetre perimetre) =>
        Egal(t.Numero, numero) || Egal(t.NumeroBl, numero) || Egal(t.NumeroBooking, numero)
        || (perimetre.NumerosDossier.TryGetValue(t.DossierId, out var dossier) && Egal(dossier, numero));

    private static bool Egal(string? a, string b) => a is not null && string.Equals(a.Trim(), b, StringComparison.OrdinalIgnoreCase);

    private static string Reference(Ticket t) =>
        t.NumeroBl is { Length: > 0 } bl ? $"Dossier {t.Numero} (BL {bl})"
        : t.NumeroBooking is { Length: > 0 } bk ? $"Dossier {t.Numero} (booking {bk})"
        : $"Dossier {t.Numero}";

    /// <summary>Abidjan est à UTC+0 toute l'année, sans heure d'été : l'heure UTC stockée EST l'heure d'Abidjan.</summary>
    private static string Date(DateTime utc) => utc.ToString("dd/MM/yyyy à HH:mm", CultureInfo.InvariantCulture);

    private static void AjouterSuite(StringBuilder texte, int total)
    {
        if (total > NombreMaxListe)
        {
            texte.AppendLine().Append(CultureInfo.InvariantCulture, $"… et {total - NombreMaxListe} autre(s), visibles dans « Mes demandes ».");
        }
    }

    /// <summary>Références citées : une suite d'au moins 5 caractères mêlant chiffres, lettres et tirets (ex. IMP-000123, MSCU1234567).</summary>
    public static IReadOnlyList<string> ReferencesCitees(string question) =>
        [.. ModeleReference().Matches(question)
            .Select(m => m.Value.Trim('-'))
            .Where(v => v.Any(char.IsDigit) && !ModeleDate().IsMatch(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)];

    [GeneratedRegex(@"\b[A-Za-z0-9][A-Za-z0-9\-/]{4,39}\b")]
    private static partial Regex ModeleReference();

    /// <summary>« 27/09/2026 » ou « 2026-09-27 » : une date n'est pas un numéro de dossier.</summary>
    [GeneratedRegex(@"^(\d{1,2}[/-]\d{1,2}[/-]\d{2,4}|\d{4}-\d{1,2}-\d{1,2})$")]
    private static partial Regex ModeleDate();

    private static bool Contient(string texte, params string[] termes) => termes.Any(texte.Contains);

    /// <summary>Minuscules sans accents, pour reconnaître « complément », « Complement » ou « COMPLÉMENT ».</summary>
    private static string Normaliser(string texte)
    {
        var decompose = texte.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        return new string([.. decompose.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)]);
    }
}

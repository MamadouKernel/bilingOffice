using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Guichet;
using CIT.Plateforme.Domain;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Assistant;

/// <summary>
/// RÉPONSES DE L'ASSISTANT À PARTIR DES DONNÉES DE L'USAGER — « où en est mon dossier ? »,
/// « qu'attend-on de moi ? », « quand est mon rendez-vous ? ». Déterministe et local : aucune
/// donnée ne quitte la plateforme, aucune réponse n'est inventée.
///
/// BILINGUE (NFR-07) : la langue de réponse suit la culture active (fr-FR / en-US), comme le reste
/// de la plateforme. La détection d'intention reconnaît les termes des DEUX langues ; les libellés
/// de statut viennent de <see cref="Domain.Tickets.LibelleStatutTicket"/> (FR/EN administrables).
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

    /// <summary>Anglais si la culture active commence par « en » (même règle que <see cref="Langue"/>).</summary>
    private static bool EnAnglais() => Langue.EstAnglais(CultureInfo.CurrentUICulture.Name);

    /// <summary>Choisit le texte selon la culture active — FR de référence, EN si culture anglaise.</summary>
    private static string T(string fr, string en) => EnAnglais() ? en : fr;

    public async Task<ReponseAssistant?> RepondreAsync(Guid utilisateurId, string question, CancellationToken ct = default)
    {
        var texte = Normaliser(question);
        var numeros = ReferencesCitees(question);
        var intention = numeros.Count > 0 ? Intention.EtatDossier
            : Contient(texte, "rendez", "rdv", "appointment", "meeting") ? Intention.RendezVous
            : Contient(texte, "attente", "complement", "attend", "a faire", "action",
                       "waiting", "pending", "complete", "to do", "need from me") ? Intention.ActionsAttendues
            : Contient(texte, "mes dossiers", "mes demandes", "ou en sont", "ou en est", "mon dossier",
                       "ma demande", "mes tickets", "combien", "suivi",
                       "my cases", "my files", "my requests", "where are", "where is", "status",
                       "how many", "my tickets", "follow up") ? Intention.Synthese
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
                reponse.AppendLine(T(
                    $"Je ne trouve aucun dossier « {numero} » parmi ceux que vous pouvez consulter. Vérifiez le numéro, ou ouvrez « Mes demandes ».",
                    $"I can't find any case \"{numero}\" among those you can view. Check the number, or open \"My requests\"."));
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
            return new ReponseAssistant(T(
                "Bonne nouvelle : aucun de vos dossiers n'attend d'action de votre part.",
                "Good news: none of your cases is waiting for action from you."), false);
        }

        var texte = new StringBuilder().Append(T(
            $"{attendus.Count} dossier(s) attendent une réponse de votre part :",
            $"{attendus.Count} case(s) are waiting for your response:"));
        foreach (var t in attendus.Take(NombreMaxListe))
        {
            texte.AppendLine().Append(T(
                $"• {Reference(t)} — complément demandé le {Date(t.DateDebutAttenteClienteUtc ?? t.DateDerniereActiviteUtc)}. Ouvrez le dossier : /tickets/{t.Id}",
                $"• {Reference(t)} — additional information requested on {Date(t.DateDebutAttenteClienteUtc ?? t.DateDerniereActiviteUtc)}. Open the case: /tickets/{t.Id}"));
        }

        AjouterSuite(texte, attendus.Count);
        texte.AppendLine().Append(T(
            "Tant que vous n'avez pas répondu, le délai de traitement CIT est suspendu.",
            "Until you reply, CIT's processing time is on hold."));
        return new ReponseAssistant(texte.ToString(), false);
    }

    private ReponseAssistant RepondreSynthese(Perimetre perimetre)
    {
        var enCours = perimetre.Tickets.Count(t => t.Statut is StatutTicket.EnAttenteDePriseEnCharge or StatutTicket.EnCours);
        var attendus = perimetre.Tickets.Count(t => t.Statut == StatutTicket.EnAttenteClient);
        var termines = perimetre.Tickets.Count(t => t.Statut is StatutTicket.Cloture or StatutTicket.Annule);

        var texte = new StringBuilder().Append(T(
            $"Vous avez {enCours} {(enCours > 1 ? "dossiers" : "dossier")} en cours de traitement chez CIT, {attendus} en attente de votre réponse et {termines} {(termines > 1 ? "terminés" : "terminé")}.",
            $"You have {enCours} {(enCours == 1 ? "case" : "cases")} being processed at CIT, {attendus} awaiting your response and {termines} completed."));

        var recents = perimetre.Tickets.OrderByDescending(t => t.DateDerniereActiviteUtc).Take(NombreMaxListe).ToList();
        if (recents.Count > 0)
        {
            texte.AppendLine().Append(T("Dernières activités :", "Latest activity:"));
            foreach (var t in recents)
            {
                texte.AppendLine().Append(T(
                    $"• {Reference(t)} — mis à jour le {Date(t.DateDerniereActiviteUtc)}. /tickets/{t.Id}",
                    $"• {Reference(t)} — updated on {Date(t.DateDerniereActiviteUtc)}. /tickets/{t.Id}"));
            }
        }

        if (attendus > 0)
        {
            texte.AppendLine().Append(T(
                "Demandez-moi « qu'attend-on de moi ? » pour voir les dossiers à compléter.",
                "Ask me \"what do you need from me?\" to see the cases to complete."));
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
            return new ReponseAssistant(T(
                "Vous n'avez aucun rendez-vous à venir. Pour en réserver un, ouvrez « Mes rendez-vous ».",
                "You have no upcoming appointments. To book one, open \"My appointments\"."), false);
        }

        var premier = prochains[0];
        var date = premier.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        var texte = new StringBuilder().Append(T(
            $"Votre prochain rendez-vous au Billing Office : le {date} de {premier.HeureDebut:hh\\:mm} à {premier.HeureFin:hh\\:mm}, pour le dossier {premier.NumeroTicket}.",
            $"Your next appointment at the Billing Office: on {date} from {premier.HeureDebut:hh\\:mm} to {premier.HeureFin:hh\\:mm}, for case {premier.NumeroTicket}."));
        if (prochains.Count > 1)
        {
            texte.AppendLine().Append(T(
                $"Vous avez {prochains.Count - 1} autre(s) rendez-vous à venir dans « Mes rendez-vous ».",
                $"You have {prochains.Count - 1} more upcoming appointment(s) in \"My appointments\"."));
        }

        return new ReponseAssistant(texte.ToString(), false);
    }

    private static string DecrireTicket(Ticket t, IReadOnlyDictionary<string, string> libelles, IReadOnlyDictionary<Guid, string> filesParId)
    {
        var statut = libelles.TryGetValue(t.Statut.ToString(), out var l) ? l : t.Statut.ToString();
        var etape = filesParId.TryGetValue(t.FileCouranteId, out var nomFile)
            ? T($", à l'étape « {nomFile} »", $", at stage \"{nomFile}\"")
            : string.Empty;
        var suite = t.Statut switch
        {
            StatutTicket.EnAttenteClient => T(
                "CIT attend un complément de votre part : ouvrez le dossier pour répondre.",
                "CIT is waiting for additional information from you: open the case to reply."),
            StatutTicket.EnAttenteDePriseEnCharge => T(
                "Il sera pris en charge par un agent CIT. Aucune action n'est attendue de votre part.",
                "It will be handled by a CIT agent. No action is expected from you."),
            StatutTicket.EnCours => T(
                "Un agent CIT le traite. Aucune action n'est attendue de votre part.",
                "A CIT agent is handling it. No action is expected from you."),
            StatutTicket.Cloture => T("Ce dossier est terminé.", "This case is completed."),
            _ => T("Ce dossier a été annulé.", "This case has been cancelled."),
        };

        return T(
            $"{Reference(t)} : « {statut} »{etape}, dernière activité le {Date(t.DateDerniereActiviteUtc)}. {suite} Ouvrez le dossier : /tickets/{t.Id}",
            $"{Reference(t)}: \"{statut}\"{etape}, last activity on {Date(t.DateDerniereActiviteUtc)}. {suite} Open the case: /tickets/{t.Id}");
    }

    private async Task<IReadOnlyDictionary<string, string>> LibellesAsync(CancellationToken ct) =>
        (await libellesStatut.ListerAsync(ct).ConfigureAwait(false))
            .ToDictionary(l => l.Code, l => EnAnglais() ? l.LibelleEn : l.LibelleFr);

    private static bool Correspond(Ticket t, string numero, Perimetre perimetre) =>
        Egal(t.Numero, numero) || Egal(t.NumeroBl, numero) || Egal(t.NumeroBooking, numero)
        || (perimetre.NumerosDossier.TryGetValue(t.DossierId, out var dossier) && Egal(dossier, numero));

    private static bool Egal(string? a, string b) => a is not null && string.Equals(a.Trim(), b, StringComparison.OrdinalIgnoreCase);

    private static string Reference(Ticket t) =>
        t.NumeroBl is { Length: > 0 } bl ? T($"Dossier {t.Numero} (BL {bl})", $"Case {t.Numero} (BL {bl})")
        : t.NumeroBooking is { Length: > 0 } bk ? T($"Dossier {t.Numero} (booking {bk})", $"Case {t.Numero} (booking {bk})")
        : T($"Dossier {t.Numero}", $"Case {t.Numero}");

    /// <summary>Abidjan est à UTC+0 toute l'année, sans heure d'été : l'heure UTC stockée EST l'heure d'Abidjan.</summary>
    private static string Date(DateTime utc) =>
        utc.ToString(EnAnglais() ? "dd/MM/yyyy HH:mm" : "dd/MM/yyyy 'à' HH:mm", CultureInfo.InvariantCulture);

    private static void AjouterSuite(StringBuilder texte, int total)
    {
        if (total > NombreMaxListe)
        {
            texte.AppendLine().Append(T(
                $"… et {total - NombreMaxListe} autre(s), visibles dans « Mes demandes ».",
                $"… and {total - NombreMaxListe} more, visible in \"My requests\"."));
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

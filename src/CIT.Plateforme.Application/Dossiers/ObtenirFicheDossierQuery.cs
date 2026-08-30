using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Client;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Dossiers;

public sealed class ObtenirFicheDossierQuery(
    IDossierRepository dossierRepository,
    ITicketRepository ticketRepository,
    IFileDeTraitementRepository fileRepository,
    ISocieteClienteRepository societeRepository,
    IUtilisateurSocieteRepository utilisateurSocieteRepository,
    IUtilisateurInfoRepository utilisateurInfoRepository)
{
    public async Task<FicheDossierVue> ExecuterAsync(Guid appelantId, Guid dossierId, CancellationToken ct = default)
    {
        var dossier = await dossierRepository.ObtenirParIdAsync(dossierId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Dossier {dossierId} introuvable.");

        var societe = await societeRepository.ObtenirParIdAsync(dossier.ClientId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Société {dossier.ClientId} introuvable.");

        var appelantInfo = await utilisateurInfoRepository.ObtenirInfoAsync(appelantId, ct).ConfigureAwait(false);
        bool estClient = appelantInfo?.Nature == NatureUtilisateur.Client;

        // 1. Contrôle de cloisonnement (Principe VII)
        if (estClient)
        {
            var userSoc = await utilisateurSocieteRepository.ObtenirParUtilisateurAsync(appelantId, ct).ConfigureAwait(false);
            if (userSoc is null || !userSoc.EstActif || userSoc.SocieteId != dossier.ClientId)
            {
                throw new AccesRefuseException($"L'utilisateur client {appelantId} n'est pas autorisé à consulter le dossier {dossier.Numero}.");
            }

            if (userSoc.DroitConsultation == DroitConsultation.PropresDemandesUniquement && !userSoc.EstAdministrateur && dossier.AuteurId != appelantId)
            {
                throw new AccesRefuseException($"L'utilisateur {appelantId} ne peut consulter que ses propres dossiers.");
            }
        }

        // 2. Auteur info
        string auteurNom = "Auteur";
        if (dossier.AuteurId.HasValue)
        {
            var auteurInfo = await utilisateurInfoRepository.ObtenirInfoAsync(dossier.AuteurId.Value, ct).ConfigureAwait(false);
            auteurNom = auteurInfo?.NomAffiche ?? auteurInfo?.Email ?? "Auteur";
        }

        // 3. Tickets du dossier
        var tickets = await ticketRepository.ListerParDossierAsync(dossierId, ct).ConfigureAwait(false);

        var fileIds = tickets.Select(t => t.FileCouranteId).Distinct().ToList();
        var files = await fileRepository.ListerAsync(ct).ConfigureAwait(false);
        var filesMap = files.ToDictionary(f => f.Id, f => f.Nom);

        var opIds = tickets.Where(t => t.OperateurAssigneId.HasValue).Select(t => t.OperateurAssigneId!.Value).Distinct().ToList();
        var opsInfo = await utilisateurInfoRepository.ObtenirInfosAsync(opIds, ct).ConfigureAwait(false);

        var ticketsLignes = tickets.Select(t =>
        {
            filesMap.TryGetValue(t.FileCouranteId, out var fileNom);
            fileNom ??= "File générale";

            string? opNom = null;
            if (t.OperateurAssigneId.HasValue && opsInfo.TryGetValue(t.OperateurAssigneId.Value, out var op))
            {
                opNom = op.NomAffiche ?? op.Email;
            }

            return new TicketLigneDossierVue(
                TicketId: t.Id,
                NumeroTicket: t.Numero,
                NumeroBl: t.NumeroBl,
                Module: t.Module,
                TypeDemande: t.TypeDemande,
                Statut: t.Statut,
                FileNom: fileNom,
                OperateurAssigneNom: opNom,
                DateCreationUtc: t.DateCreationUtc,
                DateDerniereActiviteUtc: t.DateDerniereActiviteUtc
            );
        }).ToList();

        var statutGlobal = DeterminerStatutGlobal(tickets.ToList());

        var nomSociete = societe.InformationsDeclarees;
        if (nomSociete.Contains("RaisonSociale"))
        {
            nomSociete = ExtraireNom(nomSociete);
        }

        return new FicheDossierVue(
            DossierId: dossier.Id,
            NumeroDossier: dossier.Numero,
            SocieteId: societe.Id,
            SocieteNom: nomSociete,
            SocieteProfil: societe.Profil,
            AuteurId: dossier.AuteurId,
            AuteurNom: auteurNom,
            DateCreationUtc: dossier.DateCreationUtc,
            StatutGlobal: statutGlobal,
            Tickets: ticketsLignes,
            TotalTickets: ticketsLignes.Count,
            TotalTicketsEnCours: ticketsLignes.Count(t => t.Statut == StatutTicket.EnAttenteDePriseEnCharge || t.Statut == StatutTicket.EnCours || t.Statut == StatutTicket.EnAttenteClient),
            TotalTicketsClotures: ticketsLignes.Count(t => t.Statut == StatutTicket.Cloture)
        );
    }

    private static StatutVisuelDossier DeterminerStatutGlobal(List<Ticket> tickets)
    {
        if (tickets.Count == 0) return StatutVisuelDossier.EnAttentePriseEnCharge;
        if (tickets.All(t => t.Statut == StatutTicket.Cloture)) return StatutVisuelDossier.ClotureTermine;
        if (tickets.All(t => t.Statut == StatutTicket.Annule)) return StatutVisuelDossier.Annule;
        if (tickets.Any(t => t.Statut == StatutTicket.EnAttenteClient)) return StatutVisuelDossier.EnAttenteClient;
        if (tickets.Any(t => t.Statut == StatutTicket.EnCours)) return StatutVisuelDossier.EnCoursTraitement;
        return StatutVisuelDossier.EnAttentePriseEnCharge;
    }

    private static string ExtraireNom(string raw)
    {
        try
        {
            var parts = raw.Split(new[] { '\r', '\n', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (part.Contains("RaisonSociale", StringComparison.OrdinalIgnoreCase))
                {
                    var kv = part.Split(':');
                    if (kv.Length > 1) return kv[1].Trim().Trim('"', '{', '}', ' ');
                }
            }
        }
        catch { }
        return raw;
    }
}

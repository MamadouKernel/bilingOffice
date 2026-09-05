using System.Security.Claims;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tarification;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CIT.Plateforme.Web.Api;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapPlateformeApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/v1");

        // C13-04 : GET /api/v1/tickets/{numero}
        api.MapGet("/tickets/{numero}", async (
            string numero,
            ITicketRepository tickets,
            ControleAccesRessourceService controleAcces,
            IContexteUtilisateurWeb contexteWeb,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(numero))
            {
                return Results.BadRequest(new { message = "Le numéro de ticket est requis." });
            }

            var ticket = await tickets.ObtenirParNumeroAsync(numero, ct).ConfigureAwait(false);
            if (ticket is null)
            {
                return Results.NotFound(new { message = $"Ticket {numero} introuvable." });
            }

            var ctx = await contexteWeb.ObtenirContexteAsync(ct).ConfigureAwait(false);

            // CPT-04 / SEC-03 — décision déléguée au contrôle central, refus par défaut.
            // Ce point réimplémentait auparavant sa propre règle, qui n'était appliquée que si
            // l'appelant possédait une société : un compte sans société lisait tout (P0).
            if (!await controleAcces.PeutConsulterLeTicketAsync(ctx.UtilisateurId, ticket, ct).ConfigureAwait(false))
            {
                return Results.Forbid();
            }

            var dto = new TicketStatutDto(
                ticket.Numero,
                ticket.Module,
                ticket.TypeDemande,
                ticket.Statut.ToString(),
                ticket.DateCreationUtc,
                ticket.NumeroBl);

            return Results.Ok(dto);
        }).RequireAuthorization();

        // C13-05 : GET /api/v1/dossiers/{reference}
        api.MapGet("/dossiers/{reference}", async (
            string reference,
            IDossierRepository dossiers,
            ITicketRepository tickets,
            ControleAccesRessourceService controleAcces,
            IContexteUtilisateurWeb contexteWeb,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(reference) || !Guid.TryParse(reference, out var dossierId))
            {
                return Results.BadRequest(new { message = "L'identifiant du dossier est invalide." });
            }

            var dossier = await dossiers.ObtenirParIdAsync(dossierId, ct).ConfigureAwait(false);
            if (dossier is null)
            {
                return Results.NotFound(new { message = $"Dossier {reference} introuvable." });
            }

            var ctx = await contexteWeb.ObtenirContexteAsync(ct).ConfigureAwait(false);

            // CPT-04 / ENT-03 — même contrôle central que pour le ticket, refus par défaut.
            if (!await controleAcces.PeutConsulterLeDossierAsync(ctx.UtilisateurId, dossier, ct).ConfigureAwait(false))
            {
                return Results.Forbid();
            }

            var ticketEntities = await tickets.ListerParDossierAsync(dossier.Id, ct).ConfigureAwait(false);
            var ticketDtos = ticketEntities.Select(t => new TicketStatutDto(
                t.Numero,
                t.Module,
                t.TypeDemande,
                t.Statut.ToString(),
                t.DateCreationUtc,
                t.NumeroBl)).ToList();

            var dto = new DossierDetailDto(
                dossier.Id.ToString(),
                dossier.ClientId,
                dossier.DateCreationUtc,
                ticketDtos);

            return Results.Ok(dto);
        }).RequireAuthorization();

        // C13-06 : POST /api/v1/simulations/calculer (réutilisation directe du moteur F005)
        api.MapPost("/simulations/calculer", async (
            CalculerSimulationRequeteDto requete,
            CalculerSimulationProformaQueryHandler handler,
            IReferentielsTarifairesRepository referentielsRepository,
            CancellationToken ct) =>
        {
            if (requete.Conteneurs is null || requete.Conteneurs.Count == 0)
            {
                return Results.BadRequest(new { message = "Au moins un conteneur est requis." });
            }

            // Les référentiels sont devenus des tables (analyse Merise § 3.3) : l'API résout donc
            // les codes reçus au lieu de les convertir en énumérations, et refuse un code inconnu
            // plutôt que de retomber silencieusement sur une valeur par défaut — l'ancien code
            // traduisait toute taille non « 40 » en 20 pieds et toute catégorie absente en
            // « Standard », ce qui produisait un tarif pour une demande que le client n'avait pas
            // formulée.
            var referentiels = await referentielsRepository.ChargerAsync(ct).ConfigureAwait(false);

            var sens = referentiels.SensParCode(requete.Sens);
            if (sens is null)
            {
                return Results.BadRequest(new { message = $"Sens d'opération inconnu : {requete.Sens}" });
            }

            var conteneursDomain = new List<ConteneurSimulationRequete>(requete.Conteneurs.Count);
            foreach (var c in requete.Conteneurs)
            {
                var taille = referentiels.TailleParCode(c.Taille);
                if (taille is null)
                {
                    return Results.BadRequest(new { message = $"Taille de conteneur inconnue : {c.Taille}" });
                }

                var type = referentiels.TypeParCode(c.Type);
                if (type is null)
                {
                    return Results.BadRequest(new { message = $"Type de conteneur inconnu : {c.Type}" });
                }

                var categorie = referentiels.CategorieParCode(sens.Id, c.CategorieMarchandise ?? string.Empty);
                if (categorie is null)
                {
                    return Results.BadRequest(new
                    {
                        message = $"Catégorie de marchandise « {c.CategorieMarchandise} » inconnue pour le sens « {sens.Code} »."
                    });
                }

                conteneursDomain.Add(new ConteneurSimulationRequete(
                    "API-CTR",
                    taille.Id,
                    type.Id,
                    c.Poids,
                    c.EstDangereux,
                    c.EstHorsGabarit,
                    categorie.Id));
            }

            var proformaRequete = new CalculerSimulationProformaRequete(
                sens.Id,
                DateOnly.FromDateTime(requete.DateArrivee),
                DateOnly.FromDateTime(requete.DateEnlevement),
                conteneursDomain);

            var resultat = await handler.HandleAsync(proformaRequete, ct).ConfigureAwait(false);

            var lignesDto = resultat.Lignes.Select(l => new LigneSimulationDto(
                l.NumeroConteneur,
                $"{l.Taille.Libelle} {l.Type.Libelle}",
                l.TotalLigne)).ToList();

            var responseDto = new SimulationResultatDto(
                resultat.TotalGeneralXof,
                resultat.Lignes.Count,
                lignesDto);

            return Results.Ok(responseDto);
        }).RequireAuthorization();

        return app;
    }
}

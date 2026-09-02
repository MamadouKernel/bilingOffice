using System.Security.Claims;
using CIT.Plateforme.Application.Abstractions;
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
            IDossierRepository dossiers,
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
            var dossier = await dossiers.ObtenirParIdAsync(ticket.DossierId, ct).ConfigureAwait(false);

            // Vérification de sécurité CPT-04 / SEC-03
            if (ctx.Nature == NatureUtilisateur.Client && ctx.SocieteId.HasValue && dossier is not null &&
                ctx.SocieteId.Value != dossier.ClientId)
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

            // Vérification de sécurité CPT-04
            if (ctx.Nature == NatureUtilisateur.Client && ctx.SocieteId.HasValue &&
                ctx.SocieteId.Value != dossier.ClientId)
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
            CancellationToken ct) =>
        {
            if (requete.Conteneurs is null || requete.Conteneurs.Count == 0)
            {
                return Results.BadRequest(new { message = "Au moins un conteneur est requis." });
            }

            if (!Enum.TryParse<SensTrafic>(requete.Sens, ignoreCase: true, out var sens))
            {
                return Results.BadRequest(new { message = $"Sens de trafic invalide: {requete.Sens}" });
            }

            var conteneursDomain = requete.Conteneurs.Select(c =>
            {
                var taille = c.Taille == "40" ? TailleConteneur.QuarantePieds : TailleConteneur.VingtPieds;
                var type = c.Type.Equals("Reefer", StringComparison.OrdinalIgnoreCase) ? TypeConteneur.Reefer : TypeConteneur.Dry;
                return new ConteneurSimulationRequete(
                    "API-CTR",
                    taille,
                    type,
                    c.Poids,
                    c.EstDangereux,
                    c.EstHorsGabarit,
                    c.CategorieMarchandise ?? "Standard");
            }).ToList();

            var proformaRequete = new CalculerSimulationProformaRequete(
                sens,
                DateOnly.FromDateTime(requete.DateArrivee),
                DateOnly.FromDateTime(requete.DateEnlevement),
                conteneursDomain);

            var resultat = await handler.HandleAsync(proformaRequete, ct).ConfigureAwait(false);

            var lignesDto = resultat.Lignes.Select(l => new LigneSimulationDto(
                l.NumeroConteneur,
                $"{l.Taille} {l.Type}",
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

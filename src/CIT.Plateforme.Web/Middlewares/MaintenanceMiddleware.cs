using CIT.Plateforme.Application.Supervision;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace CIT.Plateforme.Web.Middlewares;

public class MaintenanceMiddleware
{
    private readonly RequestDelegate _next;

    public MaintenanceMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, GestionnaireMaintenanceService maintenanceService)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Ne jamais bloquer les sondes de santé et assets statiques
        if (path.StartsWith("/healthz", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/ready", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/js", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (maintenanceService.EstEnMaintenance)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.Headers.Append("Retry-After", "3600");
            context.Response.ContentType = "text/html; charset=utf-8";
            var message = maintenanceService.Motif ?? "La plateforme CIT fait l'objet d'une opération de maintenance technique planifiée.";
            var html = $@"<!DOCTYPE html>
<html lang=""fr"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>Maintenance Technique — Côte d'Ivoire Terminal</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; }}
        body {{ background: #0f172a; color: #f8fafc; min-height: 100vh; display: flex; align-items: center; justify-content: center; padding: 1.5rem; }}
        .card {{ background: #1e293b; border: 1px solid #334155; border-radius: 1.5rem; max-width: 580px; width: 100%; padding: 2.5rem; text-align: center; box-shadow: 0 25px 50px -12px rgba(0,0,0,0.5); position: relative; overflow: hidden; }}
        .badge {{ display: inline-flex; align-items: center; gap: 0.5rem; background: rgba(245, 158, 11, 0.15); color: #fbbf24; border: 1px solid rgba(245, 158, 11, 0.3); padding: 0.35rem 1rem; border-radius: 9999px; font-size: 0.85rem; font-weight: 600; margin-bottom: 1.5rem; }}
        .icon-box {{ width: 80px; height: 80px; background: rgba(245, 158, 11, 0.1); border: 2px solid #f59e0b; border-radius: 50%; display: inline-flex; align-items: center; justify-content: center; font-size: 2.5rem; margin-bottom: 1.5rem; }}
        h1 {{ font-size: 1.6rem; font-weight: 700; margin-bottom: 0.75rem; color: #ffffff; }}
        p {{ color: #94a3b8; font-size: 0.95rem; line-height: 1.6; margin-bottom: 1.75rem; }}
        .info-box {{ background: #0f172a; border: 1px solid #334155; border-radius: 0.75rem; padding: 1rem; text-align: left; margin-bottom: 1.5rem; }}
        .info-title {{ font-size: 0.8rem; color: #64748b; font-weight: 600; text-transform: uppercase; letter-spacing: 0.05em; margin-bottom: 0.25rem; }}
        .info-text {{ font-size: 0.9rem; color: #cbd5e1; }}
        .footer-note {{ font-size: 0.8rem; color: #64748b; }}
    </style>
</head>
<body>
    <div class=""card"">
        <div class=""icon-box"">🛠️</div>
        <div class=""badge"">⏱️ Statut 503 • Interruption Programmée</div>
        <h1>Maintenance en cours</h1>
        <p>Notre équipe technique procède à des optimisations d'infrastructure pour garantir la haute disponibilité et la sécurité de vos opérations portuaires.</p>
        <div class=""info-box"">
            <div class=""info-title"">Motif de l'opération :</div>
            <div class=""info-text"">{message}</div>
        </div>
        <div class=""footer-note"">Côte d'Ivoire Terminal (CIT) • Espace Digital Billing Office</div>
    </div>
</body>
</html>";
            await context.Response.WriteAsync(html);
            return;
        }

        await _next(context);
    }
}

public static class MaintenanceMiddlewareExtensions
{
    public static IApplicationBuilder UseMaintenanceMode(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<MaintenanceMiddleware>();
    }
}

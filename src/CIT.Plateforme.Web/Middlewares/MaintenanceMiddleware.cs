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
            var message = maintenanceService.Motif ?? "La plateforme CIT est actuellement en maintenance planifiée.";
            await context.Response.WriteAsync($"<!DOCTYPE html><html><head><title>Maintenance</title></head><body><h1>Maintenance en cours</h1><p>{message}</p></body></html>");
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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace CIT.Plateforme.Web.Middlewares;

/// <summary>
/// Middleware de durcissement des en-têtes HTTP (SEC-04).
/// Injecte les protections contre le MIME-sniffing, le clickjacking, l'exposition de référent,
/// et la politique CSP compatible Blazor Server.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Prévention du MIME-sniffing
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // 2. Prévention du clickjacking (encapsulation en iframe)
        context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";

        // 3. Protection du Referrer
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // 4. Content Security Policy (progressive et compatible Blazor Server & SSR)
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "img-src 'self' data:; " +
            "style-src 'self' 'unsafe-inline'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "connect-src 'self' ws: wss:; " +
            "font-src 'self'; " +
            "frame-ancestors 'self';";

        await next(context).ConfigureAwait(false);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}

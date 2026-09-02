using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Web.Middlewares;

public class MetrologiePerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MetrologiePerformanceMiddleware> _logger;

    public MetrologiePerformanceMiddleware(RequestDelegate next, ILogger<MetrologiePerformanceMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var debut = Stopwatch.GetTimestamp();

        context.Response.OnStarting(() =>
        {
            var fin = Stopwatch.GetTimestamp();
            var dureeMs = Stopwatch.GetElapsedTime(debut, fin).TotalMilliseconds;

            if (!context.Response.Headers.ContainsKey("Server-Timing"))
            {
                context.Response.Headers.Append("Server-Timing", $"total;dur={dureeMs:F2}");
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}

public static class MetrologiePerformanceMiddlewareExtensions
{
    public static IApplicationBuilder UseMetrologiePerformance(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<MetrologiePerformanceMiddleware>();
    }
}

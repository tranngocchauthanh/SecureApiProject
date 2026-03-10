using System.Collections.Concurrent;

namespace SecureApiProject.Middleware;

/// <summary>
/// Activity 3: Simple in-memory rate limiter.
/// Prevents brute-force attacks on auth endpoints.
/// Limits: 10 requests/minute per IP on /api/auth routes.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    // Thread-safe dictionary: IP -> (request count, window start)
    private static readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _requestCounts = new();

    private const int MaxRequests = 10;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only rate-limit auth endpoints
        if (!context.Request.Path.StartsWithSegments("/api/auth"))
        {
            await _next(context);
            return;
        }

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTime.UtcNow;

        var entry = _requestCounts.AddOrUpdate(ip,
            addValueFactory: _ => (1, now),
            updateValueFactory: (_, existing) =>
            {
                if (now - existing.WindowStart > Window)
                    return (1, now); // Reset window
                return (existing.Count + 1, existing.WindowStart);
            });

        if (entry.Count > MaxRequests)
        {
            _logger.LogWarning("Rate limit exceeded for IP: {IP}", ip);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = "60";
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(
                """{"success":false,"message":"Too many requests. Please try again in 1 minute."}""");
            return;
        }

        await _next(context);
    }
}

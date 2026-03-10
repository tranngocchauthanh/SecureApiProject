namespace SecureApiProject.Middleware;

/// <summary>
/// Activity 3: Logs all incoming requests for security audit trail.
/// Detects and logs suspicious patterns (e.g., SQL injection attempts).
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    // Patterns that may indicate attack attempts
    private static readonly string[] SuspiciousPatterns =
    [
        "' OR ", "DROP TABLE", "UNION SELECT", "<script>", "javascript:",
        "../", "..\\", "%2e%2e", "eval(", "exec("
    ];

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var path = request.Path.Value ?? "";
        var queryString = request.QueryString.Value ?? "";

        // Check for suspicious patterns in the URL
        var fullUrl = path + queryString;
        if (ContainsSuspiciousPattern(fullUrl))
        {
            _logger.LogWarning(
                "⚠️  Suspicious request detected | IP: {IP} | Method: {Method} | Path: {Path}",
                ipAddress, request.Method, fullUrl);
        }
        else
        {
            _logger.LogInformation(
                "→ {Method} {Path} | IP: {IP} | User: {User}",
                request.Method, path, ipAddress,
                context.User.Identity?.Name ?? "anonymous");
        }

        var startTime = DateTime.UtcNow;

        await _next(context);

        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

        _logger.LogInformation(
            "← {Method} {Path} | {StatusCode} | {Elapsed}ms",
            request.Method, path, context.Response.StatusCode, elapsed);
    }

    private static bool ContainsSuspiciousPattern(string input)
    {
        var upper = input.ToUpperInvariant();
        return SuspiciousPatterns.Any(p => upper.Contains(p.ToUpperInvariant()));
    }
}

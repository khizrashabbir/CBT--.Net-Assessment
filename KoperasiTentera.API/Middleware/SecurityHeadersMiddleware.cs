namespace KoperasiTentera.API.Middleware;

/// <summary>
/// Adds baseline security response headers (OWASP secure headers project) that are
/// cheap, universally safe for a JSON API, and commonly checked by scanners like ZAP.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            HttpResponse headers = context.Response;
            headers.Headers["X-Content-Type-Options"] = "nosniff";
            headers.Headers["X-Frame-Options"] = "DENY";
            headers.Headers["Referrer-Policy"] = "no-referrer";
            headers.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; frame-ancestors 'none'";
            headers.Headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";
            return Task.CompletedTask;
        });

        return _next(context);
    }
}

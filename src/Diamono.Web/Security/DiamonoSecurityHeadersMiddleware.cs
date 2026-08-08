namespace Diamono.Web.Security;

public sealed class DiamonoSecurityHeadersMiddleware
{
    private readonly RequestDelegate next;

    public DiamonoSecurityHeadersMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers.TryAdd("X-Content-Type-Options", "nosniff");
        headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
        headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=(), payment=()");
        headers.TryAdd("X-Frame-Options", "DENY");
        headers.TryAdd(
            "Content-Security-Policy",
            "default-src 'self'; " +
            "base-uri 'self'; " +
            "object-src 'none'; " +
            "frame-ancestors 'none'; " +
            "form-action 'self'; " +
            "img-src 'self' data:; " +
            "font-src 'self' data:; " +
            "style-src 'self' 'unsafe-inline'; " +
            "script-src 'self' 'unsafe-inline'; " +
            "connect-src 'self' ws: wss:");

        return next(context);
    }
}

public static class DiamonoSecurityHeadersExtensions
{
    public static IApplicationBuilder UseDiamonoSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<DiamonoSecurityHeadersMiddleware>();
}

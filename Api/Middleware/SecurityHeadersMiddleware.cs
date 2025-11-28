using Microsoft.Extensions.Logging;

namespace Api.Middleware
{
    /// <summary>
    /// Middleware que agrega headers de seguridad a todas las respuestas HTTP
    /// para proteger contra ataques comunes como XSS, Clickjacking, etc.
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityHeadersMiddleware> _logger;

        public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Agregar headers de seguridad antes de procesar la request
            AddSecurityHeaders(context);

            await _next(context);
        }

        private void AddSecurityHeaders(HttpContext context)
        {
            var response = context.Response;
            var request = context.Request;

            try
            {
                // X-Content-Type-Options: Previene MIME type sniffing
                if (!response.Headers.ContainsKey("X-Content-Type-Options"))
                {
                    response.Headers.Append("X-Content-Type-Options", "nosniff");
                }

                // X-Frame-Options: Previene clickjacking attacks
                if (!response.Headers.ContainsKey("X-Frame-Options"))
                {
                    response.Headers.Append("X-Frame-Options", "DENY");
                }

                // X-XSS-Protection: Habilita filtro XSS del navegador
                if (!response.Headers.ContainsKey("X-XSS-Protection"))
                {
                    response.Headers.Append("X-XSS-Protection", "1; mode=block");
                }

                // Referrer-Policy: Controla información enviada en header Referer
                if (!response.Headers.ContainsKey("Referrer-Policy"))
                {
                    response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
                }

                // Content-Security-Policy: Previene XSS y code injection
                if (!response.Headers.ContainsKey("Content-Security-Policy"))
                {
                    var csp = "default-src 'self'; " +
                             "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                             "style-src 'self' 'unsafe-inline'; " +
                             "img-src 'self' data: https:; " +
                             "font-src 'self'; " +
                             "connect-src 'self'; " +
                             "frame-ancestors 'none';";
                    
                    response.Headers.Append("Content-Security-Policy", csp);
                }

                // Permissions-Policy: Controla APIs del navegador
                if (!response.Headers.ContainsKey("Permissions-Policy"))
                {
                    var permissionsPolicy = "camera=(), microphone=(), geolocation=(), payment=()";
                    response.Headers.Append("Permissions-Policy", permissionsPolicy);
                }

                // HSTS (HTTP Strict Transport Security) - Solo en HTTPS
                if (request.IsHttps && !response.Headers.ContainsKey("Strict-Transport-Security"))
                {
                    response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
                }

                // Cache-Control para recursos sensibles (APIs)
                if (request.Path.StartsWithSegments("/api") && !response.Headers.ContainsKey("Cache-Control"))
                {
                    response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate, private");
                    response.Headers.Append("Pragma", "no-cache");
                }

                _logger.LogDebug("Security headers added successfully for {Path}", request.Path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to add security headers for {Path}", request.Path);
                // No fallar la request si hay error agregando headers
            }
        }
    }
}
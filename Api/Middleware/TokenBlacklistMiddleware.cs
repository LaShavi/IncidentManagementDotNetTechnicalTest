using Application.Ports;
using Application.Helpers;

namespace Api.Middleware
{
    /// <summary>
    /// Middleware que valida si un access token está en blacklist
    /// Se ejecuta antes de la validación JWT estándar
    /// </summary>
    public class TokenBlacklistMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenBlacklistMiddleware> _logger;

        public TokenBlacklistMiddleware(RequestDelegate next, ILogger<TokenBlacklistMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITokenBlacklistRepository tokenBlacklistRepository)
        {
            var authHeader = context.Request.Headers["Authorization"].ToString();

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Replace("Bearer ", "");

                try
                {
                    var tokenHash = TokenHashHelper.HashToken(token);

                    if (await tokenBlacklistRepository.IsTokenBlacklistedAsync(tokenHash))
                    {
                        _logger.LogWarning("Blacklisted access token attempted from IP: {IpAddress}",
                            context.Connection.RemoteIpAddress);

                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";

                        await context.Response.WriteAsJsonAsync(new
                        {
                            statusCode = 401,
                            success = false,
                            message = "Token has been revoked",
                            error = "UnauthorizedAccess"
                        });
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating token blacklist");
                }
            }

            await _next(context);
        }
    }
}
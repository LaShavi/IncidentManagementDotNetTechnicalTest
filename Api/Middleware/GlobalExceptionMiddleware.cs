using Api.Helpers;
using Application.Helpers;
using System.Text.Json;

namespace Api.Middleware
{
    /// <summary>
    /// Global exception handling middleware that catches unhandled exceptions
    /// and returns standardized API responses without exposing sensitive information.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var requestId = context.TraceIdentifier;
                var userAgent = context.Request.Headers["User-Agent"].ToString();
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var method = context.Request.Method;
                var path = context.Request.Path;

                // Log completo para debugging interno (incluye información sensible)
                _logger.LogError(ex, 
                    "Unhandled exception occurred. RequestId: {RequestId}, IP: {IpAddress}, UserAgent: {UserAgent}, Method: {Method}, Path: {Path}",
                    requestId, ipAddress, userAgent, method, path);

                // Log de seguridad si es un error potencialmente malicioso
                LogSecurityEvent(ex, context);

                await HandleExceptionAsync(context, ex, requestId);
            }
        }

        private void LogSecurityEvent(Exception exception, HttpContext context)
        {
            var isSuspicious = false;
            var securityEventType = "Unknown";

            // Detectar tipos de errores que pueden indicar ataques
            switch (exception)
            {
                case UnauthorizedAccessException:
                    securityEventType = "UnauthorizedAccess";
                    isSuspicious = true;
                    break;
                case ArgumentException when exception.Message.Contains("token", StringComparison.OrdinalIgnoreCase):
                    securityEventType = "InvalidToken";
                    isSuspicious = true;
                    break;
                case FormatException when context.Request.Path.StartsWithSegments("/api"):
                    securityEventType = "MalformedRequest";
                    isSuspicious = true;
                    break;
                case OverflowException:
                    securityEventType = "DataOverflow";
                    isSuspicious = true;
                    break;
            }

            if (isSuspicious)
            {
                _logger.LogWarning(
                    "SECURITY EVENT: {EventType} from IP {IpAddress} on {Path} - RequestId: {RequestId}",
                    securityEventType,
                    context.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    context.Request.Path,
                    context.TraceIdentifier);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception, string requestId)
        {
            context.Response.ContentType = "application/json";

            var response = exception switch
            {
                ArgumentNullException => ApiResponseHelper.BadRequest(
                    ResourceTextHelper.Get("InvalidRequest"), 
                    GetSafeErrorDetails(exception, requestId)),
                
                ArgumentException => ApiResponseHelper.BadRequest(
                    ResourceTextHelper.Get("InvalidRequestParameters"), 
                    GetSafeErrorDetails(exception, requestId)),
                
                UnauthorizedAccessException => ApiResponseHelper.Unauthorized(
                    ResourceTextHelper.Get("UnauthorizedAccess")),
                
                KeyNotFoundException => ApiResponseHelper.NotFound(
                    ResourceTextHelper.Get("ResourceNotFound")),
                
                InvalidOperationException => ApiResponseHelper.BadRequest(
                    ResourceTextHelper.Get("InvalidOperation"), 
                    GetSafeErrorDetails(exception, requestId)),
                
                TimeoutException => ApiResponseHelper.InternalServerError(
                    ResourceTextHelper.Get("TimeoutExceeded"), 
                    GetSafeErrorDetails(exception, requestId)),

                TaskCanceledException => ApiResponseHelper.InternalServerError(
                    ResourceTextHelper.Get("OperationCanceled"),
                    GetSafeErrorDetails(exception, requestId)),

                NotSupportedException => ApiResponseHelper.BadRequest(
                    ResourceTextHelper.Get("OperationNotSupported"),
                    GetSafeErrorDetails(exception, requestId)),

                // Errores de validación
                ValidationException => ApiResponseHelper.BadRequest(
                    exception.Message,
                    GetSafeErrorDetails(exception, requestId)),
                
                _ => ApiResponseHelper.InternalServerError(
                    ResourceTextHelper.Get("InternalServerError"), 
                    GetSafeErrorDetails(exception, requestId))
            };

            context.Response.StatusCode = response.StatusCode;

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false // No indentado en producción por eficiencia
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        private object? GetSafeErrorDetails(Exception exception, string requestId)
        {
            // En desarrollo, mostrar información detallada para debugging
            if (_environment.IsDevelopment())
            {
                return new
                {
                    requestId = requestId,
                    type = exception.GetType().Name,
                    message = exception.Message,
                    // No incluir stack trace por defecto, solo en logs
                    timestamp = DateTime.UtcNow,
                    environment = "Development"
                };
            }

            // En producción, solo información mínima y segura
            return new
            {
                requestId = requestId,
                timestamp = DateTime.UtcNow,
                // ID único corto para correlacionar con logs internos
                reference = GenerateErrorReference(),
                support = ResourceTextHelper.Get("SupportReference")
            };
        }

        private string GenerateErrorReference()
        {
            // Generar un ID único de 8 caracteres para referencia
            return Guid.NewGuid().ToString("N")[..8].ToUpper();
        }
    }

    /// <summary>
    /// Excepción personalizada para errores de validación
    /// </summary>
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
        public ValidationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
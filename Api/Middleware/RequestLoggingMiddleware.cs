using System.Diagnostics;
using System.Text;

namespace Api.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var request = context.Request;

            _logger.LogInformation(
                "Starting request {Method} {Path} from {RemoteIp}",
                request.Method,
                request.Path,
                context.Connection.RemoteIpAddress);

            await _next(context);

            stopwatch.Stop();
            
            _logger.LogInformation(
                "Completed request {Method} {Path} - Status: {StatusCode} - Duration: {Duration}ms",
                request.Method,
                request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
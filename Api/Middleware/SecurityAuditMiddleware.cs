using System.Security.Claims;

namespace Api.Middleware
{
    /// <summary>
    /// Middleware especializado para registrar eventos de seguridad y detectar patrones sospechosos
    /// </summary>
    public class SecurityAuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityAuditMiddleware> _logger;

        // Cache en memoria para tracking básico (en producción usar Redis)
        private static readonly Dictionary<string, List<DateTime>> _requestTracker = new();
        private static readonly object _lockObject = new object();

        public SecurityAuditMiddleware(RequestDelegate next, ILogger<SecurityAuditMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var startTime = DateTime.UtcNow;
            var ipAddress = GetClientIpAddress(context);
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            var path = context.Request.Path;
            var method = context.Request.Method;

            // Detectar patrones sospechosos antes de procesar
            var suspiciousActivity = DetectSuspiciousActivity(context, ipAddress);
            
            if (suspiciousActivity.IsSuspicious)
            {
                _logger.LogWarning(
                    "SUSPICIOUS ACTIVITY DETECTED: {Activity} from IP {IpAddress} on {Method} {Path} - Reason: {Reason}",
                    suspiciousActivity.ActivityType, ipAddress, method, path, suspiciousActivity.Reason);
            }

            await _next(context);

            var duration = DateTime.UtcNow - startTime;
            var statusCode = context.Response.StatusCode;

            // Auditar eventos específicos de seguridad
            AuditSecurityEvents(context, ipAddress, userAgent, duration, statusCode);
        }

        private SuspiciousActivityResult DetectSuspiciousActivity(HttpContext context, string ipAddress)
        {
            var result = new SuspiciousActivityResult();
            
            // Detectar exceso de requests por IP
            if (IsExcessiveRequests(ipAddress))
            {
                result.IsSuspicious = true;
                result.ActivityType = "ExcessiveRequests";
                result.Reason = "Too many requests from single IP";
            }

            // Detectar intentos de acceso a rutas administrativas
            if (context.Request.Path.StartsWithSegments("/admin") ||
                context.Request.Path.StartsWithSegments("/api/admin"))
            {
                result.IsSuspicious = true;
                result.ActivityType = "AdminPathAccess";
                result.Reason = "Access attempt to admin paths";
            }

            // Detectar User-Agents sospechosos
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            if (IsSuspiciousUserAgent(userAgent))
            {
                result.IsSuspicious = true;
                result.ActivityType = "SuspiciousUserAgent";
                result.Reason = "Potentially malicious user agent";
            }

            // Detectar parámetros de query sospechosos
            if (HasSuspiciousQueryParameters(context.Request.QueryString.Value))
            {
                result.IsSuspicious = true;
                result.ActivityType = "SuspiciousQueryParams";
                result.Reason = "Query parameters contain suspicious patterns";
            }

            return result;
        }

        /// <summary>
        /// Audita eventos de seguridad importantes como intentos de login, accesos no autorizados, etc.
        /// 
        /// Este método es sincrónico porque solo realiza logging, que es una operación sincrónica.
        /// No requiere operaciones I/O asincrónicas como llamadas a BD o APIs externas.
        /// 
        /// Los eventos auditados incluyen:
        /// - Intentos de login (exitosos y fallidos)
        /// - Accesos no autorizados (401, 403)
        /// - Errores 4xx que pueden indicar ataques (inyección SQL, XSS, etc.)
        /// - Requests extremadamente lentos (posible ataque DoS)
        /// </summary>
        private void AuditSecurityEvents(HttpContext context, string ipAddress, string userAgent, 
            TimeSpan duration, int statusCode)
        {
            var userId = GetUserId(context);
            var path = context.Request.Path;
            var method = context.Request.Method;

            // 🔐 Auditar login attempts
            // Importante: Registrar todos los intentos de login (exitosos y fallidos) 
            // para detectar ataques de fuerza bruta
            if (path.StartsWithSegments("/api/auth/login"))
            {
                var eventType = statusCode == 200 ? "LoginSuccess" : "LoginFailure";
                _logger.LogInformation(
                    "SECURITY AUDIT: {EventType} - IP: {IpAddress}, UserAgent: {UserAgent}, Duration: {Duration}ms, UserId: {UserId}",
                    eventType, ipAddress, userAgent, duration.TotalMilliseconds, userId ?? "Unknown");
            }

            // 🚫 Auditar accesos no autorizados
            // Importante: Registrar intentos de acceso sin permisos (401, 403)
            // para detectar intentos de acceso a áreas protegidas
            if (statusCode == 401 || statusCode == 403)
            {
                _logger.LogWarning(
                    "SECURITY AUDIT: UnauthorizedAccess - IP: {IpAddress}, Path: {Path}, Method: {Method}, StatusCode: {StatusCode}, UserId: {UserId}",
                    ipAddress, path, method, statusCode, userId ?? "Anonymous");
            }

            // ⚠️ Auditar errores 4xx que pueden indicar ataques
            // Importante: Muchos ataques (inyección SQL, XSS, path traversal) generan 4xx
            // Excluir 404 porque son muy comunes y menos relevantes
            if (statusCode >= 400 && statusCode < 500 && statusCode != 404)
            {
                _logger.LogInformation(
                    "SECURITY AUDIT: ClientError - IP: {IpAddress}, Path: {Path}, Method: {Method}, StatusCode: {StatusCode}, Duration: {Duration}ms",
                    ipAddress, path, method, statusCode, duration.TotalMilliseconds);
            }

            // 🐌 Auditar requests extremadamente lentos
            // Importante: Las solicitudes muy lentas pueden indicar:
            // - Ataques DoS (intentos de agotar recursos)
            // - Queries SQL ineficientes
            // - Problemas de rendimiento en la aplicación
            if (duration.TotalSeconds > 30)
            {
                _logger.LogWarning(
                    "SECURITY AUDIT: SlowRequest - IP: {IpAddress}, Path: {Path}, Duration: {Duration}s - Possible DoS attempt",
                    ipAddress, path, duration.TotalSeconds);
            }
        }

        private bool IsExcessiveRequests(string ipAddress)
        {
            lock (_lockObject)
            {
                var now = DateTime.UtcNow;
                var oneMinuteAgo = now.AddMinutes(-1);

                if (!_requestTracker.ContainsKey(ipAddress))
                {
                    _requestTracker[ipAddress] = new List<DateTime>();
                }

                var requests = _requestTracker[ipAddress];
                
                // Limpiar requests antiguos
                requests.RemoveAll(r => r < oneMinuteAgo);
                
                // Agregar request actual
                requests.Add(now);

                // Considerar excesivo si más de 60 requests por minuto desde la misma IP
                return requests.Count > 60;
            }
        }

        private bool IsSuspiciousUserAgent(string userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
                return true;

            var suspiciousPatterns = new[]
            {
                "sqlmap", "nmap", "nikto", "nessus", "burp", "zap", 
                "python-requests", "curl", "wget", "bot", "crawler",
                "spider", "scraper", "<script>", "javascript:", "eval)("
            };

            return suspiciousPatterns.Any(pattern => 
                userAgent.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        private bool HasSuspiciousQueryParameters(string? queryString)
        {
            if (string.IsNullOrWhiteSpace(queryString))
                return false;

            var suspiciousPatterns = new[]
            {
                "script", "javascript:", "vbscript:", "onload=", "onerror=",
                "eval(", "alert(", "confirm(", "prompt(", "<iframe",
                "union select", "drop table", "insert into", "update set",
                "'or'1'='1", "admin'--", "' or 1=1", "../", "..\\",
                "cmd=", "exec=", "system=", "shell=", "/etc/passwd",
                "boot.ini", "win.ini"
            };

            return suspiciousPatterns.Any(pattern =>
                queryString.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        private string GetClientIpAddress(HttpContext context)
        {
            // Intentar obtener la IP real considerando proxies y load balancers
            var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xForwardedFor))
            {
                return xForwardedFor.Split(',')[0].Trim();
            }

            var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xRealIp))
            {
                return xRealIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private string? GetUserId(HttpContext context)
        {
            return context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private class SuspiciousActivityResult
        {
            public bool IsSuspicious { get; set; }
            public string ActivityType { get; set; } = string.Empty;
            public string Reason { get; set; } = string.Empty;
        }
    }
}
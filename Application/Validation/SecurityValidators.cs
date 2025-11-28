using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Application.Helpers;

namespace Application.Validation
{
    /// <summary>
    /// Validador personalizado para emails con verificación robusta
    /// </summary>
    public class SecureEmailAttribute : ValidationAttribute
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public override bool IsValid(object? value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return false;

            var email = value.ToString()!.Trim();

            // Validar longitud
            if (email.Length > 254) // RFC 5321 límite
                return false;

            // Validar formato básico
            if (!EmailRegex.IsMatch(email))
                return false;

            // Validar que no contenga caracteres peligrosos
            if (ContainsDangerousCharacters(email))
                return false;

            // Validar dominios sospechosos básicos
            var domain = email.Split('@')[1].ToLower();
            if (IsSuspiciousDomain(domain))
                return false;

            return true;
        }

        private bool ContainsDangerousCharacters(string email)
        {
            // Caracteres que pueden indicar intentos de inyección
            var dangerousChars = new[] { "<", ">", "\"", "'", "&", "\\", "/", "?", "#" };
            return dangerousChars.Any(email.Contains);
        }

        private bool IsSuspiciousDomain(string domain)
        {
            // Lista básica de dominios temporales conocidos
            var suspiciousDomains = new[]
            {
                "10minutemail.com", "guerrillamail.com", "mailinator.com",
                "tempmail.org", "temp-mail.org", "throwawaymails.com"
            };
            
            return suspiciousDomains.Any(suspicious => domain.EndsWith(suspicious));
        }

        public override string FormatErrorMessage(string name)
        {
            return ResourceTextHelper.Get("EmailInvalid") ?? $"El campo {name} debe ser una dirección de email válida y segura.";
        }
    }

    /// <summary>
    /// Validador personalizado para nombres de usuario seguros
    /// </summary>
    public class SecureUsernameAttribute : ValidationAttribute
    {
        private static readonly Regex UsernameRegex = new Regex(
            @"^[a-zA-Z0-9]([a-zA-Z0-9._-]*[a-zA-Z0-9])?$",
            RegexOptions.Compiled);

        public override bool IsValid(object? value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return false;

            var username = value.ToString()!.Trim();

            // Validar longitud
            if (username.Length < 3 || username.Length > 30)
                return false;

            // Validar formato
            if (!UsernameRegex.IsMatch(username))
                return false;

            // Validar que no sea solo números
            if (username.All(char.IsDigit))
                return false;

            // Validar que no contenga palabras reservadas
            if (IsReservedUsername(username))
                return false;

            return true;
        }

        private bool IsReservedUsername(string username)
        {
            var reserved = new[]
            {
                "admin", "administrator", "root", "system", "api", "www", "mail",
                "email", "support", "help", "info", "contact", "service", "user",
                "guest", "anonymous", "null", "undefined", "test", "demo"
            };
            
            return reserved.Any(r => string.Equals(r, username, StringComparison.OrdinalIgnoreCase));
        }

        public override string FormatErrorMessage(string name)
        {
            return ResourceTextHelper.Get("UsernameInvalid") ?? $"El {name} debe tener entre 3-30 caracteres, comenzar y terminar con letra o número, y no ser una palabra reservada.";
        }
    }

    /// <summary>
    /// Validador personalizado para nombres personales
    /// </summary>
    public class PersonalNameAttribute : ValidationAttribute
    {
        private static readonly Regex NameRegex = new Regex(
            @"^[a-zA-ZÀ-ÿ\u00f1\u00d1\s'-]+$",
            RegexOptions.Compiled);

        public override bool IsValid(object? value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return false;

            var name = value.ToString()!.Trim();

            // Validar longitud
            if (name.Length < 2 || name.Length > 50)
                return false;

            // Validar formato (letras, espacios, acentos, apostrofes, guiones)
            if (!NameRegex.IsMatch(name))
                return false;

            // Validar que no contenga múltiples espacios consecutivos
            if (name.Contains("  "))
                return false;

            // Validar que no empiece o termine con espacio, apostrofe o guión
            if (name.StartsWith(" ") || name.EndsWith(" ") || 
                name.StartsWith("'") || name.EndsWith("'") ||
                name.StartsWith("-") || name.EndsWith("-"))
                return false;

            return true;
        }

        public override string FormatErrorMessage(string name)
        {
            return ResourceTextHelper.Get("NameInvalid") ?? $"El {name} debe contener solo letras, espacios, acentos, apostrofes y guiones, con 2-50 caracteres.";
        }
    }

    /// <summary>
    /// Validador para tokens de seguridad
    /// </summary>
    public class SecureTokenAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return false;

            var token = value.ToString()!.Trim();

            // Validar longitud mínima para tokens de seguridad
            if (token.Length < 20)
                return false;

            // Validar que sea base64 válido o hexadecimal
            if (!IsValidBase64(token) && !IsValidHexadecimal(token))
                return false;

            return true;
        }

        private bool IsValidBase64(string token)
        {
            try
            {
                Convert.FromBase64String(token);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidHexadecimal(string token)
        {
            return token.All(c => char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
        }

        public override string FormatErrorMessage(string name)
        {
            return ResourceTextHelper.Get("TokenInvalid") ?? $"El {name} debe ser un token válido en formato base64 o hexadecimal.";
        }
    }
}
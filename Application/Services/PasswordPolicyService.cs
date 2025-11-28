using System.Text.RegularExpressions;

namespace Application.Services
{
    /// <summary>
    /// Servicio para validar políticas de contraseñas y seguridad
    /// </summary>
    public interface IPasswordPolicyService
    {
        /// <summary>
        /// Valida si una contraseña cumple con las políticas de seguridad
        /// </summary>
        /// <param name="password">Contraseña a validar</param>
        /// <returns>Resultado de la validación con detalles</returns>
        PasswordValidationResult ValidatePassword(string password);

        /// <summary>
        /// Verifica si una contraseña es común o está en listas de contraseñas filtradas
        /// </summary>
        /// <param name="password">Contraseña a verificar</param>
        /// <returns>True si la contraseña es segura</returns>
        bool IsPasswordSafe(string password);
    }

    /// <summary>
    /// Resultado de la validación de contraseñas
    /// </summary>
    public class PasswordValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public int Score { get; set; } // 0-100, donde 100 es muy fuerte
        public PasswordStrength Strength { get; set; }
    }

    /// <summary>
    /// Niveles de fortaleza de contraseña
    /// </summary>
    public enum PasswordStrength
    {
        VeryWeak = 0,
        Weak = 1,
        Fair = 2,
        Good = 3,
        Strong = 4,
        VeryStrong = 5
    }

    /// <summary>
    /// Implementación del servicio de políticas de contraseña
    /// </summary>
    public class PasswordPolicyService : IPasswordPolicyService
    {
        private readonly HashSet<string> _commonPasswords;

        public PasswordPolicyService()
        {
            // Lista de contraseñas comunes más usadas
            _commonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "123456", "password", "123456789", "12345678", "12345", "1234567",
                "qwerty", "abc123", "111111", "password1", "admin", "letmein",
                "welcome", "monkey", "dragon", "pass", "master", "hello", "freedom",
                "whatever", "qazwsx", "trustno1", "654321", "jordan23", "harley",
                "robert", "matthew", "jordan", "asshole", "daniel", "andrew"
            };
        }

        public PasswordValidationResult ValidatePassword(string password)
        {
            var result = new PasswordValidationResult
            {
                Errors = new List<string>()
            };

            if (string.IsNullOrWhiteSpace(password))
            {
                result.Errors.Add("La contraseña es requerida");
                result.IsValid = false;
                result.Strength = PasswordStrength.VeryWeak;
                return result;
            }

            var score = 0;

            // Validar longitud mínima
            if (password.Length < 8)
            {
                result.Errors.Add("La contraseña debe tener al menos 8 caracteres");
            }
            else if (password.Length >= 12)
            {
                score += 25; // Bonus por longitud extra
            }
            else
            {
                score += 15; // Longitud mínima aceptable
            }

            // Validar que contenga al menos una letra minúscula
            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                result.Errors.Add("La contraseña debe contener al menos una letra minúscula");
            }
            else
            {
                score += 15;
            }

            // Validar que contenga al menos una letra mayúscula
            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                result.Errors.Add("La contraseña debe contener al menos una letra mayúscula");
            }
            else
            {
                score += 15;
            }

            // Validar que contenga al menos un número
            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                result.Errors.Add("La contraseña debe contener al menos un número");
            }
            else
            {
                score += 15;
            }

            // Validar que contenga al menos un carácter especial
            if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
            {
                result.Errors.Add("La contraseña debe contener al menos un carácter especial (!@#$%^&* etc.)");
            }
            else
            {
                score += 20;
            }

            // Validar que no contenga espacios
            if (password.Contains(' '))
            {
                result.Errors.Add("La contraseña no debe contener espacios");
            }

            // Validar que no sea una contraseña común
            if (!IsPasswordSafe(password))
            {
                result.Errors.Add("Esta contraseña es muy común y no es segura");
                score -= 30; // Penalización fuerte por contraseña común
            }

            // Validar que no contenga secuencias obvias
            if (HasObviousPatterns(password))
            {
                result.Errors.Add("La contraseña no debe contener secuencias obvias (123, abc, etc.)");
                score -= 15;
            }

            // Bonus por diversidad de caracteres
            var uniqueChars = password.Distinct().Count();
            if (uniqueChars >= password.Length * 0.7) // 70% de caracteres únicos
            {
                score += 10;
            }

            // Asegurar que el score esté entre 0 y 100
            score = Math.Max(0, Math.Min(100, score));

            result.Score = score;
            result.IsValid = result.Errors.Count == 0;
            result.Strength = CalculateStrength(score);

            return result;
        }

        public bool IsPasswordSafe(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            // Verificar si está en la lista de contraseñas comunes
            if (_commonPasswords.Contains(password))
                return false;

            // Verificar variaciones simples de contraseñas comunes
            var lowerPassword = password.ToLower();
            foreach (var commonPassword in _commonPasswords)
            {
                if (lowerPassword.Contains(commonPassword.ToLower()) && 
                    password.Length <= commonPassword.Length + 3)
                {
                    return false;
                }
            }

            return true;
        }

        private bool HasObviousPatterns(string password)
        {
            var lower = password.ToLower();
            
            // Secuencias numéricas
            if (Regex.IsMatch(password, @"(012|123|234|345|456|567|678|789|890|987|876|765|654|543|432|321|210)"))
                return true;

            // Secuencias alfabéticas
            if (Regex.IsMatch(lower, @"(abc|bcd|cde|def|efg|fgh|ghi|hij|ijk|jkl|klm|lmn|mno|nop|opq|pqr|qrs|rst|stu|tuv|uvw|vwx|wxy|xyz)"))
                return true;

            // Patrones de teclado
            if (Regex.IsMatch(lower, @"(qwer|wert|erty|rtyu|tyui|yuio|uiop|asdf|sdfg|dfgh|fghj|ghjk|hjkl|zxcv|xcvb|cvbn|vbnm)"))
                return true;

            // Repeticiones simples
            if (Regex.IsMatch(password, @"(.)\1{3,}")) // 4 o más caracteres iguales seguidos
                return true;

            return false;
        }

        private PasswordStrength CalculateStrength(int score)
        {
            return score switch
            {
                >= 90 => PasswordStrength.VeryStrong,
                >= 75 => PasswordStrength.Strong,
                >= 60 => PasswordStrength.Good,
                >= 40 => PasswordStrength.Fair,
                >= 20 => PasswordStrength.Weak,
                _ => PasswordStrength.VeryWeak
            };
        }
    }
}
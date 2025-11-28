using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

namespace Application.Helpers
{
    /// <summary>
    /// Helper para hashing seguro de tokens JWT
    /// Utiliza SHA256 para generar un hash único y determinista del token
    /// Se usa para almacenar tokens en blacklist de forma segura
    /// </summary>
    public static class TokenHashHelper
    {
        /// <summary>
        /// Hashea un token JWT usando SHA256 para almacenamiento seguro
        /// No se debe guardar el token completo en BD por razones de seguridad
        /// </summary>
        /// <param name="token">Token JWT sin hashear</param>
        /// <returns>Hash SHA256 del token en formato Base64</returns>
        public static string HashToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Token cannot be null or empty", nameof(token));
            }

            using (var sha256 = SHA256.Create())
            {
                var tokenBytes = Encoding.UTF8.GetBytes(token);
                var hashBytes = sha256.ComputeHash(tokenBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        /// <summary>
        /// Extrae la fecha de expiración de un token JWT
        /// </summary>
        /// <param name="token">Token JWT a analizar</param>
        /// <param name="defaultExpirationMinutes">Minutos por defecto si no se puede extraer la expiración</param>
        /// <returns>Fecha de expiración del token</returns>
        public static DateTime GetTokenExpiration(string token, int defaultExpirationMinutes = 15)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return DateTime.UtcNow.AddMinutes(defaultExpirationMinutes);
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                return jwtToken.ValidTo;
            }
            catch
            {
                // Si falla, retornar la expiración por defecto
                return DateTime.UtcNow.AddMinutes(defaultExpirationMinutes);
            }
        }
    }
}
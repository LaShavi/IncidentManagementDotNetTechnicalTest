using Application.Ports;
using BCrypt.Net;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class BCryptPasswordHasher : IPasswordHasher
    {
        private readonly ILogger<BCryptPasswordHasher> _logger;

        public BCryptPasswordHasher(ILogger<BCryptPasswordHasher> logger)
        {
            _logger = logger;
            _logger.LogDebug("BCryptPasswordHasher initialized with work factor 12");
        }

        public string HashPassword(string password)
        {
            _logger.LogDebug("Hashing password with BCrypt (work factor: 12)");
            
            try
            {
                if (string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning("Attempted to hash null or empty password");
                    throw new ArgumentException("Password cannot be null or empty", nameof(password));
                }

                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
                _logger.LogDebug("Password hashed successfully");
                return hashedPassword;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hashing password");
                throw;
            }
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            _logger.LogDebug("Verifying password with BCrypt");
            
            try
            {
                if (string.IsNullOrEmpty(password))
                {
                    _logger.LogDebug("Password verification failed - null or empty password provided");
                    return false;
                }

                if (string.IsNullOrEmpty(hashedPassword))
                {
                    _logger.LogDebug("Password verification failed - null or empty hash provided");
                    return false;
                }

                var isValid = BCrypt.Net.BCrypt.Verify(password, hashedPassword);
                _logger.LogDebug("Password verification result: {IsValid}", isValid);
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying password");
                return false;
            }
        }
    }
}
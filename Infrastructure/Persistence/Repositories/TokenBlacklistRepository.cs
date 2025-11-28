using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Application.Ports;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Repositorio para gestionar tokens en blacklist
    /// </summary>
    public class TokenBlacklistRepository : ITokenBlacklistRepository
    {
        private readonly AppDbContext _context;

        public TokenBlacklistRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Agrega un token a la blacklist
        /// </summary>
        public async Task AddTokenAsync(Guid userId, string tokenHash, DateTime expiresAt, string reason = "Manual revocation")
        {
            var blacklistEntry = new TokenBlacklistEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = expiresAt,
                RevokedAt = DateTime.UtcNow,
                Reason = reason
            };

            _context.TokenBlacklist.Add(blacklistEntry);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Verifica si un token está en blacklist
        /// </summary>
        public async Task<bool> IsTokenBlacklistedAsync(string tokenHash)
        {
            return await _context.TokenBlacklist
                .AnyAsync(tb => tb.TokenHash == tokenHash && tb.ExpiresAt > DateTime.UtcNow);
        }

        /// <summary>
        /// Limpia tokens expirados de la blacklist
        /// </summary>
        public async Task CleanExpiredTokensAsync()
        {
            var expiredTokens = await _context.TokenBlacklist
                .Where(tb => tb.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync();

            _context.TokenBlacklist.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Elimina todos los tokens de un usuario de la blacklist
        /// </summary>
        public async Task RemoveUserTokensAsync(Guid userId)
        {
            var userTokens = await _context.TokenBlacklist
                .Where(tb => tb.UserId == userId)
                .ToListAsync();

            _context.TokenBlacklist.RemoveRange(userTokens);
            await _context.SaveChangesAsync();
        }
    }
}
using Application.Ports;
using Domain.Entities;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Repository for managing refresh tokens in the database.
    /// </summary>
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RefreshTokenRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefreshTokenRepository"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="logger">The logger instance.</param>
        public RefreshTokenRepository(AppDbContext context, ILogger<RefreshTokenRepository> logger)
        {
            _context = context;
            _logger = logger;
            _logger.LogDebug("RefreshTokenRepository initialized successfully");
        }

        /// <summary>
        /// Gets a refresh token by its token string.
        /// </summary>
        /// <param name="token">The refresh token string.</param>
        /// <returns>The refresh token if found; otherwise, null.</returns>
        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            _logger.LogDebug("Retrieving refresh token: {TokenPrefix}...", token[..8]);
            
            try
            {
                var entity = await _context.RefreshTokens
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Token == token);
                
                if (entity == null)
                {
                    _logger.LogDebug("Refresh token not found: {TokenPrefix}...", token[..8]);
                    return null;
                }
                
                var refreshToken = MapToDomain(entity);
                _logger.LogDebug("Refresh token retrieved successfully: {TokenPrefix}... (UserId: {UserId})", 
                    token[..8], entity.UserId);
                return refreshToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving refresh token: {TokenPrefix}...", token[..8]);
                throw;
            }
        }

        /// <summary>
        /// Gets all active (not revoked and not expired) refresh tokens for a user.
        /// </summary>
        /// <param name="userId">The user's ID.</param>
        /// <returns>A collection of active refresh tokens.</returns>
        public async Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(Guid userId)
        {
            _logger.LogDebug("Retrieving active refresh tokens for user: {UserId}", userId);
            
            try
            {
                var entities = await _context.RefreshTokens
                    .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync();
                
                var tokens = entities.Select(MapToDomain).ToList();
                _logger.LogDebug("Retrieved {Count} active refresh tokens for user: {UserId}", tokens.Count, userId);
                return tokens;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active refresh tokens for user: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Adds a new refresh token to the database.
        /// </summary>
        /// <param name="refreshToken">The refresh token to add.</param>
        public async Task AddAsync(RefreshToken refreshToken)
        {
            _logger.LogDebug("Adding refresh token for user: {UserId}, expires at: {ExpiresAt}", 
                refreshToken.UserId, refreshToken.ExpiresAt);
            
            try
            {
                var entity = MapToEntity(refreshToken);
                _context.RefreshTokens.Add(entity);
                await _context.SaveChangesAsync();
                
                _logger.LogDebug("Refresh token added successfully for user: {UserId} (TokenId: {TokenId})", 
                    refreshToken.UserId, refreshToken.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding refresh token for user: {UserId}", refreshToken.UserId);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing refresh token in the database.
        /// </summary>
        /// <param name="refreshToken">The refresh token with updated values.</param>
        public async Task UpdateAsync(RefreshToken refreshToken)
        {
            _logger.LogDebug("Updating refresh token: {TokenId} (UserId: {UserId})", refreshToken.Id, refreshToken.UserId);
            
            try
            {
                var entity = await _context.RefreshTokens.FindAsync(refreshToken.Id);
                if (entity != null)
                {
                    entity.IsRevoked = refreshToken.IsRevoked;
                    entity.RevokedAt = refreshToken.RevokedAt;
                    entity.ReplacedBy = refreshToken.ReplacedBy;

                    await _context.SaveChangesAsync();
                    _logger.LogDebug("Refresh token updated successfully: {TokenId} (UserId: {UserId})", 
                        refreshToken.Id, refreshToken.UserId);
                }
                else
                {
                    _logger.LogWarning("Attempted to update non-existent refresh token: {TokenId}", refreshToken.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating refresh token: {TokenId} (UserId: {UserId})", 
                    refreshToken.Id, refreshToken.UserId);
                throw;
            }
        }

        /// <summary>
        /// Revokes all active refresh tokens for a user.
        /// </summary>
        /// <param name="userId">The user's ID.</param>
        public async Task RevokeAllByUserIdAsync(Guid userId)
        {
            _logger.LogInformation("Revoking all refresh tokens for user: {UserId}", userId);
            
            try
            {
                var tokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                    .ToListAsync();

                if (tokens.Count == 0)
                {
                    _logger.LogDebug("No active tokens found to revoke for user: {UserId}", userId);
                    return;
                }

                foreach (var token in tokens)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Revoked {Count} refresh tokens for user: {UserId}", tokens.Count, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking all refresh tokens for user: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Revokes a specific refresh token by its token string.
        /// </summary>
        /// <param name="token">The refresh token string.</param>
        public async Task RevokeTokenAsync(string token)
        {
            _logger.LogDebug("Revoking specific refresh token: {TokenPrefix}...", token[..8]);
            
            try
            {
                var entity = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == token);

                if (entity != null)
                {
                    entity.IsRevoked = true;
                    entity.RevokedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    
                    _logger.LogDebug("Refresh token revoked successfully: {TokenPrefix}... (UserId: {UserId})", 
                        token[..8], entity.UserId);
                }
                else
                {
                    _logger.LogWarning("Attempted to revoke non-existent refresh token: {TokenPrefix}...", token[..8]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking refresh token: {TokenPrefix}...", token[..8]);
                throw;
            }
        }

        /// <summary>
        /// Removes all expired or revoked refresh tokens from the database.
        /// </summary>
        public async Task RemoveExpiredTokensAsync()
        {
            _logger.LogInformation("Removing expired and revoked refresh tokens");
            
            try
            {
                var expiredTokens = await _context.RefreshTokens
                    .Where(rt => rt.ExpiresAt <= DateTime.UtcNow || rt.IsRevoked)
                    .ToListAsync();

                if (expiredTokens.Count == 0)
                {
                    _logger.LogDebug("No expired tokens found to remove");
                    return;
                }

                _context.RefreshTokens.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Removed {Count} expired/revoked refresh tokens", expiredTokens.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing expired refresh tokens");
                throw;
            }
        }

        /// <summary>
        /// Maps a refresh token entity to the domain model.
        /// </summary>
        /// <param name="entity">The refresh token entity.</param>
        /// <returns>The domain refresh token.</returns>
        private static RefreshToken MapToDomain(RefreshTokenEntity entity)
        {
            var domain = new RefreshToken
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Token = entity.Token,
                CreatedAt = entity.CreatedAt,
                ExpiresAt = entity.ExpiresAt,
                IsRevoked = entity.IsRevoked,
                RevokedAt = entity.RevokedAt,
                ReplacedBy = entity.ReplacedBy
            };

            if (entity.User != null)
            {
                domain.User = new User
                {
                    Id = entity.User.Id,
                    Username = entity.User.Username,
                    Email = entity.User.Email,
                    PasswordHash = entity.User.PasswordHash,
                    FirstName = entity.User.FirstName,
                    LastName = entity.User.LastName,
                    Role = entity.User.Role,
                    IsActive = entity.User.IsActive,
                    CreatedAt = entity.User.CreatedAt,
                    LastAccess = entity.User.LastAccess,
                    FailedAttempts = entity.User.FailedAttempts,
                    LockedUntil = entity.User.LockedUntil
                };
            }

            return domain;
        }

        /// <summary>
        /// Maps a domain refresh token to the entity model.
        /// </summary>
        /// <param name="domain">The domain refresh token.</param>
        /// <returns>The refresh token entity.</returns>
        private static RefreshTokenEntity MapToEntity(RefreshToken domain)
        {
            return new RefreshTokenEntity
            {
                Id = domain.Id,
                UserId = domain.UserId,
                Token = domain.Token,
                CreatedAt = domain.CreatedAt,
                ExpiresAt = domain.ExpiresAt,
                IsRevoked = domain.IsRevoked,
                RevokedAt = domain.RevokedAt,
                ReplacedBy = domain.ReplacedBy
            };
        }
    }
}
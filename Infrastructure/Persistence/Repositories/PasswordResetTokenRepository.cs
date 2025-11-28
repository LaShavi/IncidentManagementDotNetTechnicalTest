using Domain.Entities;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Application.Ports;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Repository for managing password reset tokens in the database.
    /// Handles creation, retrieval, and marking tokens as used for password reset flows.
    /// </summary>
    public class PasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PasswordResetTokenRepository> _logger;
        
        public PasswordResetTokenRepository(AppDbContext context, ILogger<PasswordResetTokenRepository> logger)
        {
            _context = context;
            _logger = logger;
            _logger.LogDebug("PasswordResetTokenRepository initialized successfully");
        }

        /// <summary>
        /// Adds a new password reset token to the database.
        /// </summary>
        /// <param name="token">The password reset token entity to add.</param>
        public async Task AddAsync(PasswordResetToken token)
        {
            _logger.LogInformation("Adding password reset token for user: {UserId}, expires at: {ExpiresAt}", 
                token.UserId, token.ExpiresAt);
            
            try
            {
                var entity = new PasswordResetTokenEntity
                {
                    Id = token.Id,
                    UserId = token.UserId,
                    Token = token.Token,
                    ExpiresAt = token.ExpiresAt,
                    IsUsed = token.IsUsed
                };
                
                _context.PasswordResetTokens.Add(entity);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Password reset token added successfully: {TokenId} (UserId: {UserId})", 
                    token.Id, token.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding password reset token for user: {UserId}", token.UserId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a password reset token by its token string.
        /// </summary>
        /// <param name="token">The token string to search for.</param>
        /// <returns>The matching PasswordResetToken or null if not found.</returns>
        public async Task<PasswordResetToken?> GetByTokenAsync(string token)
        {
            _logger.LogDebug("Retrieving password reset token: {TokenPrefix}...", token[..8]);
            
            try
            {
                var entity = await _context.PasswordResetTokens
                    .FirstOrDefaultAsync(t => t.Token == token);
                    
                if (entity == null) 
                {
                    _logger.LogDebug("Password reset token not found: {TokenPrefix}...", token[..8]);
                    return null;
                }
                
                var resetToken = new PasswordResetToken
                {
                    Id = entity.Id,
                    UserId = entity.UserId,
                    Token = entity.Token,
                    ExpiresAt = entity.ExpiresAt,
                    IsUsed = entity.IsUsed
                };
                
                _logger.LogDebug("Password reset token retrieved successfully: {TokenPrefix}... (UserId: {UserId}, IsUsed: {IsUsed}, ExpiresAt: {ExpiresAt})", 
                    token[..8], entity.UserId, entity.IsUsed, entity.ExpiresAt);
                return resetToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving password reset token: {TokenPrefix}...", token[..8]);
                throw;
            }
        }

        /// <summary>
        /// Marks a password reset token as used in the database.
        /// </summary>
        /// <param name="id">The ID of the token to mark as used.</param>
        public async Task MarkAsUsedAsync(Guid id)
        {
            _logger.LogInformation("Marking password reset token as used: {TokenId}", id);
            
            try
            {
                var entity = await _context.PasswordResetTokens.FindAsync(id);
                if (entity != null)
                {
                    entity.IsUsed = true;
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Password reset token marked as used successfully: {TokenId} (UserId: {UserId})", 
                        id, entity.UserId);
                }
                else
                {
                    _logger.LogWarning("Attempted to mark non-existent password reset token as used: {TokenId}", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking password reset token as used: {TokenId}", id);
                throw;
            }
        }
    }
}

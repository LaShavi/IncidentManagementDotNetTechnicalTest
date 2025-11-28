using Domain.Entities;

namespace Application.Ports
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(Guid usuarioId);
        Task AddAsync(RefreshToken refreshToken);
        Task UpdateAsync(RefreshToken refreshToken);
        Task RevokeAllByUserIdAsync(Guid usuarioId);
        Task RevokeTokenAsync(string token);
        Task RemoveExpiredTokensAsync();
    }
}
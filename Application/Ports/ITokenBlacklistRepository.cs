namespace Application.Ports
{
    /// <summary>
    /// Interfaz para el repositorio de Token Blacklist
    /// </summary>
    public interface ITokenBlacklistRepository
    {
        Task AddTokenAsync(Guid userId, string tokenHash, DateTime expiresAt, string reason = "Manual revocation");
        Task<bool> IsTokenBlacklistedAsync(string tokenHash);
        Task CleanExpiredTokensAsync();
        Task RemoveUserTokensAsync(Guid userId);
    }
}

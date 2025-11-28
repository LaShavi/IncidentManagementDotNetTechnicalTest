using Domain.Entities;

namespace Application.Ports
{
    public interface IPasswordResetTokenRepository
    {
        Task AddAsync(PasswordResetToken token);
        Task<PasswordResetToken?> GetByTokenAsync(string token);
        Task MarkAsUsedAsync(Guid id);
    }
}

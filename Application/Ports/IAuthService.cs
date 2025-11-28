using Application.DTOs.Auth;
using Domain.Entities;

namespace Application.Ports
{
    public interface IAuthService
    {
        Task<LoginResponseDTO> LoginAsync(LoginRequestDTO request);
        Task<LoginResponseDTO> RegisterAsync(RegisterRequestDTO request);
        Task<LoginResponseDTO> RefreshTokenAsync(RefreshTokenRequestDTO request);
        Task RevokeTokenAsync(string token);
        Task RevokeAllTokensAsync(Guid userId);
        Task RevokeAccessTokenAsync(Guid userId, string token);
        Task<bool> ValidateTokenAsync(string token);
        string GenerateJwtToken(User user);
        string GenerateRefreshToken();
        Task<User?> GetUserFromTokenAsync(string token);
        Task UpdateUserProfileAsync(Guid userId, UpdateUserProfileDTO dto);
        Task ChangePasswordAsync(Guid userId, ChangePasswordDTO dto);
        Task RequestPasswordResetAsync(string email);
        Task ResetPasswordAsync(ResetPasswordDTO dto);
        Task DeleteUserAsync(Guid userId);
    }
}

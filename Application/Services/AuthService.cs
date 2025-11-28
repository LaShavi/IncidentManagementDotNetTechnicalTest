using Application.DTOs.Auth;
using Application.Ports;
using Application.Helpers;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;
        private readonly ITokenBlacklistRepository _tokenBlacklistRepository;

        public AuthService(
            IUserRepository userRepository, 
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHasher passwordHasher,
            IConfiguration configuration,
            IPasswordResetTokenRepository passwordResetTokenRepository,
            IEmailService emailService,
            ILogger<AuthService> logger,
            ITokenBlacklistRepository tokenBlacklistRepository)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
            _passwordResetTokenRepository = passwordResetTokenRepository;
            _emailService = emailService;
            _logger = logger;
            _tokenBlacklistRepository = tokenBlacklistRepository;

            _logger.LogDebug("AuthService initialized successfully");
        }

        /// <summary>
        /// Authenticates a user with username and password. Returns JWT and refresh token if successful.
        /// </summary>
        /// <param name="request">Login credentials (username and password).</param>
        /// <returns>LoginResponseDTO with tokens and user info.</returns>
        public async Task<LoginResponseDTO> LoginAsync(LoginRequestDTO request)
        {
            _logger.LogInformation("Login attempt for user: {Username}", request.Username);

            var user = await _userRepository.GetByUsernameAsync(request.Username);
            if (user == null)
            {
                _logger.LogWarning("Login failed - User not found: {Username}", request.Username);
                throw new UnauthorizedAccessException(ResourceTextHelper.Get("InvalidCredentials"));
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed - Account deactivated: {Username} (UserId: {UserId})", request.Username, user.Id);
                throw new UnauthorizedAccessException(ResourceTextHelper.Get("AccountDeactivated"));
            }

            if (user.IsLocked())
            {
                _logger.LogWarning("Login failed - Account locked: {Username} (UserId: {UserId}) until {LockedUntil}", 
                    request.Username, user.Id, user.LockedUntil);
                try
                {
                    await _emailService.SendAccountLockedNotificationAsync(user.Email, user.Username, user.LockedUntil);
                    _logger.LogInformation("Account locked notification sent to: {Email} (UserId: {UserId})", user.Email, user.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send account locked notification to: {Email} (UserId: {UserId})", user.Email, user.Id);
                }
                throw new UnauthorizedAccessException(string.Format(ResourceTextHelper.Get("AccountLocked"), $"{user.LockedUntil:dd/MM/yyyy HH:mm}"));
            }

            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed - Invalid password: {Username} (UserId: {UserId}). Failed attempts: {FailedAttempts}", 
                    request.Username, user.Id, user.FailedAttempts + 1);
                
                user.RegisterFailedAttempt();
                await _userRepository.UpdateAsync(user);
                throw new UnauthorizedAccessException(ResourceTextHelper.Get("InvalidCredentials"));
            }

            // Successful login
            _logger.LogInformation("Login successful: {Username} (UserId: {UserId})", request.Username, user.Id);
            user.RegisterSuccessfulAccess();
            await _userRepository.UpdateAsync(user);

            // Generate tokens
            var accessToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            _logger.LogDebug("Generated new tokens for user: {Username} (UserId: {UserId})", request.Username, user.Id);

            // Save refresh token
            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenExpirationDays())
            };

            await _refreshTokenRepository.AddAsync(refreshTokenEntity);

            return new LoginResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes()),
                User = new UserInfoDTO
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    LastAccess = user.LastAccess
                }
            };
        }

        /// <summary>
        /// Registers a new user and returns tokens and user info if registration is successful.
        /// </summary>
        /// <param name="request">Registration data (username, email, password, first name, last name).</param>
        /// <returns>LoginResponseDTO with tokens and user info for the new user.</returns>
        public async Task<LoginResponseDTO> RegisterAsync(RegisterRequestDTO request)
        {
            _logger.LogInformation("Registration attempt for username: {Username}, email: {Email}", request.Username, request.Email);

            // Validar política de contraseñas
            var passwordPolicyService = new PasswordPolicyService();
            var passwordValidation = passwordPolicyService.ValidatePassword(request.Password);
            
            if (!passwordValidation.IsValid)
            {
                var errorMessage = string.Join("; ", passwordValidation.Errors);
                _logger.LogWarning("Registration failed - Password policy violation for username: {Username}. Errors: {Errors}", 
                    request.Username, errorMessage);
                throw new InvalidOperationException(string.Format(ResourceTextHelper.Get("PasswordPolicyViolation"), errorMessage));
            }

            _logger.LogDebug("Password validation passed for user: {Username}. Strength: {Strength}, Score: {Score}", 
                request.Username, passwordValidation.Strength, passwordValidation.Score);

            // Verificar si el usuario ya existe
            if (await _userRepository.ExistsUsernameAsync(request.Username))
            {
                _logger.LogWarning("Registration failed - Username already exists: {Username}", request.Username);
                throw new InvalidOperationException(ResourceTextHelper.Get("UsernameAlreadyExists"));
            }

            if (await _userRepository.ExistsEmailAsync(request.Email))
            {
                _logger.LogWarning("Registration failed - Email already registered: {Email}", request.Email);
                throw new InvalidOperationException(ResourceTextHelper.Get("EmailAlreadyRegistered"));
            }

            // Create new user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);
            _logger.LogInformation("User registered successfully: {Username} (UserId: {UserId}) with password strength: {Strength}", 
                user.Username, user.Id, passwordValidation.Strength);

            // Send welcome email
            try
            {
                await _emailService.SendWelcomeEmailAsync(user.Email, user.Username);
                _logger.LogInformation("Welcome email sent to: {Email} (UserId: {UserId})", user.Email, user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to: {Email} (UserId: {UserId})", user.Email, user.Id);
            }

            // Generate tokens
            var accessToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            // Save refresh token
            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenExpirationDays())
            };

            await _refreshTokenRepository.AddAsync(refreshTokenEntity);

            return new LoginResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes()),
                User = new UserInfoDTO
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    LastAccess = user.LastAccess
                }
            };
        }

        /// <summary>
        /// Issues a new access token using a valid refresh token.
        /// </summary>
        /// <param name="request">Refresh token request.</param>
        /// <returns>LoginResponseDTO with new tokens and user info.</returns>
        public async Task<LoginResponseDTO> RefreshTokenAsync(RefreshTokenRequestDTO request)
        {
            _logger.LogDebug("Token refresh attempt with token: {TokenPrefix}...", request.RefreshToken[..8]);

            var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);
            
            if (refreshToken == null || !refreshToken.IsActive())
            {
                _logger.LogWarning("Token refresh failed - Invalid or expired refresh token: {TokenPrefix}...", request.RefreshToken[..8]);
                throw new UnauthorizedAccessException(ResourceTextHelper.Get("InvalidOrExpiredToken"));
            }

            var user = await _userRepository.GetByIdAsync(refreshToken.UserId);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Token refresh failed - Invalid user for token: {TokenPrefix}... (UserId: {UserId})", 
                    request.RefreshToken[..8], refreshToken.UserId);
                throw new UnauthorizedAccessException(ResourceTextHelper.Get("InvalidUser"));
            }

            _logger.LogInformation("Token refresh successful for user: {Username} (UserId: {UserId})", user.Username, user.Id);

            // Revoke current token
            refreshToken.Revoke();
            await _refreshTokenRepository.UpdateAsync(refreshToken);

            // Generate new tokens
            var newAccessToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            // Save new refresh token
            var newRefreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = newRefreshToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenExpirationDays())
            };

            await _refreshTokenRepository.AddAsync(newRefreshTokenEntity);

            return new LoginResponseDTO
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes()),
                User = new UserInfoDTO
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    LastAccess = user.LastAccess
                }
            };
        }

        /// <summary>
        /// Revokes a specific refresh token, making it unusable for future requests.
        /// </summary>
        /// <param name="token">Refresh token to revoke.</param>
        public async Task RevokeTokenAsync(string token)
        {
            _logger.LogInformation("Revoking single refresh token: {TokenPrefix}...", token[..8]);
            await _refreshTokenRepository.RevokeTokenAsync(token);
        }

        /// <summary>
        /// Revokes all refresh tokens for a specific user (logout everywhere).
        /// </summary>
        /// <param name="userId">User ID whose tokens will be revoked.</param>
        public async Task RevokeAllTokensAsync(Guid userId)
        {
            _logger.LogInformation("Revoking all refresh tokens for user: {UserId}", userId);
            await _refreshTokenRepository.RevokeAllByUserIdAsync(userId);
        }

        /// <summary>
        /// Validates if a JWT token is valid and not expired.
        /// </summary>
        /// <param name="token">JWT token to validate.</param>
        /// <returns>True if the token is valid, false otherwise.</returns>
        public Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = GetTokenValidationParameters();
                
                tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Generates a JWT access token for a user.
        /// </summary>
        /// <param name="user">User entity.</param>
        /// <returns>JWT token as string.</returns>
        public string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(GetSecretKey());
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.GivenName, user.FirstName),
                    new Claim(ClaimTypes.Surname, user.LastName),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("username", user.Username),
                    new Claim("fullName", user.GetFullName())
                }),
                Expires = DateTime.UtcNow.AddMinutes(GetAccessTokenExpirationMinutes()),
                Issuer = GetIssuer(),
                Audience = GetAudience(),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Generates a secure random refresh token.
        /// </summary>
        /// <returns>Refresh token as string.</returns>
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        /// <summary>
        /// Gets the user entity from a valid JWT token.
        /// </summary>
        /// <param name="token">JWT token.</param>
        /// <returns>User entity if token is valid, null otherwise.</returns>
        public async Task<User?> GetUserFromTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = GetTokenValidationParameters();
                
                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
                
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return await _userRepository.GetByIdAsync(userId);
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Updates the user's profile information (email, first name, last name).
        /// </summary>
        public async Task UpdateUserProfileAsync(Guid userId, UpdateUserProfileDTO dto)
        {
            _logger.LogInformation("Updating profile for user: {UserId}", userId);
            
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) 
            {
                _logger.LogWarning("Profile update failed - User not found: {UserId}", userId);
                throw new InvalidOperationException(ResourceTextHelper.Get("UserNotFound"));
            }
            
            var oldEmail = user.Email;
            user.Email = dto.Email;
            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            
            await _userRepository.UpdateAsync(user);
            _logger.LogInformation("Profile updated successfully for user: {UserId}. Email changed from {OldEmail} to {NewEmail}", 
                userId, oldEmail, dto.Email);

            // Notificar por correo la actualización de perfil
            try
            {
                await _emailService.SendProfileUpdatedNotificationAsync(user.Email, user.Username);
                _logger.LogInformation("Profile update notification sent to: {Email} (UserId: {UserId})", user.Email, user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send profile update notification to: {Email} (UserId: {UserId})", user.Email, user.Id);
            }
        }

        /// <summary>
        /// Changes the user's password (requires current password).
        /// </summary>
        public async Task ChangePasswordAsync(Guid userId, ChangePasswordDTO dto)
        {
            _logger.LogInformation("Password change attempt for user: {UserId}", userId);
            
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) 
            {
                _logger.LogWarning("Password change failed - User not found: {UserId}", userId);
                throw new InvalidOperationException(ResourceTextHelper.Get("UserNotFound"));
            }
            
            if (!_passwordHasher.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
            {
                _logger.LogWarning("Password change failed - Current password incorrect for user: {UserId}", userId);
                throw new UnauthorizedAccessException(ResourceTextHelper.Get("CurrentPasswordIncorrect"));
            }
            
            if (dto.NewPassword != dto.ConfirmNewPassword)
            {
                _logger.LogWarning("Password change failed - New passwords do not match for user: {UserId}", userId);
                throw new InvalidOperationException(ResourceTextHelper.Get("PasswordsDoNotMatch"));
            }

            // Validar política de contraseñas para la nueva contraseña
            var passwordPolicyService = new PasswordPolicyService();
            var passwordValidation = passwordPolicyService.ValidatePassword(dto.NewPassword);
            
            if (!passwordValidation.IsValid)
            {
                var errorMessage = string.Join("; ", passwordValidation.Errors);
                _logger.LogWarning("Password change failed - New password policy violation for user: {UserId}. Errors: {Errors}", 
                    userId, errorMessage);
                throw new InvalidOperationException(string.Format(ResourceTextHelper.Get("PasswordPolicyViolation"), errorMessage));
            }

            _logger.LogDebug("New password validation passed for user: {UserId}. Strength: {Strength}, Score: {Score}", 
                userId, passwordValidation.Strength, passwordValidation.Score);
            
            user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);
            await _userRepository.UpdateAsync(user);
            _logger.LogInformation("Password changed successfully for user: {UserId} with strength: {Strength}", 
                userId, passwordValidation.Strength);

            // Notificar por correo el cambio de contraseña
            try
            {
                await _emailService.SendPasswordChangedNotificationAsync(user.Email, user.Username);
                _logger.LogInformation("Password change notification sent to: {Email} (UserId: {UserId})", user.Email, user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password change notification to: {Email} (UserId: {UserId})", user.Email, user.Id);
            }
        }

        /// <summary>
        /// Initiates password reset process (send token to email if user exists).
        /// </summary>
        public async Task RequestPasswordResetAsync(string email)
        {
            _logger.LogInformation("Password reset request for email: {Email}", email);
            
            var user = await _userRepository.GetByEmailAsync(email);
            // Always respond as if email was sent, even if user doesn't exist (security best practice)
            if (user == null) 
            {
                _logger.LogInformation("Password reset request for non-existent email: {Email}", email);
                return;
            }

            // Generate token
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            var expiresAt = DateTime.UtcNow.AddHours(1);
            var resetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = token,
                ExpiresAt = expiresAt,
                IsUsed = false
            };
            await _passwordResetTokenRepository.AddAsync(resetToken);

            // Send password reset email
            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, user.Username, token);
                _logger.LogInformation("Password reset email sent successfully to: {Email} (UserId: {UserId})", user.Email, user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to: {Email} (UserId: {UserId})", user.Email, user.Id);
            }
        }

        /// <summary>
        /// Resets the user's password using a reset token.
        /// </summary>
        public async Task ResetPasswordAsync(ResetPasswordDTO dto)
        {
            _logger.LogInformation("Password reset attempt with token: {TokenPrefix}...", dto.Token[..8]);
            
            var resetToken = await _passwordResetTokenRepository.GetByTokenAsync(dto.Token);
            if (resetToken == null || resetToken.IsUsed || resetToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Password reset failed - Invalid or expired token: {TokenPrefix}...", dto.Token[..8]);
                throw new InvalidOperationException(ResourceTextHelper.Get("InvalidOrExpiredResetToken"));
            }

            var user = await _userRepository.GetByIdAsync(resetToken.UserId);
            if (user == null)
            {
                _logger.LogError("Password reset failed - User not found for valid token: {TokenPrefix}... (UserId: {UserId})", 
                    dto.Token[..8], resetToken.UserId);
                throw new InvalidOperationException(ResourceTextHelper.Get("UserNotFound"));
            }

            if (dto.NewPassword != dto.ConfirmNewPassword)
            {
                _logger.LogWarning("Password reset failed - Passwords do not match for user: {UserId}", resetToken.UserId);
                throw new InvalidOperationException(ResourceTextHelper.Get("PasswordsDoNotMatch"));
            }

            // Validar política de contraseñas
            var passwordPolicyService = new PasswordPolicyService();
            var passwordValidation = passwordPolicyService.ValidatePassword(dto.NewPassword);
            
            if (!passwordValidation.IsValid)
            {
                var errorMessage = string.Join("; ", passwordValidation.Errors);
                _logger.LogWarning("Password reset failed - Password policy violation for user: {UserId}. Errors: {Errors}", 
                    resetToken.UserId, errorMessage);
                throw new InvalidOperationException(string.Format(ResourceTextHelper.Get("PasswordPolicyViolation"), errorMessage));
            }

            _logger.LogDebug("Password reset validation passed for user: {UserId}. Strength: {Strength}, Score: {Score}", 
                resetToken.UserId, passwordValidation.Strength, passwordValidation.Score);

            user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);
            await _userRepository.UpdateAsync(user);
            await _passwordResetTokenRepository.MarkAsUsedAsync(resetToken.Id);
            
            _logger.LogInformation("Password reset completed successfully for user: {UserId} with strength: {Strength}", 
                resetToken.UserId, passwordValidation.Strength);
        }

        /// <summary>
        /// Deletes a user by ID.
        /// </summary>
        public async Task DeleteUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new InvalidOperationException("User not found");
            await _userRepository.DeleteAsync(userId);

            // Notificar por correo la eliminación de cuenta
            try
            {
                await _emailService.SendAccountDeletedNotificationAsync(user.Email, user.Username);
                _logger.LogInformation("Account deletion notification sent to: {Email} (UserId: {UserId})", user.Email, user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send account deletion notification to: {Email} (UserId: {UserId})", user.Email, user.Id);
            }
        }

        private TokenValidationParameters GetTokenValidationParameters()
        {
            return new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(GetSecretKey())),
                ValidateIssuer = true,
                ValidIssuer = GetIssuer(),
                ValidateAudience = true,
                ValidAudience = GetAudience(),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        }

        private string GetSecretKey() => _configuration["Authentication:SecretKey"] ?? throw new InvalidOperationException("SecretKey no configurada");
        private string GetIssuer() => _configuration["Authentication:Issuer"] ?? throw new InvalidOperationException("Issuer no configurado");
        private string GetAudience() => _configuration["Authentication:Audience"] ?? throw new InvalidOperationException("Audience no configurado");
        private int GetAccessTokenExpirationMinutes() => int.Parse(_configuration["Authentication:AccessTokenExpiration"] ?? "15");
        private int GetRefreshTokenExpirationDays() => int.Parse(_configuration["Authentication:RefreshTokenExpiration"] ?? "7");

        /// <summary>
        /// Revokes an access token immediately by adding it to the blacklist
        /// </summary>
        public async Task RevokeAccessTokenAsync(Guid userId, string token)
        {
            try
            {
                var tokenHash = TokenHashHelper.HashToken(token);
                var expiresAt = TokenHashHelper.GetTokenExpiration(token, GetAccessTokenExpirationMinutes());

                await _tokenBlacklistRepository.AddTokenAsync(
                    userId,
                    tokenHash,
                    expiresAt,
                    "access_token_revocation"
                );

                _logger.LogInformation("Access token revoked for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking access token");
                throw;
            }
        }
    }
}
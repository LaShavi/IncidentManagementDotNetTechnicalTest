using Application.DTOs.Auth;
using Application.Ports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Api.Helpers;
using Api.DTOs.Common;
using Application.Services;
using Application.Helpers;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Authenticates a user with username and password, returning JWT and refresh token if successful.
        /// </summary>
        /// <param name="request">Login credentials (username and password).</param>
        /// <returns>JWT access token, refresh token, and user info if credentials are valid.</returns>
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponseDTO>>> Login([FromBody] LoginRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponseHelper.ValidationError(ModelState));

            try
            {
                var response = await _authService.LoginAsync(request);
                return Ok(ApiResponseHelper.Success(response, ResourceTextHelper.Get("LoginSuccess")));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponseHelper.Unauthorized(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseHelper.InternalServerError(ResourceTextHelper.Get("UserNotValid"), ex));
            }
        }

        /// <summary>
        /// Registers a new user and returns tokens and user info if registration is successful.
        /// </summary>
        /// <param name="request">Registration data (username, email, password, first name, last name).</param>
        /// <returns>JWT access token, refresh token, and user info for the newly registered user.</returns>
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<LoginResponseDTO>>> Register([FromBody] RegisterRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponseHelper.ValidationError(ModelState));

            try
            {
                var response = await _authService.RegisterAsync(request);
                return StatusCode(201, ApiResponseHelper.Created(response, ResourceTextHelper.Get("UserRegisteredSuccess")));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponseHelper.BadRequest(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseHelper.InternalServerError(ResourceTextHelper.Get("UserNotValid"), ex));
            }
        }

        /// <summary>
        /// Issues a new access token using a valid refresh token.
        /// </summary>
        /// <param name="request">Refresh token request.</param>
        /// <returns>New JWT access token, refresh token, and user info if the refresh token is valid.</returns>
        [HttpPost("refresh")]
        public async Task<ActionResult<ApiResponse<LoginResponseDTO>>> RefreshToken([FromBody] RefreshTokenRequestDTO request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponseHelper.ValidationError(ModelState));

            try
            {
                var response = await _authService.RefreshTokenAsync(request);
                return Ok(ApiResponseHelper.Success(response, ResourceTextHelper.Get("TokenRefreshedSuccess")));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponseHelper.Unauthorized(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseHelper.InternalServerError(ResourceTextHelper.Get("UserNotValid"), ex));
            }
        }

        ///// <summary>
        ///// Revokes the current access token immediately, making it unusable.
        ///// </summary>
        ///// <returns>Success message if token is revoked.</returns>
        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> Logout()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    return Unauthorized(ApiResponseHelper.Unauthorized(ResourceTextHelper.Get("InvalidUser")));

                var accessToken = Request.Headers["Authorization"]
                    .ToString()
                    .Replace("Bearer ", "");

                if (string.IsNullOrWhiteSpace(accessToken))
                    return BadRequest(ApiResponseHelper.BadRequest("Access token is missing"));

                await _authService.RevokeAccessTokenAsync(userId, accessToken);
                return Ok(ApiResponseHelper.Success(message: ResourceTextHelper.Get("TokenRevokedSuccess")));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseHelper.InternalServerError(ResourceTextHelper.Get("UserNotValid"), ex));
            }
        }

        /// <summary>
        /// Revokes a specific refresh token, making it unusable for future requests.
        /// </summary>
        /// <param name="request">Refresh token to revoke.</param>
        /// <returns>Success message if the token is revoked.</returns>
        [HttpPost("revoke-refresh-token")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> RevokeRefreshToken([FromBody] RefreshTokenRequestDTO request)
        {
            try
            {
                await _authService.RevokeTokenAsync(request.RefreshToken);
                return Ok(ApiResponseHelper.Success(message: ResourceTextHelper.Get("TokenRevokedSuccess")));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseHelper.InternalServerError(ResourceTextHelper.Get("UserNotValid"), ex));
            }
        }

        /// <summary>
        /// Logs out the current user by revoking all their refresh tokens.
        /// </summary>
        /// <returns>Success message if all tokens are revoked.</returns>
        [HttpPost("revoke-all-refresh-token")] // logout
        [Authorize]
        public async Task<ActionResult<ApiResponse>> RevokeAllRefreshToken()//Logout()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    await _authService.RevokeAllTokensAsync(userId);
                    return Ok(ApiResponseHelper.Success(message: ResourceTextHelper.Get("LogoutSuccess")));
                }
                return BadRequest(ApiResponseHelper.BadRequest(ResourceTextHelper.Get("InvalidUser")));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseHelper.InternalServerError(ResourceTextHelper.Get("UserNotValid"), ex));
            }
        }

        /// <summary>
        /// Gets information about the currently authenticated user.
        /// </summary>
        /// <returns>User info (id, username, email, first name, last name, role, last access).</returns>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserInfoDTO>>> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                    var user = await _authService.GetUserFromTokenAsync(accessToken);
                    if (user != null)
                    {
                        var response = new UserInfoDTO
                        {
                            Id = user.Id,
                            Username = user.Username,
                            Email = user.Email,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            Role = user.Role,
                            LastAccess = user.LastAccess
                        };
                        return Ok(ApiResponseHelper.Success(response, ResourceTextHelper.Get("UserAuthenticated")));
                    }
                }
                return Unauthorized(ApiResponseHelper.Unauthorized(ResourceTextHelper.Get("InvalidToken")));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseHelper.InternalServerError(ResourceTextHelper.Get("UserNotValid"), ex));
            }
        }

        /// <summary>
        /// Validates if a refresh token is still valid and active.
        /// </summary>
        /// <param name="request">Refresh token to validate.</param>
        /// <returns>True if the token is valid, false otherwise.</returns>
        [HttpPost("validate")]
        public async Task<ActionResult<ApiResponse>> ValidateToken([FromBody] RefreshTokenRequestDTO request)
        {
            try
            {
                var isValid = await _authService.ValidateTokenAsync(request.RefreshToken);
                return Ok(ApiResponseHelper.Success(new { isValid }, ResourceTextHelper.Get("TokenValidation")));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseHelper.InternalServerError(ResourceTextHelper.Get("UserNotValid"), ex));
            }
        }

        /// <summary>
        /// Updates the profile information of the authenticated user.
        /// </summary>
        /// <param name="dto">Profile data to update (email, first name, last name).</param>
        /// <returns>No content if successful.</returns>
        [HttpPut("profile")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> UpdateProfile([FromBody] UpdateUserProfileDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponseHelper.ValidationError(ModelState));
            
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(ApiResponseHelper.Unauthorized());
            
            await _authService.UpdateUserProfileAsync(userId, dto);
            return Ok(ApiResponseHelper.Success(message: ResourceTextHelper.Get("ProfileUpdatedSuccess")));
        }

        /// <summary>
        /// Changes the password of the authenticated user.
        /// </summary>
        /// <param name="dto">Current and new password data.</param>
        /// <returns>No content if successful.</returns>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> ChangePassword([FromBody] ChangePasswordDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponseHelper.ValidationError(ModelState));
            
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    return Unauthorized(ApiResponseHelper.Unauthorized());
                
                await _authService.ChangePasswordAsync(userId, dto);
                return Ok(ApiResponseHelper.Success(message: ResourceTextHelper.Get("PasswordChangedSuccess")));
            }
            catch (UnauthorizedAccessException ex)
            {
                return BadRequest(ApiResponseHelper.BadRequest(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseHelper.InternalServerError(ResourceTextHelper.Get("UserNotValid"), ex));
            }
        }

        /// <summary>
        /// Initiates the password reset process by sending a reset token to the user's email.
        /// </summary>
        /// <param name="email">User's email address.</param>
        /// <returns>Always returns 200 OK for security reasons.</returns>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse>> ForgotPassword([FromBody] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(ApiResponseHelper.BadRequest(ResourceTextHelper.Get("EmailRequired")));
            
            await _authService.RequestPasswordResetAsync(email);
            return Ok(ApiResponseHelper.Success(message: ResourceTextHelper.Get("ResetLinkSent")));
        }

        /// <summary>
        /// Resets the user's password using a valid reset token.
        /// </summary>
        /// <param name="dto">Reset token and new password data.</param>
        /// <returns>No content if successful.</returns>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse>> ResetPassword([FromBody] ResetPasswordDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponseHelper.ValidationError(ModelState));
            
            try
            {
                await _authService.ResetPasswordAsync(dto);
                return Ok(ApiResponseHelper.Success(message: ResourceTextHelper.Get("PasswordResetSuccess")));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponseHelper.BadRequest(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponseHelper.InternalServerError(ResourceTextHelper.Get("UserNotValid"), ex));
            }
        }

        /// <summary>
        /// Validates password strength without storing it. Useful for real-time validation in frontend.
        /// </summary>
        /// <param name="request">Password to validate</param>
        /// <returns>Password strength and validation details</returns>
        [HttpPost("validate-password")]
        [AllowAnonymous]
        public ActionResult<ApiResponse<object>> ValidatePasswordStrength([FromBody] ValidatePasswordRequestDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(ApiResponseHelper.BadRequest(ResourceTextHelper.Get("PasswordRequired")));

            var passwordPolicyService = new PasswordPolicyService();
            var validation = passwordPolicyService.ValidatePassword(request.Password);

            var response = new
            {
                isValid = validation.IsValid,
                score = validation.Score,
                strength = validation.Strength.ToString(),
                errors = validation.Errors,
                recommendations = GetPasswordRecommendations(validation)
            };

            return Ok(ApiResponseHelper.Success(response, ResourceTextHelper.Get("PasswordValidationCompleted")));
        }

        private List<string> GetPasswordRecommendations(PasswordValidationResult validation)
        {
            var recommendations = new List<string>();

            if (validation.Score < 60)
            {
                recommendations.Add(ResourceTextHelper.Get("PasswordRecommendation.VeryWeak.LongerPassword"));
                recommendations.Add(ResourceTextHelper.Get("PasswordRecommendation.VeryWeak.MixedCharacters"));
                recommendations.Add(ResourceTextHelper.Get("PasswordRecommendation.VeryWeak.AvoidCommon"));
            }
            else if (validation.Score < 80)
            {
                recommendations.Add(ResourceTextHelper.Get("PasswordRecommendation.Weak.Strengthen"));
                recommendations.Add(ResourceTextHelper.Get("PasswordRecommendation.Weak.MoreSpecialChars"));
            }
            else
            {
                recommendations.Add(ResourceTextHelper.Get("PasswordRecommendation.Strong.Excellent"));
            }

            return recommendations;
        }
    }

    /// <summary>
    /// DTO para validación de contraseñas
    /// </summary>
    public class ValidatePasswordRequestDTO
    {
        public string Password { get; set; } = string.Empty;
    }
}
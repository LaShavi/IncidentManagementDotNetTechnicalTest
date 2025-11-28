using System.ComponentModel.DataAnnotations;
using Application.Validation;
using Application.Helpers;

namespace Application.DTOs.Auth
{
    public class ResetPasswordDTO
    {
        [Required(ErrorMessageResourceType = typeof(ResetPasswordErrorMessages), ErrorMessageResourceName = nameof(ResetPasswordErrorMessages.EmailRequired))]
        [SecureEmail]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(ResetPasswordErrorMessages), ErrorMessageResourceName = nameof(ResetPasswordErrorMessages.TokenRequired))]
        [SecureToken]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(ResetPasswordErrorMessages), ErrorMessageResourceName = nameof(ResetPasswordErrorMessages.NewPasswordRequired))]
        [StringLength(128, MinimumLength = 8, ErrorMessageResourceType = typeof(ResetPasswordErrorMessages), ErrorMessageResourceName = nameof(ResetPasswordErrorMessages.PasswordLengthRequired))]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(ResetPasswordErrorMessages), ErrorMessageResourceName = nameof(ResetPasswordErrorMessages.PasswordConfirmationRequired))]
        [Compare("NewPassword", ErrorMessageResourceType = typeof(ResetPasswordErrorMessages), ErrorMessageResourceName = nameof(ResetPasswordErrorMessages.PasswordsDoNotMatch))]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Clase auxiliar para acceder a mensajes de error localizados en ResetPassword
    /// </summary>
    public static class ResetPasswordErrorMessages
    {
        public const string EmailRequired = nameof(EmailRequired);
        public const string TokenRequired = nameof(TokenRequired);
        public const string NewPasswordRequired = nameof(NewPasswordRequired);
        public const string PasswordLengthRequired = nameof(PasswordLengthRequired);
        public const string PasswordConfirmationRequired = nameof(PasswordConfirmationRequired);
        public const string PasswordsDoNotMatch = nameof(PasswordsDoNotMatch);
    }
}
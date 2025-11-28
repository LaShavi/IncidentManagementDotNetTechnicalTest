using System.ComponentModel.DataAnnotations;
using Application.Validation;
using Application.Helpers;

namespace Application.DTOs.Auth
{
    public class RegisterRequestDTO
    {
        [Required(ErrorMessage = nameof(ErrorMessages.UsernameRequired))]
        [SecureUsername]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.EmailRequired))]
        [SecureEmail]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.PasswordRequired))]
        [StringLength(128, MinimumLength = 8, ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.PasswordLengthRequired))]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.PasswordConfirmationRequired))]
        [Compare("Password", ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.PasswordsDoNotMatch))]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.FirstNameRequired))]
        [PersonalName]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(ErrorMessages), ErrorMessageResourceName = nameof(ErrorMessages.LastNameRequired))]
        [PersonalName]
        public string LastName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Clase auxiliar para acceder a mensajes de error localizados
    /// </summary>
    public static class ErrorMessages
    {
        public const string UsernameRequired = nameof(UsernameRequired);
        public const string EmailRequired = nameof(EmailRequired);
        public const string PasswordRequired = nameof(PasswordRequired);
        public const string PasswordLengthRequired = nameof(PasswordLengthRequired);
        public const string PasswordConfirmationRequired = nameof(PasswordConfirmationRequired);
        public const string FirstNameRequired = nameof(FirstNameRequired);
        public const string LastNameRequired = nameof(LastNameRequired);
        public const string PasswordsDoNotMatch = nameof(PasswordsDoNotMatch);
    }
}
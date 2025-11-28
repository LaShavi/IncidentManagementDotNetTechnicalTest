using Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.EmailTemplates.Models
{
    /// <summary>
    /// Modelo base para todas las plantillas de email
    /// </summary>
    public abstract class BaseEmailModel
    {
        protected readonly LocalizationSettings _localizationSettings;

        public string Username { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;  //"Hexagonal Architecture Template";
        public string SupportEmail { get; set; } = string.Empty; //"support@company.com";
        
        public DateTime CurrentDate { get; set; }

        /// <summary>
        /// Textos del footer del email
        /// </summary>
        public string AutomaticMessageText { get; set; } = string.Empty;
        public string SupportContactText { get; set; } = string.Empty;
        public string CopyRightsText { get; set; } = string.Empty;
        public string FarewellText { get; set; } = string.Empty;
        public string TeamPrefixText { get; set; } = string.Empty;

        /// <summary>
        /// Constructor que recibe la configuración de localización
        /// </summary>
        protected BaseEmailModel(IOptions<LocalizationSettings> localizationSettings)
        {
            _localizationSettings = localizationSettings.Value;
        }

        /// <summary>
        /// Constructor sin parámetros para compatibilidad con RazorLight
        /// </summary>
        protected BaseEmailModel()
        {
            _localizationSettings = new LocalizationSettings();
        }

        /// <summary>
        /// Obtiene la fecha actual en zona horaria local
        /// </summary>
        public DateTime CurrentDateLocal
        {
            get
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_localizationSettings.TimeZone.TimeZoneId);
                return TimeZoneInfo.ConvertTimeFromUtc(CurrentDate, timeZone);
            }
        }

        /// <summary>
        /// Obtiene el formato de visualización desde la configuración
        /// </summary>
        public string DisplayFormat => _localizationSettings.TimeZone.DisplayFormat;
    }

    /// <summary>
    /// Modelo para el correo de bienvenida
    /// </summary>
    public class WelcomeEmailModel : BaseEmailModel
    {
        public string WelcomeMessage { get; set; } = string.Empty;
        public string WelcomeTitle { get; set; } = string.Empty;
        public string WelcomeHeading { get; set; } = string.Empty;
        
        /// <summary>
        /// Textos de la sección de confirmación
        /// </summary>
        public string AccountCreatedTitle { get; set; } = string.Empty;
        public string FullServiceAccess { get; set; } = string.Empty;
        
        /// <summary>
        /// Textos de pasos iniciales
        /// </summary>
        public string BeginningStepsTitle { get; set; } = string.Empty;
        public string CompleteProfileStep { get; set; } = string.Empty;
        public string ExploreFeatureStep { get; set; } = string.Empty;
        public string ConfigurePreferencesStep { get; set; } = string.Empty;
        
        /// <summary>
        /// Otros textos
        /// </summary>
        public string NeedHelpMessage { get; set; } = string.Empty;
        public string EnjoyExperienceMessage { get; set; } = string.Empty;

        public WelcomeEmailModel() { }
        public WelcomeEmailModel(IOptions<LocalizationSettings> localizationSettings) : base(localizationSettings) { }
    }

    /// <summary>
    /// Modelo para el correo de recuperación de contraseña
    /// </summary>
    public class PasswordResetEmailModel : BaseEmailModel
    {
        public string ResetToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        
        public int ExpirationHours => (int)(ExpiresAt - DateTime.UtcNow).TotalHours;

        /// <summary>
        /// Textos del email de recuperación
        /// </summary>
        public string RequestReceivedMessage { get; set; } = string.Empty;
        public string TokenTitle { get; set; } = string.Empty;
        public string ImportantInfoTitle { get; set; } = string.Empty;
        public string ExpirationWarningText { get; set; } = string.Empty;
        public string SingleUseOnlyText { get; set; } = string.Empty;
        public string IgnoreIfNotRequestedText { get; set; } = string.Empty;
        public string HowToResetText { get; set; } = string.Empty;
        public string SecurityConcernText { get; set; } = string.Empty;
        public string ResetTitle { get; set; } = string.Empty;
        public string ResetHeading { get; set; } = string.Empty;
        public string GreetingText { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene la fecha de expiración en zona horaria local
        /// </summary>
        public DateTime ExpiresAtLocal
        {
            get
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_localizationSettings?.TimeZone.TimeZoneId ?? "SA Pacific Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(ExpiresAt, timeZone);
            }
        }

        public PasswordResetEmailModel() { }
        public PasswordResetEmailModel(IOptions<LocalizationSettings> localizationSettings) : base(localizationSettings) { }
    }

    /// <summary>
    /// Modelo para el correo de confirmación de email
    /// </summary>
    public class EmailConfirmationModel : BaseEmailModel
    {
        public string ConfirmationToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        
        public int ExpirationHours => (int)(ExpiresAt - DateTime.UtcNow).TotalHours;

        /// <summary>
        /// Textos del email de confirmación
        /// </summary>
        public string ConfirmEmailRequestText { get; set; } = string.Empty;
        public string ConfirmationTokenTitle { get; set; } = string.Empty;
        public string UseTokenInstructionsText { get; set; } = string.Empty;
        public string TokenExpirationText { get; set; } = string.Empty;
        public string RequestNewTokenText { get; set; } = string.Empty;
        public string FullAccessText { get; set; } = string.Empty;
        public string IgnoreIfNotYouText { get; set; } = string.Empty;
        public string ConfirmationTitle { get; set; } = string.Empty;
        public string ConfirmationHeading { get; set; } = string.Empty;
        public string GreetingText { get; set; } = string.Empty;

        /// <summary>
        /// Obtiene la fecha de expiración en zona horaria local
        /// </summary>
        public DateTime ExpiresAtLocal
        {
            get
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_localizationSettings?.TimeZone.TimeZoneId ?? "SA Pacific Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(ExpiresAt, timeZone);
            }
        }

        public EmailConfirmationModel() { }
        public EmailConfirmationModel(IOptions<LocalizationSettings> localizationSettings) : base(localizationSettings) { }
    }

    /// <summary>
    /// Modelo para notificación de cambio de contraseña
    /// </summary>
    public class PasswordChangedNotificationModel : BaseEmailModel
    {
        public DateTime ChangeDate { get; set; }

        /// <summary>
        /// Textos de notificación de cambio de contraseña
        /// </summary>
        public string NotificationTitleText { get; set; } = string.Empty;
        public string ConfirmationMessageText { get; set; } = string.Empty;
        public string ChangeConfirmedText { get; set; } = string.Empty;
        public string AccountProtectedText { get; set; } = string.Empty;
        public string NotYouText { get; set; } = string.Empty;
        public string UnauthorizedAccessText { get; set; } = string.Empty;
        public string AdditionalSecurityMeasuresText { get; set; } = string.Empty;
        public string UseStrongPasswordsText { get; set; } = string.Empty;
        public string Enable2FAText { get; set; } = string.Empty;
        public string DontShareCredentialsText { get; set; } = string.Empty;
        public string PasswordChangedTitle { get; set; } = string.Empty;
        public string GreetingText { get; set; } = string.Empty;
        public string ContactSupportText { get; set; } = string.Empty;
        public string ChangePasswordASAPText { get; set; } = string.Empty;
        public string ReviewRecentActivityText { get; set; } = string.Empty;

        public DateTime ChangeDateLocal
        {
            get
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_localizationSettings?.TimeZone.TimeZoneId ?? "SA Pacific Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(ChangeDate, timeZone);
            }
        }

        public PasswordChangedNotificationModel() { }
        public PasswordChangedNotificationModel(IOptions<LocalizationSettings> localizationSettings) : base(localizationSettings) { }
    }

    /// <summary>
    /// Modelo para notificación de actualización de perfil
    /// </summary>
    public class ProfileUpdatedNotificationModel : BaseEmailModel
    {
        public DateTime UpdateDate { get; set; }

        /// <summary>
        /// Textos de notificación de actualización de perfil
        /// </summary>
        public string ProfileUpdatedText { get; set; } = string.Empty;
        public string ConfirmationMessageText { get; set; } = string.Empty;
        public string UpdateConfirmedText { get; set; } = string.Empty;
        public string ChangesActiveText { get; set; } = string.Empty;
        public string NotYouText { get; set; } = string.Empty;
        public string UnauthorizedUpdateText { get; set; } = string.Empty;
        public string ReviewAccountText { get; set; } = string.Empty;
        public string ChangePasswordText { get; set; } = string.Empty;
        public string ContactSupportText { get; set; } = string.Empty;
        public string UpdatedTitle { get; set; } = string.Empty;
        public string GreetingText { get; set; } = string.Empty;
        public string RememberUpdateText { get; set; } = string.Empty;
        public string AnyQuestionText { get; set; } = string.Empty;

        public DateTime UpdateDateLocal
        {
            get
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_localizationSettings?.TimeZone.TimeZoneId ?? "SA Pacific Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(UpdateDate, timeZone);
            }
        }

        public ProfileUpdatedNotificationModel() { }
        public ProfileUpdatedNotificationModel(IOptions<LocalizationSettings> localizationSettings) : base(localizationSettings) { }
    }

    /// <summary>
    /// Modelo para notificación de cuenta bloqueada
    /// </summary>
    public class AccountLockedNotificationModel : BaseEmailModel
    {
        public DateTime? LockedUntil { get; set; }

        /// <summary>
        /// Textos de notificación de cuenta bloqueada
        /// </summary>
        public string AccountLockedText { get; set; } = string.Empty;
        public string LockReasonText { get; set; } = string.Empty;
        public string SecurityMeasureText { get; set; } = string.Empty;
        public string ProtectionReasonText { get; set; } = string.Empty;
        public string WhatToDoText { get; set; } = string.Empty;
        public string WaitForUnlockText { get; set; } = string.Empty;
        public string ContactSupportForUnlockText { get; set; } = string.Empty;
        public string VerifyCredentialsText { get; set; } = string.Empty;
        public string NotYouText { get; set; } = string.Empty;
        public string UnauthorizedAttemptText { get; set; } = string.Empty;
        public string LockedTitle { get; set; } = string.Empty;
        public string GreetingText { get; set; } = string.Empty;
        public string BlockedIntroText { get; set; } = string.Empty;
        public string ChangePasswordAfterUnlockText { get; set; } = string.Empty;
        public string ReviewActivityText { get; set; } = string.Empty;
        public string ContactSecurityTeamText { get; set; } = string.Empty;
        public string ApologyMessageText { get; set; } = string.Empty;

        /// <summary>
        /// Texto formateado con la fecha de desbloqueo en zona horaria local
        /// </summary>
        public string LockedUntilText
        {
            get
            {
                if (!LockedUntil.HasValue)
                {
                    return "temporalmente";
                }

                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_localizationSettings?.TimeZone.TimeZoneId ?? "SA Pacific Standard Time");
                var localDate = TimeZoneInfo.ConvertTimeFromUtc(LockedUntil.Value, timeZone);
                return $"hasta {localDate.ToString(_localizationSettings?.TimeZone.DisplayFormat ?? "dd/MM/yyyy HH:mm")}";
            }
        }

        public AccountLockedNotificationModel() { }
        public AccountLockedNotificationModel(IOptions<LocalizationSettings> localizationSettings) : base(localizationSettings) { }
    }

    /// <summary>
    /// Modelo para notificación de cuenta eliminada
    /// </summary>
    public class AccountDeletedNotificationModel : BaseEmailModel
    {
        public DateTime DeletionDate { get; set; }

        /// <summary>
        /// Textos de notificación de cuenta eliminada
        /// </summary>
        public string AccountDeletedText { get; set; } = string.Empty;
        public string DeletionConfirmationText { get; set; } = string.Empty;
        public string DeletionConfirmedText { get; set; } = string.Empty;
        public string DataRemovedText { get; set; } = string.Empty;
        public string NotYouText { get; set; } = string.Empty;
        public string UnauthorizedDeletionText { get; set; } = string.Empty;
        public string ContactImmediatelyText { get; set; } = string.Empty;
        public string WhatMeansDeletionText { get; set; } = string.Empty;
        public string NoAccessText { get; set; } = string.Empty;
        public string PermanentDeletionText { get; set; } = string.Empty;
        public string RecreateAccountText { get; set; } = string.Empty;
        public string DeletedTitle { get; set; } = string.Empty;
        public string GreetingText { get; set; } = string.Empty;
        public string ProvideDetailsText { get; set; } = string.Empty;
        public string VerifyOtherServicesText { get; set; } = string.Empty;
        public string QuestionAboutDeletionText { get; set; } = string.Empty;

        public DateTime DeletionDateLocal
        {
            get
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(
                    _localizationSettings?.TimeZone.TimeZoneId ?? "SA Pacific Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DeletionDate, timeZone);
            }
        }

        public AccountDeletedNotificationModel() { }
        public AccountDeletedNotificationModel(IOptions<LocalizationSettings> localizationSettings) : base(localizationSettings) { }
    }
}
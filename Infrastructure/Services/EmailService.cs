using Application.Ports;
using Application.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RazorLight;
using System.Net;
using System.Net.Mail;
using Infrastructure.Services.EmailTemplates.Models;
using System.Web;
using Infrastructure.Helpers;
using Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services
{
    /// <summary>
    /// Email service implementation using SMTP with RazorLight templates.
    /// Encapsula el envío de correos electrónicos profesionales para distintos escenarios de usuario.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly IRazorLightEngine _razorEngine;
        private readonly TimeZoneHelper _timeZoneHelper;
        private readonly IOptions<LocalizationSettings> _localizationSettings;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly bool _enableSsl;
        private readonly string _companyName;
        private readonly string _supportEmail;

        /// <summary>
        /// Inicializa el servicio de email con configuración SMTP y RazorLight.
        /// </summary>
        public EmailService(
            IConfiguration configuration,
            ILogger<EmailService> logger,
            IRazorLightEngine razorEngine,
            TimeZoneHelper timeZoneHelper,
            IOptions<LocalizationSettings> localizationSettings)
        {
            _configuration = configuration;
            _logger = logger;
            _razorEngine = razorEngine;
            _timeZoneHelper = timeZoneHelper;
            _localizationSettings = localizationSettings;

            // Carga la configuración SMTP desde appsettings.json
            var emailSettings = _configuration.GetSection("EmailSettings");
            _smtpHost = emailSettings["SmtpHost"] ?? throw new ArgumentNullException("EmailSettings:SmtpHost");
            _smtpPort = int.Parse(emailSettings["SmtpPort"] ?? "587");
            _smtpUsername = emailSettings["SmtpUsername"] ?? throw new ArgumentNullException("EmailSettings:SmtpUsername");
            _smtpPassword = emailSettings["SmtpPassword"] ?? throw new ArgumentNullException("EmailSettings:SmtpPassword");
            _fromEmail = emailSettings["FromEmail"] ?? throw new ArgumentNullException("EmailSettings:FromEmail");
            _fromName = emailSettings["FromName"] ?? "Hexagonal Architecture Template";
            _enableSsl = bool.Parse(emailSettings["EnableSsl"] ?? "true");

            _companyName = emailSettings["CompanyName"] ?? "Hexagonal Architecture Template";
            _supportEmail = emailSettings["SupportEmail"] ?? "support@company.com";

            _logger.LogDebug("EmailService initialized with RazorLight template engine");
        }

        /// <summary>
        /// Envia un correo electrónico básico con asunto y cuerpo.
        /// </summary>
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                _logger.LogInformation("Sending email to {Email} with subject: {Subject}", to, subject);

                using var client = CreateSmtpClient();
                using var message = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName, System.Text.Encoding.UTF8),
                    Subject = subject,
                    SubjectEncoding = System.Text.Encoding.UTF8,
                    Body = body,
                    BodyEncoding = System.Text.Encoding.UTF8,
                    IsBodyHtml = true
                };

                message.To.Add(to);
                await client.SendMailAsync(message);

                _logger.LogInformation("Email sent successfully to {Email}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", to);
                throw;
            }
        }

        /// <summary>
        /// Envía un correo con el token de recuperación de contraseña usando plantilla profesional.
        /// </summary>
        public async Task SendPasswordResetEmailAsync(string to, string username, string resetToken)
        {
            try
            {
                _logger.LogDebug("Rendering password reset email template for user: {Username}", username);

                var expiresAt = DateTime.UtcNow.AddHours(24);
                var expirationHours = (int)(expiresAt - DateTime.UtcNow).TotalHours;
                
                var model = new PasswordResetEmailModel(_localizationSettings)
                {
                    Username = SanitizeInput(username),
                    ResetToken = SanitizeInput(resetToken),
                    ExpiresAt = expiresAt,
                    CurrentDate = DateTime.UtcNow,
                    CompanyName = _companyName,
                    SupportEmail = _supportEmail,

                    // Textos del email de recuperación
                    RequestReceivedMessage = ResourceTextHelper.Get("EmailPasswordReset.RequestReceived"),
                    TokenTitle = ResourceTextHelper.Get("EmailPasswordReset.TokenTitle"),
                    ImportantInfoTitle = ResourceTextHelper.Get("EmailPasswordReset.ImportantInfo"),
                    ExpirationWarningText = string.Format(ResourceTextHelper.Get("EmailPasswordReset.ExpirationWarning"), expirationHours),
                    SingleUseOnlyText = ResourceTextHelper.Get("EmailPasswordReset.SingleUseOnly"),
                    IgnoreIfNotRequestedText = ResourceTextHelper.Get("EmailPasswordReset.IgnoreIfNotRequested"),
                    HowToResetText = ResourceTextHelper.Get("EmailPasswordReset.HowToReset"),
                    SecurityConcernText = ResourceTextHelper.Get("EmailPasswordReset.SecurityConcern"),
                    ResetTitle = ResourceTextHelper.Get("EmailPasswordReset.Title"),
                    ResetHeading = ResourceTextHelper.Get("EmailPasswordReset.Heading"),
                    GreetingText = ResourceTextHelper.Get("EmailCommon.Greeting"),
                    
                    // Footer texts
                    AutomaticMessageText = ResourceTextHelper.Get("EmailLayout.AutomaticMessage"),
                    SupportContactText = string.Format(ResourceTextHelper.Get("EmailLayout.SupportContact"), _supportEmail),
                    CopyRightsText = ResourceTextHelper.Get("EmailLayout.CopyRights"),
                    FarewellText = ResourceTextHelper.Get("EmailLayout.Farewell"),
                    TeamPrefixText = ResourceTextHelper.Get("EmailLayout.TeamPrefix")
                };

                var body = await _razorEngine.CompileRenderAsync("PasswordResetEmail.cshtml", model);
                var subject = ResourceTextHelper.Get("PasswordResetSubject");

                await SendEmailAsync(to, subject, body);
                _logger.LogInformation("Password reset email sent to {Email} for user {Username}", to, username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to render or send password reset email to {Email}", to);
                throw;
            }
        }

        /// <summary>
        /// Envía un correo de bienvenida al usuario recién registrado usando plantilla profesional.
        /// </summary>
        public async Task SendWelcomeEmailAsync(string to, string username)
        {
            try
            {
                _logger.LogDebug("Rendering welcome email template for user: {Username}", username);

                var model = new WelcomeEmailModel(_localizationSettings)
                {
                    Username = SanitizeInput(username),
                    CurrentDate = DateTime.UtcNow,
                    CompanyName = _companyName,
                    SupportEmail = _supportEmail,

                    // Textos de bienvenida
                    WelcomeMessage = ResourceTextHelper.Get("EmailWelcome.WelcomeMessage"),
                    WelcomeTitle = ResourceTextHelper.Get("EmailWelcome.WelcomeTitle"),
                    WelcomeHeading = ResourceTextHelper.Get("EmailWelcome.WelcomeHeading"),
                    AccountCreatedTitle = ResourceTextHelper.Get("EmailWelcome.AccountCreatedTitle"),
                    FullServiceAccess = ResourceTextHelper.Get("EmailWelcome.FullServiceAccess"),
                    BeginningStepsTitle = ResourceTextHelper.Get("EmailWelcome.BeginningStepsTitle"),
                    CompleteProfileStep = ResourceTextHelper.Get("EmailWelcome.CompleteProfile"),
                    ExploreFeatureStep = ResourceTextHelper.Get("EmailWelcome.ExploreFeatures"),
                    ConfigurePreferencesStep = ResourceTextHelper.Get("EmailWelcome.ConfigurePreferences"),
                    NeedHelpMessage = ResourceTextHelper.Get("EmailWelcome.NeedHelp"),
                    EnjoyExperienceMessage = ResourceTextHelper.Get("EmailWelcome.EnjoyExperience"),

                    // Footer texts
                    AutomaticMessageText = ResourceTextHelper.Get("EmailLayout.AutomaticMessage"),
                    SupportContactText = string.Format(ResourceTextHelper.Get("EmailLayout.SupportContact"), _supportEmail),
                    CopyRightsText = ResourceTextHelper.Get("EmailLayout.CopyRights"),
                    FarewellText = ResourceTextHelper.Get("EmailLayout.Farewell"),
                    TeamPrefixText = ResourceTextHelper.Get("EmailLayout.TeamPrefix")
                };

                var body = await _razorEngine.CompileRenderAsync("WelcomeEmail.cshtml", model);
                var subject = ResourceTextHelper.Get("WelcomeEmailSubject");

                await SendEmailAsync(to, subject, body);
                _logger.LogInformation("Welcome email sent to {Email} for user {Username}", to, username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to render or send welcome email to {Email}", to);
                throw;
            }
        }
        

        /// Envía un correo para confirmar la dirección de email del usuario usando plantilla profesional.
        /// </summary>
        public async Task SendEmailConfirmationAsync(string to, string username, string confirmationToken)
        {
            try
            {
                _logger.LogDebug("Rendering email confirmation template for user: {Username}", username);

                var expiresAt = DateTime.UtcNow.AddHours(48);
                var expirationHours = (int)(expiresAt - DateTime.UtcNow).TotalHours;

                var model = new EmailConfirmationModel(_localizationSettings)
                {
                    Username = SanitizeInput(username),
                    ConfirmationToken = SanitizeInput(confirmationToken),
                    ExpiresAt = expiresAt,
                    CurrentDate = DateTime.UtcNow,
                    CompanyName = _companyName,
                    SupportEmail = _supportEmail,

                    // Textos de confirmación de email
                    ConfirmEmailRequestText = ResourceTextHelper.Get("EmailConfirmation.ConfirmEmailRequest"),
                    ConfirmationTokenTitle = ResourceTextHelper.Get("EmailConfirmation.ConfirmationTokenTitle"),
                    UseTokenInstructionsText = ResourceTextHelper.Get("EmailConfirmation.UseTokenInstructions"),
                    TokenExpirationText = string.Format(ResourceTextHelper.Get("EmailConfirmation.TokenExpiration"), expirationHours),
                    RequestNewTokenText = ResourceTextHelper.Get("EmailConfirmation.RequestNewToken"),
                    FullAccessText = ResourceTextHelper.Get("EmailConfirmation.FullAccess"),
                    IgnoreIfNotYouText = ResourceTextHelper.Get("EmailConfirmation.IgnoreIfNotYou"),
                    ConfirmationTitle = ResourceTextHelper.Get("EmailConfirmation.Title"),
                    ConfirmationHeading = ResourceTextHelper.Get("EmailConfirmation.Heading"),
                    GreetingText = ResourceTextHelper.Get("EmailCommon.Greeting"),

                    // Footer texts
                    AutomaticMessageText = ResourceTextHelper.Get("EmailLayout.AutomaticMessage"),
                    SupportContactText = string.Format(ResourceTextHelper.Get("EmailLayout.SupportContact"), _supportEmail),
                    CopyRightsText = ResourceTextHelper.Get("EmailLayout.CopyRights"),
                    FarewellText = ResourceTextHelper.Get("EmailLayout.Farewell"),
                    TeamPrefixText = ResourceTextHelper.Get("EmailLayout.TeamPrefix")
                };

                var body = await _razorEngine.CompileRenderAsync("EmailConfirmation.cshtml", model);
                var subject = ResourceTextHelper.Get("EmailConfirmationSubject");

                await SendEmailAsync(to, subject, body);
                _logger.LogInformation("Email confirmation sent to {Email} for user {Username}", to, username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to render or send email confirmation to {Email}", to);
                throw;
            }
        }

        /// <summary>
        /// Envía una notificación cuando la contraseña del usuario ha sido cambiada usando plantilla profesional.
        /// </summary>
        public async Task SendPasswordChangedNotificationAsync(string to, string username)
        {
            try
            {
                _logger.LogDebug("Rendering password changed notification template for user: {Username}", username);

                var changeDate = DateTime.UtcNow;
                var changeDateLocal = TimeZoneInfo.ConvertTimeFromUtc(changeDate, TimeZoneInfo.FindSystemTimeZoneById(_localizationSettings.Value.TimeZone.TimeZoneId ?? "SA Pacific Standard Time"));
                
                var model = new PasswordChangedNotificationModel(_localizationSettings)
                {
                    Username = SanitizeInput(username),
                    ChangeDate = changeDate,
                    CurrentDate = DateTime.UtcNow,
                    CompanyName = _companyName,
                    SupportEmail = _supportEmail,

                    // Textos de cambio de contraseña
                    NotificationTitleText = ResourceTextHelper.Get("EmailPasswordChanged.NotificationTitle"),
                    ConfirmationMessageText = string.Format(ResourceTextHelper.Get("EmailPasswordChanged.ConfirmationMessage"), changeDateLocal.ToString("dd/MM/yyyy"), changeDateLocal.ToString("HH:mm")),
                    ChangeConfirmedText = ResourceTextHelper.Get("EmailPasswordChanged.ChangeConfirmed"),
                    AccountProtectedText = ResourceTextHelper.Get("EmailPasswordChanged.AccountProtected"),
                    NotYouText = ResourceTextHelper.Get("EmailPasswordChanged.NotYou"),
                    UnauthorizedAccessText = ResourceTextHelper.Get("EmailPasswordChanged.UnauthorizedAccess"),
                    AdditionalSecurityMeasuresText = ResourceTextHelper.Get("EmailPasswordChanged.AdditionalSecurityMeasures"),
                    UseStrongPasswordsText = ResourceTextHelper.Get("EmailPasswordChanged.UseStrongPasswords"),
                    Enable2FAText = ResourceTextHelper.Get("EmailPasswordChanged.Enable2FA"),
                    DontShareCredentialsText = ResourceTextHelper.Get("EmailPasswordChanged.DontShareCredentials"),
                    PasswordChangedTitle = ResourceTextHelper.Get("EmailPasswordChanged.Title"),
                    GreetingText = ResourceTextHelper.Get("EmailCommon.Greeting"),
                    ContactSupportText = ResourceTextHelper.Get("EmailPasswordChanged.ContactSupport"),
                    ChangePasswordASAPText = ResourceTextHelper.Get("EmailPasswordChanged.ChangePasswordASAP"),
                    ReviewRecentActivityText = ResourceTextHelper.Get("EmailPasswordChanged.ReviewRecentActivity"),

                    // Footer texts
                    AutomaticMessageText = ResourceTextHelper.Get("EmailLayout.AutomaticMessage"),
                    SupportContactText = string.Format(ResourceTextHelper.Get("EmailLayout.SupportContact"), _supportEmail),
                    CopyRightsText = ResourceTextHelper.Get("EmailLayout.CopyRights"),
                    FarewellText = ResourceTextHelper.Get("EmailLayout.Farewell"),
                    TeamPrefixText = ResourceTextHelper.Get("EmailLayout.TeamPrefix")
                };

                var body = await _razorEngine.CompileRenderAsync("PasswordChangedNotification.cshtml", model);
                var subject = ResourceTextHelper.Get("PasswordChangedSubject");

                await SendEmailAsync(to, subject, body);
                _logger.LogInformation("Password changed notification sent to {Email} for user {Username}", to, username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to render or send password changed notification to {Email}", to);
                throw;
            }
        }

        /// <summary>
        /// Envía una notificación cuando el perfil del usuario ha sido actualizado usando plantilla profesional.
        /// </summary>
        public async Task SendProfileUpdatedNotificationAsync(string to, string username)
        {
            try
            {
                _logger.LogDebug("Rendering profile updated notification template for user: {Username}", username);

                var updateDate = DateTime.UtcNow;
                var updateDateLocal = TimeZoneInfo.ConvertTimeFromUtc(updateDate, TimeZoneInfo.FindSystemTimeZoneById(_localizationSettings.Value.TimeZone.TimeZoneId ?? "SA Pacific Standard Time"));
                
                var model = new ProfileUpdatedNotificationModel(_localizationSettings)
                {
                    Username = SanitizeInput(username),
                    UpdateDate = updateDate,
                    CurrentDate = DateTime.UtcNow,
                    CompanyName = _companyName,
                    SupportEmail = _supportEmail,

                    // Textos de actualización de perfil
                    ProfileUpdatedText = ResourceTextHelper.Get("EmailProfileUpdated.ProfileUpdated"),
                    ConfirmationMessageText = string.Format(ResourceTextHelper.Get("EmailProfileUpdated.ConfirmationMessage"), updateDateLocal.ToString("dd/MM/yyyy"), updateDateLocal.ToString("HH:mm")),
                    UpdateConfirmedText = ResourceTextHelper.Get("EmailProfileUpdated.UpdateConfirmed"),
                    ChangesActiveText = ResourceTextHelper.Get("EmailProfileUpdated.ChangesActive"),
                    NotYouText = ResourceTextHelper.Get("EmailProfileUpdated.NotYou"),
                    UnauthorizedUpdateText = ResourceTextHelper.Get("EmailProfileUpdated.UnauthorizedUpdate"),
                    ReviewAccountText = ResourceTextHelper.Get("EmailProfileUpdated.ReviewAccount"),
                    ChangePasswordText = ResourceTextHelper.Get("EmailProfileUpdated.ChangePassword"),
                    ContactSupportText = ResourceTextHelper.Get("EmailProfileUpdated.ContactSupport"),
                    UpdatedTitle = ResourceTextHelper.Get("EmailProfileUpdated.Title"),
                    GreetingText = ResourceTextHelper.Get("EmailCommon.Greeting"),
                    RememberUpdateText = ResourceTextHelper.Get("EmailProfileUpdated.RememberUpdate"),
                    AnyQuestionText = ResourceTextHelper.Get("EmailProfileUpdated.AnyQuestion"),

                    // Footer texts
                    AutomaticMessageText = ResourceTextHelper.Get("EmailLayout.AutomaticMessage"),
                    SupportContactText = string.Format(ResourceTextHelper.Get("EmailLayout.SupportContact"), _supportEmail),
                    CopyRightsText = ResourceTextHelper.Get("EmailLayout.CopyRights"),
                    FarewellText = ResourceTextHelper.Get("EmailLayout.Farewell"),
                    TeamPrefixText = ResourceTextHelper.Get("EmailLayout.TeamPrefix")
                };

                var body = await _razorEngine.CompileRenderAsync("ProfileUpdatedNotification.cshtml", model);
                var subject = ResourceTextHelper.Get("ProfileUpdatedSubject");

                await SendEmailAsync(to, subject, body);
                _logger.LogInformation("Profile updated notification sent to {Email} for user {Username}", to, username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to render or send profile updated notification to {Email}", to);
                throw;
            }
        }

        /// <summary>
        /// Envía una notificación cuando la cuenta del usuario ha sido bloqueada usando plantilla profesional.
        /// </summary>
        public async Task SendAccountLockedNotificationAsync(string to, string username, DateTime? lockedUntil)
        {
            try
            {
                _logger.LogDebug("Rendering account locked notification template for user: {Username}", username);

                var model = new AccountLockedNotificationModel(_localizationSettings)
                {
                    Username = SanitizeInput(username),
                    LockedUntil = lockedUntil,
                    CurrentDate = DateTime.UtcNow,
                    CompanyName = _companyName,
                    SupportEmail = _supportEmail,

                    // Textos de cuenta bloqueada
                    AccountLockedText = ResourceTextHelper.Get("EmailAccountLocked.AccountLocked"),
                    LockReasonText = ResourceTextHelper.Get("EmailAccountLocked.LockReason"),
                    SecurityMeasureText = ResourceTextHelper.Get("EmailAccountLocked.SecurityMeasure"),
                    ProtectionReasonText = ResourceTextHelper.Get("EmailAccountLocked.ProtectionReason"),
                    WhatToDoText = ResourceTextHelper.Get("EmailAccountLocked.WhatToDo"),
                    WaitForUnlockText = ResourceTextHelper.Get("EmailAccountLocked.WaitForUnlock"),
                    ContactSupportForUnlockText = ResourceTextHelper.Get("EmailAccountLocked.ContactSupportForUnlock"),
                    VerifyCredentialsText = ResourceTextHelper.Get("EmailAccountLocked.VerifyCredentials"),
                    NotYouText = ResourceTextHelper.Get("EmailAccountLocked.NotYou"),
                    UnauthorizedAttemptText = ResourceTextHelper.Get("EmailAccountLocked.UnauthorizedAttempt"),
                    LockedTitle = ResourceTextHelper.Get("EmailAccountLocked.Title"),
                    GreetingText = ResourceTextHelper.Get("EmailCommon.Greeting"),
                    BlockedIntroText = ResourceTextHelper.Get("EmailAccountLocked.BlockedIntro"),
                    ChangePasswordAfterUnlockText = ResourceTextHelper.Get("EmailAccountLocked.ChangePasswordAfterUnlock"),
                    ReviewActivityText = ResourceTextHelper.Get("EmailAccountLocked.ReviewActivity"),
                    ContactSecurityTeamText = ResourceTextHelper.Get("EmailAccountLocked.ContactSecurityTeam"),
                    ApologyMessageText = ResourceTextHelper.Get("EmailAccountLocked.ApologyMessage"),

                    // Footer texts
                    AutomaticMessageText = ResourceTextHelper.Get("EmailLayout.AutomaticMessage"),
                    SupportContactText = string.Format(ResourceTextHelper.Get("EmailLayout.SupportContact"), _supportEmail),
                    CopyRightsText = ResourceTextHelper.Get("EmailLayout.CopyRights"),
                    FarewellText = ResourceTextHelper.Get("EmailLayout.Farewell"),
                    TeamPrefixText = ResourceTextHelper.Get("EmailLayout.TeamPrefix")
                };

                var body = await _razorEngine.CompileRenderAsync("AccountLockedNotification.cshtml", model);
                var subject = ResourceTextHelper.Get("AccountLockedSubject");

                await SendEmailAsync(to, subject, body);
                _logger.LogInformation("Account locked notification sent to {Email} for user {Username}", to, username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to render or send account locked notification to {Email}", to);
                throw;
            }
        }

        /// <summary>
        /// Envía una notificación cuando la cuenta del usuario ha sido eliminada usando plantilla profesional.
        /// </summary>
        public async Task SendAccountDeletedNotificationAsync(string to, string username)
        {
            try
            {
                _logger.LogDebug("Rendering account deleted notification template for user: {Username}", username);

                var deletionDate = DateTime.UtcNow;
                var deletionDateLocal = TimeZoneInfo.ConvertTimeFromUtc(deletionDate, TimeZoneInfo.FindSystemTimeZoneById(_localizationSettings.Value.TimeZone.TimeZoneId ?? "SA Pacific Standard Time"));
                
                var model = new AccountDeletedNotificationModel(_localizationSettings)
                {
                    Username = SanitizeInput(username),
                    DeletionDate = deletionDate,
                    CurrentDate = DateTime.UtcNow,
                    CompanyName = _companyName,
                    SupportEmail = _supportEmail,

                    // Textos de cuenta eliminada
                    AccountDeletedText = ResourceTextHelper.Get("EmailAccountDeleted.AccountDeleted"),
                    DeletionConfirmationText = string.Format(ResourceTextHelper.Get("EmailAccountDeleted.DeletionConfirmation"), deletionDateLocal.ToString("dd/MM/yyyy"), deletionDateLocal.ToString("HH:mm")),
                    DeletionConfirmedText = ResourceTextHelper.Get("EmailAccountDeleted.DeletionConfirmed"),
                    DataRemovedText = ResourceTextHelper.Get("EmailAccountDeleted.DataRemoved"),
                    NotYouText = ResourceTextHelper.Get("EmailAccountDeleted.NotYou"),
                    UnauthorizedDeletionText = ResourceTextHelper.Get("EmailAccountDeleted.UnauthorizedDeletion"),
                    ContactImmediatelyText = ResourceTextHelper.Get("EmailAccountDeleted.ContactImmediately"),
                    WhatMeansDeletionText = ResourceTextHelper.Get("EmailAccountDeleted.WhatMeansDeletion"),
                    NoAccessText = ResourceTextHelper.Get("EmailAccountDeleted.NoAccess"),
                    PermanentDeletionText = ResourceTextHelper.Get("EmailAccountDeleted.PermanentDeletion"),
                    RecreateAccountText = ResourceTextHelper.Get("EmailAccountDeleted.RecreateAccount"),
                    DeletedTitle = ResourceTextHelper.Get("EmailAccountDeleted.Title"),
                    GreetingText = ResourceTextHelper.Get("EmailCommon.Greeting"),
                    ProvideDetailsText = ResourceTextHelper.Get("EmailAccountDeleted.ProvideDetails"),
                    VerifyOtherServicesText = ResourceTextHelper.Get("EmailAccountDeleted.VerifyOtherServices"),
                    QuestionAboutDeletionText = ResourceTextHelper.Get("EmailAccountDeleted.QuestionAboutDeletion"),

                    // Footer texts
                    AutomaticMessageText = ResourceTextHelper.Get("EmailLayout.AutomaticMessage"),
                    SupportContactText = string.Format(ResourceTextHelper.Get("EmailLayout.SupportContact"), _supportEmail),
                    CopyRightsText = ResourceTextHelper.Get("EmailLayout.CopyRights"),
                    FarewellText = ResourceTextHelper.Get("EmailLayout.Farewell"),
                    TeamPrefixText = ResourceTextHelper.Get("EmailLayout.TeamPrefix")
                };

                var body = await _razorEngine.CompileRenderAsync("AccountDeletedNotification.cshtml", model);
                var subject = ResourceTextHelper.Get("AccountDeletedSubject");

                await SendEmailAsync(to, subject, body);
                _logger.LogInformation("Account deleted notification sent to {Email} for user {Username}", to, username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to render or send account deleted notification to {Email}", to);
                throw;
            }
        }

        /// <summary>
        /// Sanitiza la entrada del usuario para prevenir inyección HTML/XSS.
        /// </summary>
        /// <param name="input">Texto de entrada del usuario</param>
        /// <returns>Texto sanitizado y seguro</returns>
        private static string SanitizeInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Escapar caracteres HTML peligrosos
            return HttpUtility.HtmlEncode(input.Trim());
        }

        /// <summary>
        /// Crea y configura el cliente SMTP para el envío de correos.
        /// </summary>
        private SmtpClient CreateSmtpClient()
        {
            var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = _enableSsl,
                Timeout = 30000 // 30 seconds timeout
            };

            return client;
        }
    }
}
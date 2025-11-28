namespace Application.Ports
{
    public interface IEmailService
    {
        /// <summary>
        /// Sends a basic email with subject and body
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body content</param>
        /// <returns>Task representing the async operation</returns>
        Task SendEmailAsync(string to, string subject, string body);

        /// <summary>
        /// Sends a password reset email with token
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="username">Username for personalization</param>
        /// <param name="resetToken">Password reset token</param>
        /// <returns>Task representing the async operation</returns>
        Task SendPasswordResetEmailAsync(string to, string username, string resetToken);

        /// <summary>
        /// Sends a welcome email to new users
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="username">Username for personalization</param>
        /// <returns>Task representing the async operation</returns>
        Task SendWelcomeEmailAsync(string to, string username);

        /// <summary>
        /// Sends an email confirmation link to verify email address
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="username">Username for personalization</param>
        /// <param name="confirmationToken">Email confirmation token</param>
        /// <returns>Task representing the async operation</returns>
        Task SendEmailConfirmationAsync(string to, string username, string confirmationToken);

        /// <summary>
        /// Sends a notification email when the password is changed
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="username">Username for personalization</param>
        /// <returns>Task representing the async operation</returns>
        Task SendPasswordChangedNotificationAsync(string to, string username);

        /// <summary>
        /// Sends a notification email when the profile is updated
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="username">Username for personalization</param>
        /// <returns>Task representing the async operation</returns>
        Task SendProfileUpdatedNotificationAsync(string to, string username);

        /// <summary>
        /// Sends a notification email when the account is locked
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="username">Username for personalization</param>
        /// <param name="lockedUntil">Optional locked until date</param>
        /// <returns>Task representing the async operation</returns>
        Task SendAccountLockedNotificationAsync(string to, string username, DateTime? lockedUntil);

        /// <summary>
        /// Sends a notification email when the account is deleted
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="username">Username for personalization</param>
        /// <returns>Task representing the async operation</returns>
        Task SendAccountDeletedNotificationAsync(string to, string username);
    }
}
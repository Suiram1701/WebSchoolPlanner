using Microsoft.AspNetCore.Identity;

namespace WebSchoolPlanner.IdentityProviders.EmailSenders;

/// <summary>
/// A implementation that only provides views rendering
/// </summary>
/// <typeparam name="TUser">The type of the user</typeparam>
public abstract class EmailSenderBase<TUser> : IEmailSender<TUser>
    where TUser : IdentityUser
{
    protected readonly ILogger _logger;

    protected EmailSenderBase(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// A method that sends the email
    /// </summary>
    /// <param name="user">The user that send the email</param>
    /// <param name="email">The destination email</param>
    /// <param name="subject">The subject of the email</param>
    /// <param name="body">The body of the email</param>
    /// <param name="purpose">The purpose to send the email (only internally used)</param>
    /// <returns>The async task</returns>
    public abstract Task SendEmail(TUser user, string email, string subject, string body, string purpose);

    public Task SendConfirmationLinkAsync(TUser user, string email, string confirmationLink)
    {
        throw new NotImplementedException();
    }

    public Task SendPasswordResetCodeAsync(TUser user, string email, string resetCode)
    {
        throw new NotImplementedException();
    }

    public Task SendPasswordResetLinkAsync(TUser user, string email, string resetLink)
    {
        throw new NotImplementedException();
    }
}

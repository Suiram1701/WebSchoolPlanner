using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using WebSchoolPlanner.Options;

namespace WebSchoolPlanner.IdentityProviders.EmailSenders;

public class SendGridEmailSender<TUser> : EmailSenderBase<TUser>
    where TUser : IdentityUser
{
    private readonly SendGridOptions _options;

    public SendGridEmailSender(ILogger<SendGridEmailSender<TUser>> logger, IOptions<SendGridOptions> optionsAccessor) : base(logger)
    {
        _options = optionsAccessor.Value;
    }

    public override async Task SendEmailAsync(TUser user, string email, string subject, string body, string purpose)
    {
        SendGridMessage message = new()
        {
            From = _options.FromAddress,
            Subject = subject,
            PlainTextContent = body,
            HtmlContent = body,
        };
        message.AddTo(email, user.UserName);
        message.SetClickTracking(false, false);

        SendGridClient client = new(_options.ApiKey);
        Response response = await client.SendEmailAsync(message, CancellationToken.None);

        if (!response.IsSuccessStatusCode)
        {
            Dictionary<string, string> responseHeaders = response.DeserializeResponseHeaders();
            Dictionary<string, dynamic> responseBody = await response.DeserializeResponseBodyAsync();
            string headerJson = JsonConvert.SerializeObject(responseBody);
            string bodyJson = JsonConvert.SerializeObject(responseBody);

            _logger.LogError("Failed to send email with purpose '{0}' by user {1}; Code: {2}; Headers: {3}; Body: {4}", purpose, user.Id, response.StatusCode, headerJson, bodyJson);
            throw new Exception(string.Format("Failed to send email with purpose {0}", purpose));
        }
        else
            _logger.LogInformation("Email with purpose {0} send successfully by user {1}", purpose, user.Id);
    }
}

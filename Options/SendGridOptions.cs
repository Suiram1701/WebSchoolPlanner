using SendGrid.Helpers.Mail;

namespace WebSchoolPlanner.Options;

public class SendGridOptions
{
    /// <summary>
    /// The api key to access SendGrid
    /// </summary>
    public string ApiKey { get; set; }

    /// <summary>
    /// The address to send the mail from
    /// </summary>
    public EmailAddress FromAddress { get; set; }

    public SendGridOptions()
    {
        ApiKey = string.Empty;
        FromAddress = new();
    }
}

using OtpNet;

namespace WebSchoolPlanner.Options;

/// <summary>
/// Options to generate a totp uri
/// </summary>
public class TotpAuthenticationOptions
{
    /// <summary>
    /// The name of the service providing the authentication
    /// </summary>
    public string Issuer { get; set; }

    /// <summary>
    /// The timespan how long the generated token would be valid (usually 30 seconds)
    /// </summary>
    public TimeSpan ValidTimeSpan { get; set; }

    /// <summary>
    /// The number of digits that the token contains (usually 6)
    /// </summary>
    public int DigitsCount { get; set; }

    /// <summary>
    /// The algorithm wich is used by the generation (usually SHA1)
    /// </summary>
    public OtpHashMode Algorithm { get; set; }

    public TotpAuthenticationOptions()
    {
        Issuer = string.Empty;
        ValidTimeSpan = TimeSpan.FromSeconds(30);
        DigitsCount = 6;
        Algorithm = OtpHashMode.Sha1;
    }
}

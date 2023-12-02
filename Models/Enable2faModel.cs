namespace WebSchoolPlanner.Models;

public class Enable2faModel
{
    /// <summary>
    /// The by the user entered code
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// The 2fa secret to set
    /// </summary>
    public string EncodedSecret { get; set; }

    /// <summary>
    /// Should the client remembered
    /// </summary>
    public bool RememberMe { get; set; }

    public Enable2faModel()
    {
        Code = string.Empty;
        EncodedSecret = string.Empty;
    }

    public Enable2faModel(byte[] secret) : this()
    {
        string secretString = Convert.ToHexString(secret);
        EncodedSecret = secretString;
    }
}

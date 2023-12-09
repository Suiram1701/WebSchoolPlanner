namespace WebSchoolPlanner.Models;

public class Enable2faModel
{
    /// <summary>
    /// The by the user entered code
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Should the client remembered
    /// </summary>
    public bool RememberMe { get; set; }

    public string Secret { get; set; }

    public Enable2faModel()
    {
        Code = string.Empty;
        Secret = string.Empty;
    }

    public Enable2faModel(byte[] secret) : this()
    {
        Secret = Convert.ToHexString(secret);
    }

    public byte[] GetSecret()
    {
        return Convert.FromHexString(Secret);
    }
}

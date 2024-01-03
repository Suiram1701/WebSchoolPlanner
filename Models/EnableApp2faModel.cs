namespace WebSchoolPlanner.Models;

public class EnableApp2faModel : Enable2faModel
{
    /// <summary>
    /// The hex encoded string of the secret
    /// </summary>
    public string Secret { get; set; }

    public EnableApp2faModel() : base()
    {
        Secret = string.Empty;
    }

    public EnableApp2faModel(byte[] secret) : this()
    {
        Secret = Convert.ToHexString(secret);
    }

    public byte[] GetSecret()
    {
        return Convert.FromHexString(Secret);
    }
}

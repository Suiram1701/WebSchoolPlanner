namespace WebSchoolPlanner.Models;

public class Validate2faModel
{
    /// <summary>
    /// The by the user entered code
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Remember the browser
    /// </summary>
    public bool RememberMe { get; set; }

    /// <summary>
    /// The method of the 2fa sign in
    /// </summary>
    public TwoFactorMethod TwoFactorMethod { get; set; }

    public Validate2faModel()
    {
        Code = string.Empty;
        TwoFactorMethod = TwoFactorMethod.App;
    }
}

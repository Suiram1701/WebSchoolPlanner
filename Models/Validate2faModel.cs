namespace WebSchoolPlanner.Models;

public class Validate2faModel
{
    /// <summary>
    /// The by the user entered code
    /// </summary>
    public string Code
    {
        get => _code;
        set => _code = value.Replace(" ", "");
    }
    private string _code;

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
        _code = string.Empty;
        TwoFactorMethod = TwoFactorMethod.App;
    }
}

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

    public Validate2faModel()
    {
        Code = string.Empty;
    }
}

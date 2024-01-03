namespace WebSchoolPlanner.Models;

public class LoginModel
{
    public string Username { get; set; }

    public string Password { get; set; }

    public LoginModel()
    {
        Username = string.Empty;
        Password = string.Empty;
    }
}

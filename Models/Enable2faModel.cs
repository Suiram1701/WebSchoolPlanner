namespace WebSchoolPlanner.Models;

public class Enable2faModel
{
    public string Code
    {
        get => _code;
        set => _code = value.Replace(" ", "");
    }
    private string _code;

    public Enable2faModel()
    {
        _code = string.Empty;
    }
}

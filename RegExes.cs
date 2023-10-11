using System.Text.RegularExpressions;

namespace WebSchoolPlanner;

public static partial class RegExes
{
    /// <summary>
    /// A RegEx that validate a culture string
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"^[a-z]{2}(?:-[A-Z]{2})?$")]
    public static partial Regex CultureRegex();
}

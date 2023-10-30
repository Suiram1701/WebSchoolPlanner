using Microsoft.AspNetCore.Identity;
using WebSchoolPlanner.Db.Models;

namespace WebSchoolPlanner.Extensions;

public static class UserManagerExtensions
{
    /// <summary>
    /// Update the profile image of the givent user
    /// </summary>
    /// <param name="userManager"></param>
    /// <param name="user">The user</param>
    /// <param name="content">The content to set</param>
    /// <returns>The task</returns>
    /// <exception cref="ArgumentException">The file is too large</exception>
    public static async Task SetProfileImageAsync(this UserManager<User> userManager, User user, byte[]? content)
    {
        if (content?.Length > MaxAccountImageSize)
            throw new ArgumentException($"The file must be smaller than or same than {MaxAccountImageSize} bytes.");

        user.AccountImage = content;
        await userManager.UpdateAsync(user);
    }
}

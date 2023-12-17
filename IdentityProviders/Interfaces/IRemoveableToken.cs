using Microsoft.AspNetCore.Identity;

namespace WebSchoolPlanner.IdentityProviders.Interfaces;

/// <summary>
/// An inerface that provides the removing of a user token
/// </summary>
/// <typeparam name="TUser"></typeparam>
public interface IRemoveableToken<TUser>
    where TUser : IdentityUser
{
    /// <summary>
    /// Removes a user token
    /// </summary>
    /// <param name="manager">The manager to use</param>
    /// <param name="user">The user that owns the token</param>
    /// <param name="purpose">The purpose of the token</param>
    /// <returns>The result</returns>
    public Task<IdentityResult> RemoveAsync(UserManager<TUser> manager, TUser user, string purpose);
}

using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json.Linq;

namespace WebSchoolPlanner.IdentityProviders.Interfaces;

/// <summary>
/// An interface that provides countable user tokens
/// </summary>
/// <typeparam name="TUser">The type of the user</typeparam>
public interface ICountableToken<TUser>
    where TUser : IdentityUser
{
    /// <summary>
    /// Get the count of tokens owned by the user
    /// </summary>
    /// <param name="manager">The user manager to use</param>
    /// <param name="user">The user that owns the tokens</param>
    /// <param name="purpose">The purpose of the tokens</param>
    /// <returns>The count</returns>
    public Task<int> CountAsync(UserManager<TUser> manager, TUser user, string purpose);
}

using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebSchoolPlanner.Db.Models;

/// <summary>
/// A user
/// </summary>
public class User : IdentityUser
{
    /// <summary>
    /// The profile imager
    /// </summary>
    [MaxLength((int)MaxAccountImageSize)]
    public byte[]? AccountImage { get; set; }

    /// <summary>
    /// A new instance
    /// </summary>
    public User() : base()
    {
    }

    /// <summary>
    /// A new instance with a username
    /// </summary>
    /// <param name="username">The name</param>
    public User(string username) : base(username)
    {
    }

    /// <summary>
    /// A new instance with username and email
    /// </summary>
    /// <param name="username">The name</param>
    /// <param name="email">The email</param>
    public User(string username, string email) : base(username)
    {
        Email = email;
    }
}

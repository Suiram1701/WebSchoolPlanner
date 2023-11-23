using System.ComponentModel.DataAnnotations;

namespace WebSchoolPlanner.Db.Models;

/// <summary>
/// A account image
/// </summary>
public class UserImageModel : ModelBase
{
    /// <summary>
    /// The content
    /// </summary>
    [MaxLength((int)MaxAccountImageSize)]
    public byte[] Image { get; set; }

    /// <summary>
    /// Creates an empty instance
    /// </summary>
    public UserImageModel() : base()
    {
        Image = Array.Empty<byte>();
    }

    /// <summary>
    /// Creates a new instance
    /// </summary>
    /// <param name="image">The content</param>
    public UserImageModel(byte[] image) : base()
    {
        Image = image;
    }

    /// <summary>
    /// Creates a new instance
    /// </summary>
    /// <param name="id">The id of the user</param>
    /// <param name="image">The </param>
    public UserImageModel(string id, byte[] image)
    {
        Id = id;
        Image = image;
    }
}

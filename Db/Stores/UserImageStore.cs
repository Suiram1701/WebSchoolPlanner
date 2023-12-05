using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebSchoolPlanner.Db.Models;

namespace WebSchoolPlanner.Db.Stores;

/// <summary>
/// A class to manage account images
/// </summary>
/// <typeparam name="TUser">The user type</typeparam>
public class UserImageStore<TUser>
    where TUser : IdentityUser
{
    private readonly ILogger _logger;
    private readonly WebSchoolPlannerDbContext _dbContext;

    public UserImageStore(ILogger<UserImageStore<TUser>> logger, WebSchoolPlannerDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Adds a user image for the specified user
    /// </summary>
    /// <param name="user">The user that owns the image</param>
    /// <param name="image">The image to set</param>
    public async Task AddImageAsync(TUser user, byte[] image)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));
        ArgumentOutOfRangeException.ThrowIfGreaterThan<int>(image.Length, (int)MaxAccountImageSize, nameof(image));

        UserImageModel? existingModel = await _dbContext.UserImages.FindAsync(user.Id);
        if (existingModel is null)     // If not available create it
        {
            UserImageModel model = new(user.Id, image);
            await _dbContext.UserImages.AddAsync(model);
        }
        else
        {
            existingModel.Image = image;
        }
        
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Account image of user {0} updated", user.Id);
    }

    /// <summary>
    /// Read the image of the specified user
    /// </summary>
    /// <param name="user">The user</param>
    /// <returns>The saved image (<see langword="null"/> when not image was set)</returns>
    public async Task<byte[]?> GetImageAsync(TUser user)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));

        UserImageModel? model = await _dbContext.UserImages.FindAsync(user.Id);
        if (model is null)     // No image was set
            return null;

        return model.Image;
    }

    /// <summary>
    /// Remove the account image of the specified user
    /// </summary>
    /// <param name="user">The user</param>
    /// <returns>Was it removed or not (<see langword="false"/> means only that nothing to remove was available)</returns>
    public async Task<bool> RemoveImageAsync(TUser user)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));

        UserImageModel? model = await _dbContext.UserImages.FindAsync(user.Id);
        if (model is not null)     // null if no image is defined
        {
            _dbContext.UserImages.Remove(model);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Account image of user {0} was removed", user.Id);
        }

        return model is not null;
    }
}

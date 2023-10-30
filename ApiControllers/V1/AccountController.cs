using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using WebSchoolPlanner.Db.Models;
using WebSchoolPlanner.Extensions;
using WebSchoolPlanner.Models;

namespace WebSchoolPlanner.ApiControllers.V1;

/// <summary>
/// Generally settings and actions for accounts.
/// </summary>
[Route(ApiRoutePrefix + "Account/")]
[Authorize]
[ApiVersion("1")]
[ApiController]
public sealed class AccountController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly UserManager<User> _userManager;

    private const string AcceptHeaderName = "Accept";
    private const string ImageType = "image/png";

    public AccountController(ILogger<AccountController> logger, UserManager<User> userManager)
    {
        _logger = logger;
        _userManager = userManager;
    }

    /// <summary>
    /// Returns the profile image of the currently logged in account.
    /// </summary>
    /// <param name="contentTypeHeader">The MIME-Type of the image to return. Possible values are image/png, image/gif, image/jpeg (jpg, jpeg), image/bmp, image/x-portable-bitmap (pbm), image/tga, image/tiff and image/webp. OPTIONAL 'image/png' by default</param>
    /// <returns>The image of the account</returns>
    /// <response code="204">No image was set for the profile.</response>
    [HttpGet]
    [Route("Image")]
    [Produces(ImageType, "image/gif", "image/jpeg", "image/bmp", "image/x-portable-bitmap", "image/tga", "image/tiff", "image/webp", Type = typeof(byte[]))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(byte[]))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetImage([FromHeader(Name = AcceptHeaderName)] string? contentTypeHeader = null)
    {
        User user = (await _userManager.GetUserAsync(User))!;
        return await GetUserImageAsync(user);
    }

    /// <summary>
    /// Returns the profile image of the account with the given id.
    /// </summary>
    /// <param name="accountId">The id of the account.</param>
    /// <param name="contentTypeHeader">The MIME-Type of the image to return. Possible values are image/png, image/gif, image/jpeg (jpg, jpeg), image/bmp, image/x-portable-bitmap (pbm), image/tga, image/tiff and image/webp. OPTIONAL 'image/png' by default</param>
    /// <returns>The image of the account with the given id.</returns>
    /// <response code="204">No image was set for the profile.</response>
    [HttpGet]
    [Route("Image/{accountId}")]
    [Produces(ImageType, "image/gif", "image/jpeg", "image/bmp", "image/x-portable-bitmap", "image/tga", "image/tiff", "image/webp", Type = typeof(byte[]))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(byte[]))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetImage([FromRoute] string accountId, [FromHeader(Name = AcceptHeaderName)] string? contentTypeHeader = null)
    {
        if (!Guid.TryParse(accountId, out _))
            throw new FormatException($"The route value '{nameof(accountId)}' must be in the format of a GUID.");

        User? user = await _userManager.FindByIdAsync(accountId);

        if (user is null)
        {
            string currentUserId = _userManager.GetUserId(User)!;
            _logger.LogInformation("User {0} requested account image of not exist user {1}", currentUserId, accountId);

            throw new ArgumentException("No user with the given accountId could be found.");
        }

        return await GetUserImageAsync(user);
    }

    [NonAction]
    private async Task<IActionResult> GetUserImageAsync(User user)
    {
        if (user.AccountImage is null)
            return StatusCode(StatusCodes.Status204NoContent);

        using Stream stream = new MemoryStream(user.AccountImage);
        using Image image = await Image.LoadAsync(stream);
        using MemoryStream memoryStream = new();

        // Use the type with highest request priority
        string choosenTyp = string.Empty;
        IEnumerable<string> acceptHeaders = HttpContext.Request.GetTypedHeaders().Accept
            .OrderByDescending(h => h, MediaTypeHeaderValueComparer.QualityComparer)
            .Select(mh => mh.MediaType == "*/*" ? ImageType : mh.MediaType.ToString());
        foreach (string mediaType in acceptHeaders)
        {
            switch (mediaType)
            {
                case "image/png":
                    await image.SaveAsPngAsync(memoryStream);
                    break;
                case "image/jpeg":
                    await image.SaveAsJpegAsync(memoryStream);
                    break;
                case "image/gif":
                    await image.SaveAsGifAsync(memoryStream);
                    break;
                case "image/bmp":
                    await image.SaveAsBmpAsync(memoryStream);
                    break;
                case "image/x-portable-bitmap":
                    await image.SaveAsPbmAsync(memoryStream);
                    break;
                case "image/tga":
                    await image.SaveAsTgaAsync(memoryStream);
                    break;
                case "image/tiff":
                    await image.SaveAsTiffAsync(memoryStream);
                    break;
                case "image/webp":
                    await image.SaveAsWebpAsync(memoryStream);
                    break;
                default:
                    continue;
            }

            choosenTyp = mediaType;
            break;
        }

        if (string.IsNullOrEmpty(choosenTyp))
            return Problem(
                type: "None of the specified media types of the “Accept” header supported. Supported types are image/png, image/gif, image/jpeg, image/bmp, image/x-portable-bitmap, image/tga, image/tiff and image/webp. By default is image/png choosen.",
                title: "UnSupportedMediaTypes",
                statusCode: StatusCodes.Status406NotAcceptable);

        return File(memoryStream.ToArray(), choosenTyp);
    }

    /// <summary>
    /// Clears the profile image of the current user
    /// </summary>
    [HttpDelete]
    [Route("Image")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteImage()
    {
        User user = (await _userManager.GetUserAsync(User))!;
        await _userManager.SetProfileImageAsync(user, null);

        return Ok();
    }

    /// <summary>
    /// Set the given image to the current account as profile image.
    /// </summary>
    /// <param name="imageForm">The image to set</param>
    /// <param name="cropData">The data to crop the image</param>
    /// <remarks>
    /// A rectangular image is required for the profile picture. When uploading, you can crop the image using the crop part of the form. If this part isn't given, the largest possible image is taken.
    /// 
    /// Sample request:
    ///                         
    ///     POST /api/v1/Account/Image
    ///     Content-Length: 123456
    ///     Content-Type: multipart/form-data; boundary=abcdefg
    ///     
    ///     --abcdefg--
    ///     Content-Disposition: form-data; name="crop"
    ///     Content-Type: application/json
    ///     {
    ///         "Point": {
    ///             "X": 0,
    ///             "Y": 0
    ///         },
    ///         "Size": 200
    ///     }    
    /// 
    ///     --abcdefg--
    ///     Content-Disposition: form-data; name="image"; filename="image1.png"
    ///     Content-Type: application/octet-stream
    ///     {…file content…}
    ///     
    ///     --abcdefg--
    ///     
    /// Valid image formats for the uploaded image are Gif, Jpg, Jpeg, Bitmap, Pbm, Png, Tga, Tiff and WebP
    /// </remarks>
    [HttpPost]
    [Route("Image")]
    [Consumes(typeof(IFormFile), "multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PostImage([FromForm(Name = "Image")] IFormFile imageForm, [FromForm(Name = "Crop")] CropModel? cropData = null)
    {
        // Setup the parameter
        cropData = null;
        if (HttpContext.Request.Form["Crop"].Any())
        {
            string? cropJson = HttpContext.Request.Form["Crop"][0];
            if (cropJson is not null)
                cropData = JsonConvert.DeserializeObject<CropModel>(cropJson);
        }

        if (imageForm is null)
            throw new ArgumentNullException(nameof(imageForm), "A content to set must be given.");

        if (imageForm.Length > MaxAccountImageSize)
            throw new ArgumentException($"The file must be smaller than or same than {MaxAccountImageSize} bytes.");

        if (cropData is not null)
        {
            // Throw an exception when any org is out of range
            const string tooSmallErrorMessage = "The value of '{0}' must be larger or same than 0.";
            if (cropData.Point.X < 0)
                throw new ArgumentOutOfRangeException(nameof(cropData.Point.X), string.Format(tooSmallErrorMessage, "Crop.Point.X"));
            if (cropData.Point.Y < 0)
                throw new ArgumentOutOfRangeException(nameof(cropData.Point.Y), string.Format(tooSmallErrorMessage, "Crop.Point.Y"));
            if (cropData.Size <= 0)
                throw new ArgumentOutOfRangeException(nameof(cropData.Size), "The value of 'Crop.size' must be larger than 0.");
        }

        using Image image = await Image.LoadAsync(imageForm.OpenReadStream());

        // Crop it to rect
        Point point;
        Size cropSize;

        if (cropData is not null)
        {
            point = cropData.Point;
            cropSize = new(cropData.Size);

            // Check if any arg isn't valid
            const string tooLargeErrorMessage = "The value of '{0}' must be smaller than the images {1} {2}px";
            if (point.X >= image.Width)
                throw new ArgumentOutOfRangeException(nameof(cropData.Point.X), string.Format(tooLargeErrorMessage, "Crop.Point.X", "width", image.Width));
            if (point.Y >= image.Height)
                throw new ArgumentOutOfRangeException(nameof(cropData.Point.Y), string.Format(tooLargeErrorMessage, "Crop.Point.Y", "height", image.Height));

            if ((point.X + cropSize.Width) > image.Width && (point.Y + cropSize.Height) > image.Height)
                throw new ArgumentOutOfRangeException(nameof(cropData.Size), "The value of 'Crop.Size' is too large to crop the image correctly.");
        }
        else
        {
            // Automatic image crop
            int shorterSide = Math.Min(image.Height, image.Width);
            int yOffSet = (image.Height - shorterSide) / 2;
            int xOffSet = (image.Width - shorterSide) / 2;

            point = new(xOffSet, yOffSet);
            cropSize = new(shorterSide);
        }

        image.Mutate(op => op.Crop(new(point, cropSize)));

        using MemoryStream stream = new();
        await image.SaveAsPngAsync(stream);

        User user = (await _userManager.GetUserAsync(User))!;
        await _userManager.SetProfileImageAsync(user, stream.ToArray());

        string currentUserId = _userManager.GetUserId(User)!;
        _logger.LogInformation("User {1} updated account image", currentUserId);
        return Ok();
    }
}

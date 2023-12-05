using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using static QRCoder.Base64QRCode;
using System.Net;
using WebSchoolPlanner.ApiModels;
using WebSchoolPlanner.Db.Models;
using WebSchoolPlanner.Db.Stores;
using WebSchoolPlanner.Extensions;
using WebSchoolPlanner.Swagger.Attributes;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Pbm;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats.Png;

namespace WebSchoolPlanner.ApiControllers.V1;

/// <summary>
/// Manage the account image
/// </summary>
[Route(ApiRoutePrefix + "account/image")]
[Authorize]
[ApiVersion("1")]
[ApiController]
public sealed class AccountImageController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly UserImageStore<User> _userImageStore;

    private const string _acceptHeaderName = "Accept";
    private const string _anyTypeChar = "*";

    private const string _defaultImageMimeType = "image/png";
    private static readonly IImageEncoder _defaultImageEncoder = new PngEncoder();     // Changing this value would be effects on saved images (if you change this you have to adapt the default mime type)

    public AccountImageController(ILogger<AccountImageController> logger, SignInManager<User> signInManager, UserManager<User> userManager, UserImageStore<User> userImageStore)
    {
        _logger = logger;
        _signInManager = signInManager;
        _userManager = userManager;
        _userImageStore = userImageStore;
    }

    /// <summary>
    /// Returns the profile image of the currently logged in account.
    /// </summary>
    /// <remarks>
    /// Returns the profile image of the currently logged in user as in the 'Accept' header specified image format.
    /// </remarks>
    /// <param name="contentTypeHeader">The MIME-Type of the image to return. Possible values are image/png, image/gif, image/jpeg (jpg, jpeg), image/bmp, image/x-portable-bitmap (pbm), image/tga, image/tiff and image/webp.</param>
    /// <response code="204">No image was set for the profile.</response>
    [HttpGet]
    [ProducesIntegrityHash(StatusCodes.Status200OK)]
    [Produces("image/png", "image/gif", "image/jpeg", "image/bmp", "image/x-portable-bitmap", "image/tga", "image/tiff", "image/webp", Type = typeof(byte[]))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(byte[]))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetImage([FromHeader(Name = _acceptHeaderName)] string? contentTypeHeader = _defaultImageMimeType)
    {
        User user = (await _userManager.GetUserAsync(User))!;
        return await GetUserImageAsync(user);
    }

    /// <summary>
    /// Returns the profile image of the account with the given id.
    /// </summary>
    /// <remarks>
    /// Returns the profile image of the specified user as in the 'Accept' header specified image format.
    /// </remarks>
    /// <param name="accountId">The id of the account.</param>
    /// <param name="contentTypeHeader">The MIME-Type of the image to return. Possible values are image/png, image/gif, image/jpeg (jpg, jpeg), image/bmp, image/x-portable-bitmap (pbm), image/tga, image/tiff and image/webp.</param>
    /// <response code="204">No image was set for the profile.</response>
    [HttpGet("{accountId}")]
    [AllowAnonymous]
    [ProducesIntegrityHash(StatusCodes.Status200OK)]
    [Produces("image/png", "image/gif", "image/jpeg", "image/bmp", "image/x-portable-bitmap", "image/tga", "image/tiff", "image/webp", Type = typeof(byte[]))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(byte[]))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetImage([FromRoute] string accountId, [FromHeader(Name = _acceptHeaderName)] string? contentTypeHeader = _defaultImageMimeType)
    {
        if (!Guid.TryParse(accountId, out _))
            throw new FormatException($"The route value '{nameof(accountId)}' must be in the format of a GUID.");

        User? user = await _userManager.FindByIdAsync(accountId);
        if (user is null)
            throw new ArgumentException("No user with the given accountId could be found.");

        return await GetUserImageAsync(user);
    }

    [NonAction]
    private async Task<IActionResult> GetUserImageAsync(User user)
    {
        byte[]? imageContent = await _userImageStore.GetImageAsync(user);
        if (imageContent is null)
            return NoContent();
        using Stream stream = new MemoryStream(imageContent);

        IList<MediaTypeHeaderValue> mediaTypeHeaderValues = HttpContext.Request.GetTypedHeaders().Accept;
        IImageEncoder? encoder = DetermineImageEncoderType(mediaTypeHeaderValues, out MediaTypeHeaderValue? encoderType);
        if (encoder is null)     // Could not determine requested media type
        {
            return Problem(
                type: "None of the specified media types of the “Accept” header supported. Supported types are image/png, image/gif, image/jpeg, image/bmp, image/x-portable-bitmap, image/tga, image/tiff and image/webp. By default is image/png chosen.",
                title: "UnSupportedMediaType",
                statusCode: StatusCodes.Status406NotAcceptable);
        }

        if (_defaultImageEncoder.GetType().Equals(encoder))     // Already encoded as as the the default type
            return this.FileWithIntegrityHash(stream, _defaultImageMimeType);

        // Encode it as the determined type
        using Image image = await Image.LoadAsync(stream, HttpContext.RequestAborted);
        MemoryStream memoryStream = new();
        HttpContext.Response.OnCompleted(async () => await memoryStream.DisposeAsync());     // Dispose when the request ends (if using is used it were disposed too early)

        await image.SaveAsync(memoryStream, encoder, HttpContext.RequestAborted);
        return this.FileWithIntegrityHash(memoryStream, encoderType!.ToString());
    }

    /// <summary>
    /// Determine the requested image encoder type
    /// </summary>
    /// <param name="mediaTypes">All in the requested specified accepted header</param>
    /// <param name="encoderType">The determined media type. <see langword="null"/> if no type could be determined.</param>
    /// <returns>The determined encoder type. <see langword="null"/> if no encoder could be determined.</returns>
    [NonAction]
    private static IImageEncoder? DetermineImageEncoderType(IEnumerable<MediaTypeHeaderValue> mediaTypes, out MediaTypeHeaderValue? encoderType)
    {
        ArgumentNullException.ThrowIfNull(mediaTypes, nameof(mediaTypes));

        // Use image/png by default
        if (!mediaTypes.Any())
        {
            mediaTypes = new[]
            {
                new MediaTypeHeaderValue(_defaultImageMimeType, 1)
            };
        }

        mediaTypes = mediaTypes
            .Select(mt => mt.Type == _anyTypeChar ? new(_defaultImageMimeType, mt.Quality ?? 0.1) : mt)     // Replace type any with image/png
            .Where(mt => mt.Type == "image");
        if (!mediaTypes.Any())
            throw new ArgumentException("In the 'accept' header was a image media type expected.");

        foreach (MediaTypeHeaderValue mediaType in mediaTypes
            .OrderDescending(MediaTypeHeaderValueComparer.QualityComparer))
        {
            encoderType = mediaType;

            if (mediaType.SubType == _anyTypeChar)
                return _defaultImageEncoder;

            IImageEncoder? encoder = mediaType.SubType.ToString() switch
            {
                "png" => new PngEncoder(),
                "jpeg" => new JpegEncoder(),
                "gif" => new GifEncoder(),
                "bmp" => new BmpEncoder(),
                "image/x-portable-bitmap" => new PbmEncoder(),
                "tga" => new TgaEncoder(),
                "tiff" => new TiffEncoder(),
                "webp" => new WebpEncoder(),
                _ => null
            };
            if (encoder is not null)
                return encoder;
        }

        encoderType = null;
        return null;
    }

    /// <summary>
    /// Clears the profile image of the current user.
    /// </summary>
    /// <remarks>
    /// This clears the account image of the currently logged in user unrecoverable.
    /// </remarks>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteImage()
    {
        User user = (await _userManager.GetUserAsync(User))!;
        await _userImageStore.RemoveImageAsync(user);

        return Ok();
    }

    /// <summary>
    /// Set the given image to the current account as profile image.
    /// </summary>
    /// <param name="imageForm">The image to set</param>
    /// <param name="cropData">The data to crop the image.</param>
    /// <remarks>
    /// This method sets the account image of the currently logged in user.
    /// 
    /// A rectangular image is required for the profile picture. When uploading, you can crop the image using the crop part of the form. If this part isn't given, the largest possible image is taken.
    /// 
    /// Sample request:
    ///                         
    ///     PUT /api/v1/account/image
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
    [HttpPut]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PutImage([FromForm(Name = "Image")] IFormFile imageForm, [FromForm(Name = "Crop")] CropModel? cropData = null)
    {
        // Setup the parameter
        cropData = null;
        if (HttpContext.Request.Form["Crop"].Any())
        {
            // Parse it manually because it wont be parsed correctly
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

        using Image image = await Image.LoadAsync(imageForm.OpenReadStream(), HttpContext.RequestAborted);

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
        await image.SaveAsync(stream, _defaultImageEncoder, HttpContext.RequestAborted);

        User user = (await _userManager.GetUserAsync(User))!;
        await _userImageStore.AddImageAsync(user, stream.ToArray());

        return Ok();
    }
}

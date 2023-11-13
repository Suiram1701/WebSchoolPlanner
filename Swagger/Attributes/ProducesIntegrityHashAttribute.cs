namespace WebSchoolPlanner.Swagger.Attributes;

/// <summary>
/// Adds for swagger documentation the Content-SHA256 header for this endpoint
/// </summary>
public sealed class ProducesIntegrityHashAttribute : ProducesResponseHeaderAttribute
{
    private const string _headerName = "Content-SHA256";

    /// <summary>
    /// The header should be applied to all responses
    /// </summary>
    public ProducesIntegrityHashAttribute() : this(-1)
    {
    }

    /// <summary>
    /// The header should be applied to all responses with the given status codes
    /// </summary>
    /// <param name="statusCode">The status code</param>
    /// <param name="additionalStatusCodes">Other status codes</param>
    public ProducesIntegrityHashAttribute(int statusCode, params int[] additionalStatusCodes) : base(_headerName, statusCode, additionalStatusCodes)
    {
        Description = "The SHA256 hash of the content for integrity check.";
        HeaderType = typeof(byte[]);
    }
}

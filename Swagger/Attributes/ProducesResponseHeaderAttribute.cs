namespace WebSchoolPlanner.Swagger.Attributes;

/// <summary>
/// A attribute that add a response header in a swagger action.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class ProducesResponseHeaderAttribute : Attribute
{
    /// <summary>
    /// The name of the header
    /// </summary>
    public string HeaderName { get; set; }

    /// <summary>
    /// The status codes where the header should be used
    /// </summary>
    public IList<int> StatusCodes { get; set; }

    /// <summary>
    /// The type of the header value
    /// </summary>
    public Type HeaderType { get; set; } = typeof(string);
    
    public string? Description { get; set; }

    /// <summary>
    /// Creates a new attribute
    /// </summary>
    /// <remarks>
    /// The header is uses to every response
    /// </remarks>
    /// <param name="name">The header name</param>
    public ProducesResponseHeaderAttribute(string name) : this(name, -1)
    {
    }

    /// <summary>
    /// Creates new attribute
    /// </summary>
    /// <param name="name">The header name</param>
    /// <param name="statusCode">The status code where the header should be used.</param>
    /// <param name="additionalStatusCodes">Additional status codes where the header should be used.</param>
    public ProducesResponseHeaderAttribute(string name, int statusCode, params int[] additionalStatusCodes)
    {
        HeaderName = name;

        StatusCodes = new List<int>();
        StatusCodes.Add(statusCode);
        foreach (int additionalStatusCode in additionalStatusCodes)
            StatusCodes.Add(additionalStatusCode);
    }
}

namespace WebSchoolPlanner.RouteConstraints;

/// <summary>
/// A attribute that add the route constraint automatically
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RouteConstraintAttribute : Attribute
{
    /// <summary>
    /// The suffix for route constrains
    /// </summary>
    private const string _routeConstraintSuffix = "RouteConstraint";

    /// <summary>
    /// The name of the route constraint
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The type of the route constraint
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// A new route constraint attribute
    /// </summary>
    /// <param name="type">The type of the route constraint. This type must be implement <see cref="IRouteConstraint"/></param>
    /// <remarks>
    /// The <see cref="Name"/> is the type name. If available the route constraint suffix will be removed.
    /// </remarks>
    public RouteConstraintAttribute(Type type)
    {
        Type = type;

        string name = Type.Name;
        if (name.EndsWith(_routeConstraintSuffix))
            Name = name.Remove(name.Length - _routeConstraintSuffix.Length);
        else
            Name = name;
    }

    /// <summary>
    /// A new route constraint attribute with the the type as name removed the suffix and in lower case
    /// </summary>
    /// <param name="type">The type of the route constraint. This type must be implement <see cref="IRouteConstraint"/></param>
    /// <param name="routeConstraintName">The name of the constraint</param>
    public RouteConstraintAttribute(Type type, string routeConstraintName) : this(type)
    {
        Name = routeConstraintName;
    }
}

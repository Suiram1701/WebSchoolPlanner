using Microsoft.AspNetCore.Routing.Constraints;

namespace WebSchoolPlanner.RouteConstraints;

/// <summary>
/// A route constraint that validate a culture string
/// </summary>
[RouteConstraint(typeof(CultureRouteConstraint))]
public sealed class CultureRouteConstraint : RegexRouteConstraint
{
    public CultureRouteConstraint() : base(CultureRegex())
    {
    }
}

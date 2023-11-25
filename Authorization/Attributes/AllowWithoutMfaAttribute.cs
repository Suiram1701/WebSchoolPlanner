namespace WebSchoolPlanner.Authorization.Attributes;

/// <summary>
/// This attribute marks a controller or action as accessible without confirmed 2fa login
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowWithoutMfaAttribute : Attribute
{
}

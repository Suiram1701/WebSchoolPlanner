using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Converters;
using System.Security.Claims;

namespace WebSchoolPlanner.IdentityProviders;

/// <summary>
/// A factory that provides support to create from the app used claims
/// </summary>
/// <typeparam name="TUser">The type of the user</typeparam>
/// <typeparam name="TRole">The type of the role</typeparam>
public class UserClaimsPrincipialFactory<TUser, TRole> : UserClaimsPrincipalFactory<TUser, TRole>
    where TUser : IdentityUser
    where TRole : IdentityRole
{
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;

    public UserClaimsPrincipialFactory(
        UserManager<TUser> userManager,
        RoleManager<TRole> roleManager,
        IOptions<IdentityOptions> options,
        ILoggerFactory loggerFactory,
        IConfiguration configuration)
        : base(userManager, roleManager, options)
    {
        _logger = loggerFactory.CreateLogger<UserClaimsPrincipalFactory<TUser, TRole>>();
        _configuration = configuration;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(TUser user)
    {
        ClaimsIdentity claims = await base.GenerateClaimsAsync(user);

        // Add expiration data
        long cUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Claim issuedAtClaim = new("iat", cUnix.ToString());

        const string expiresConfigurationPath = AuthenticationConfigurationPrefix + "Expires";
        string loginSpanString = _configuration[expiresConfigurationPath]
            ?? "3600";     // Default set to one hour
        if (uint.TryParse(loginSpanString, out uint loginSpanSeconds))
        {
            TimeSpan loginSpan = TimeSpan.FromSeconds(loginSpanSeconds);
            long expUnix = DateTimeOffset.UtcNow.Add(loginSpan).ToUnixTimeSeconds();
            Claim expiresAtClaim = new("exp", expUnix.ToString());

            claims.AddClaim(issuedAtClaim);
            claims.AddClaim(expiresAtClaim);
        }
        else
            _logger.LogError("The specified log in span string have to specify a integer between {0} and {1}; Configuration path: \"{2}\"", uint.MinValue, uint.MaxValue, expiresConfigurationPath);
        
        return claims;
    }
}

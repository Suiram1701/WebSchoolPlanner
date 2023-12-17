using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using WebSchoolPlanner.IdentityProviders.Interfaces;
using WebSchoolPlanner.Options;

namespace WebSchoolPlanner.IdentityProviders;

/// <summary>
/// A provider for email two factor confirmation
/// </summary>
public class UserEmailTwoFactorTokenProvider<TUser> : IUserTwoFactorTokenProvider<TUser>, IRemoveableToken<TUser>
    where TUser : IdentityUser
{
    /// <summary>
    /// The name of this provider
    /// </summary>
    public const string ProviderName = $"[{nameof(UserEmailTwoFactorTokenProvider<TUser>)}]";

    private readonly ILogger _logger;
    private readonly IPasswordHasher<TUser> _passwordHasher;
    private readonly EmailTwoFactorOptions _options;

    public UserEmailTwoFactorTokenProvider(ILogger<UserEmailTwoFactorTokenProvider<TUser>> logger, IPasswordHasher<TUser> passwordHasher, IOptions<EmailTwoFactorOptions> optionsAccessor)
    {
        _logger = logger;
        _passwordHasher = passwordHasher;
        _options = optionsAccessor.Value;
    }

    public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user) =>
        Task.FromResult(manager.SupportsUserAuthenticationTokens && manager.SupportsUserTwoFactor);

    public async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
    {
        string formattedCode = TokenHelpers.GenerateFormattedCode();

        TimeSpan validSpan = TimeSpan.FromSeconds(_options.ExpirationTime);
        DateTimeOffset validOffset = DateTimeOffset.UtcNow.Add(validSpan);

        TokenModel token = new(validOffset);
        token.SetToken(_passwordHasher, user, formattedCode);
        string tokenJson = JsonConvert.SerializeObject(token);

        // Create code in db
        IdentityResult result = await manager.SetAuthenticationTokenAsync(user, ProviderName, purpose, tokenJson);
        if (!result.Succeeded)
        {
            string errorJson = JsonConvert.SerializeObject(result.Errors);
            _logger.LogError("An error happened while trying to generate 2fa email code for user {0}; Error: {1}", user.Id, errorJson);
            throw new Exception("An error happened while trying to generate 2fa emailt code.");
        }

        _logger.LogInformation("New email confirmation code generated for user {0}; Expires: {1}", user.Id, validOffset.ToString("s"));
        return formattedCode;
    }

    public async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
    {
        string? tokenJson = await manager.GetAuthenticationTokenAsync(user, ProviderName, purpose);
        if (string.IsNullOrEmpty(tokenJson))     // No email confirmation requested
            return false;

        TokenModel model = JsonConvert.DeserializeObject<TokenModel>(tokenJson)!;
        if (model.IssuedAt > DateTime.UtcNow || model.Expires < DateTime.UtcNow)     // expired or not valid yet
        {
            await RemoveAsync(manager, user, purpose);
            return false;
        }

        bool result = model.ValidateToken(_passwordHasher, user, token);
        if (result)
            await RemoveAsync(manager, user, purpose);

        return result;
    }

    public async Task<IdentityResult> RemoveAsync(UserManager<TUser> manager, TUser user, string purpose)
    {
        IdentityResult result = await manager.RemoveAuthenticationTokenAsync(user, ProviderName, purpose);
        if (result.Succeeded)
            _logger.LogInformation("Email confirmation code for user {0} removed", user.Id);
        else
        {
            string jsonContent = JsonConvert.SerializeObject(result.Errors);
            _logger.LogError("An error happened while removing 2fa code for user {0}", user.Id);
        }

        return result;
    }

    /// <summary>
    /// A token
    /// </summary>7
    private class TokenModel
    {
        /// <summary>
        /// The token to save
        /// </summary>
        [JsonProperty("thsh")]
        private string _tokenHash;

        /// <summary>
        /// The timestamp where the token expires
        /// </summary>
        [JsonIgnore]
        public DateTimeOffset Expires
        {
            get => DateTimeOffset.FromUnixTimeSeconds(_expiration);
            set => _expiration = value.ToUnixTimeSeconds();
        }

        [JsonProperty("exp")]
        private long _expiration;

        /// <summary>
        /// The timestamp from which the token is valid
        /// </summary>
        [JsonIgnore]
        public DateTimeOffset IssuedAt
        {
            get => DateTimeOffset.FromUnixTimeSeconds(_issuedAt);
            set => _issuedAt = value.ToUnixTimeSeconds();
        }

        [JsonProperty("iat")]
        private long _issuedAt;

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public TokenModel()
        {
            _tokenHash = string.Empty;
            Expires = DateTime.UtcNow;
            IssuedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="expires">The expires time</param>
        public TokenModel(DateTimeOffset expires) : this()
        {
            Expires = expires;
        }

        /// <summary>
        /// Set a unhashed token
        /// </summary>
        /// <param name="hasher">The hasher to use</param>
        /// <param name="user">The user that owns the token</param>
        /// <param name="token">The token to set</param>
        public void SetToken(IPasswordHasher<TUser> hasher, TUser user, string token) =>
            _tokenHash = HashToken(hasher, user, token);

        /// <summary>
        /// Validates the specified token with the saved token
        /// </summary>
        /// <param name="hasher">The hasher that hashed the token before</param>
        /// <param name="user">The user that owns the code</param>
        /// <param name="token">The token to validate</param>
        /// <returns>The result</returns>
        public bool ValidateToken(IPasswordHasher<TUser> hasher, TUser user, string token) =>
            hasher.VerifyHashedPassword(user, _tokenHash, token) != PasswordVerificationResult.Failed;

        /// <summary>
        /// Hashes a token
        /// </summary>
        /// <param name="hasher">The password hasher to use</param>
        /// <param name="user">The user that owns the code</param>
        /// <param name="token">The token to hash</param>
        /// <returns>The hashed code</returns>
        private string HashToken(IPasswordHasher<TUser> hasher, TUser user, string token) =>
            hasher.HashPassword(user, token);
    }
}

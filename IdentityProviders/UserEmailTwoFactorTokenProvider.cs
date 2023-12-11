using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using WebSchoolPlanner.Options;

namespace WebSchoolPlanner.IdentityProviders;

/// <summary>
/// A provider for email two factor confirmation
/// </summary>
public class UserEmailTwoFactorTokenProvider<TUser> : IUserTwoFactorTokenProvider<TUser>
    where TUser : IdentityUser
{
    /// <summary>
    /// The name of this provider
    /// </summary>
    public const string ProviderName = $"[{nameof(UserEmailTwoFactorTokenProvider<TUser>)}]";

    private readonly ILogger _logger;
    private readonly EmailTwoFactorOptions _options;

    public UserEmailTwoFactorTokenProvider(ILogger<UserEmailTwoFactorTokenProvider<TUser>> logger, IOptions<EmailTwoFactorOptions> optionsAccessor)
    {
        _logger = logger;
        _options = optionsAccessor.Value;
    }

    public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user) =>
        Task.FromResult(manager.SupportsUserAuthenticationTokens && manager.SupportsUserTwoFactor);

    public async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
    {
        string formattedCode = TokenHelpers.GenerateFormattedCode();

        TimeSpan validSpan = TimeSpan.FromSeconds(_options.ExpirationTime);
        DateTimeOffset validOffset = DateTimeOffset.UtcNow.Add(validSpan);

        TokenModel token = new(formattedCode, validOffset);
        string tokenJson = JsonConvert.SerializeObject(token);

        // Create code in db
        IdentityResult result = await manager.SetAuthenticationTokenAsync(user, ProviderName, purpose, tokenJson);
        if (!result.Succeeded)
        {
            string errorJson = JsonConvert.SerializeObject(result.Errors);
            _logger.LogError("An error happened while trying to generate 2fa email code for user {0}; Error: {1}", user.Id, errorJson);
            throw new Exception("An error happened while trying to generate 2fa emailt code.");
        }

        return formattedCode;
    }

    public async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
    {
        string? tokenJson = await manager.GetAuthenticationTokenAsync(user, ProviderName, purpose);
        if (string.IsNullOrEmpty(tokenJson))     // No email confirmation requested
            return false;

        try
        {
            TokenModel model = JsonConvert.DeserializeObject<TokenModel>(tokenJson)!;
            if (model.IssuedAt > DateTime.UtcNow || model.Expires < DateTime.UtcNow)     // expired or not valid yet
            {
                await RemoveAsync(manager, user, purpose);
                return false;
            }

            bool result = model.ValidateToken(token);
            if (result)
                await RemoveAsync(manager, user, purpose);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError("Unable to validate 2fa email code of user {0}; Error: {1}", user.Id, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Removes any existing code from the user
    /// </summary>
    /// <param name="manager">The manager to use</param>
    /// <param name="user">The user</param>
    /// <param name="purpose">The purpose of the token</param>
    /// <returns>The result</returns>
    public async Task<IdentityResult> RemoveAsync(UserManager<TUser> manager, TUser user, string purpose)
    {
        return await manager.RemoveAuthenticationTokenAsync(user, ProviderName, purpose);
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
        /// Creates a new instance
        /// </summary>
        /// <param name="token">The token to set</param>
        /// <param name="expires">The expires time</param>
        public TokenModel(string token, DateTimeOffset expires) : this(expires)
        {
            SetToken(token);
        }

        /// <summary>
        /// Set a token
        /// </summary>
        /// <param name="token">The token to set</param>
        public void SetToken(string token) =>
            _tokenHash = HashToken(token);

        /// <summary>
        /// Validates the specified token with the saved token
        /// </summary>
        /// <param name="token">The token to validate</param>
        /// <returns>The result</returns>
        public bool ValidateToken(string token) =>
            _tokenHash == HashToken(token);

        /// <summary>
        /// Hashes a token
        /// </summary>
        /// <param name="token">The token to hash</param>
        /// <returns>The base64 encoded hash</returns>
        private string HashToken(string token)
        {
            byte[] tokenData = Encoding.UTF8.GetBytes(token);
            byte[] data = SHA256.HashData(tokenData);
            return Convert.ToBase64String(data);
        }
    }
}

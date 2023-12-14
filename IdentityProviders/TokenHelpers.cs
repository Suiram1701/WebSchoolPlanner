using Microsoft.AspNetCore.DataProtection;
using OtpNet;
using System.Security.Cryptography;
using System.Text;

namespace WebSchoolPlanner.IdentityProviders;

public static class TokenHelpers
{
    private const string _allowedCodeChars = "23456789BCDFGHJKMNPQRTVWXY";

    /// <summary>
    /// Generates a specified count of random chars of the <see cref="_allowedCodeChars"/>
    /// </summary>
    /// <param name="length">The count of generated chars</param>
    /// <returns>The random chars</returns>
    public static IEnumerable<char> GenerateRandomChars(int length)
    {
        byte[] randomBytes = new byte[length];
        RandomNumberGenerator.Fill(randomBytes);

        foreach (byte b in randomBytes)
        {
            int charIndex = b % _allowedCodeChars.Length;
            yield return _allowedCodeChars[charIndex];
        }
    }

    /// <summary>
    /// Generates a random code with in the 'XXXXX-XXXXX' format
    /// </summary>
    /// <returns>The generated code</returns>
    public static string GenerateFormattedCode()
    {
        IEnumerable<char> chars = GenerateRandomChars(10);
        StringBuilder formattedCodeBuilder = new StringBuilder(11)
            .Append(chars.Take(..5))
            .Append('-')
            .Append(chars.Take(5..));
        return formattedCodeBuilder.ToString();
    }

    /// <summary>
    /// Protects a token
    /// </summary>
    /// <param name="token">The token to protect</param>
    /// <param name="dataProtector">The protected that should be used</param>
    /// <returns>The base64 string of the protected token</returns>
    public static string ProtectToken(string token, IDataProtector dataProtector)
    {
        byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
        byte[] protectedToken = dataProtector.Protect(tokenBytes);
        return Convert.ToBase64String(protectedToken);
    }

    /// <summary>
    /// Unprotect a by the <see cref="ProtectToken(string, IDataProtector)"/> method protected token
    /// </summary>
    /// <param name="token">The token to unprotect</param>
    /// <param name="dataProtector">The token protector</param>
    /// <returns>The unprotected data</returns>
    public static string UnprotectToken(string token, IDataProtector dataProtector)
    {
        byte[] protectedToken = Convert.FromBase64String(token);
        byte[] unprotectedToken = dataProtector.Unprotect(protectedToken);
        return Encoding.UTF8.GetString(unprotectedToken);
    }
}

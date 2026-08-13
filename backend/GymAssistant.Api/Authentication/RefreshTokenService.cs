using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
namespace GymAssistant.Api.Authentication;

public class RefreshTokenService : IRefreshTokenService
{
    public string GenerateRefreshToken()
    {
       var randomBytes = RandomNumberGenerator.GetBytes(32);
       return WebEncoders.Base64UrlEncode(randomBytes);
    }

    public byte[] HashRefreshToken(string refreshToken)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
    }
}
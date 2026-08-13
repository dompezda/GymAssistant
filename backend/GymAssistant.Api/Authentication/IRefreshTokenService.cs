namespace GymAssistant.Api.Authentication;

public interface IRefreshTokenService
{
    string GenerateRefreshToken();
    byte[] HashRefreshToken(string refreshToken);
}
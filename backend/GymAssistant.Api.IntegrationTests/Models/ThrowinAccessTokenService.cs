using GymAssistant.Api.Authentication;
using GymAssistant.Api.Models;

public class ThrowingAccessTokenService : IAccessTokenService
{
    public AccessTokenResult CreateToken(ApplicationUser user)
    {
        throw new InvalidOperationException("Simulated service failure");
    }
}
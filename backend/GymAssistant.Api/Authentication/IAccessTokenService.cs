using GymAssistant.Api.Models;

namespace GymAssistant.Api.Authentication;

public interface IAccessTokenService
{
    AccessTokenResult CreateToken(ApplicationUser user);
}
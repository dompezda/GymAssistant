using System.IdentityModel.Tokens.Jwt;

namespace GymAssistant.Api.Authentication;
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User
                .FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                throw new InvalidOperationException(
                    "The current user is not authenticated.");
            }

            return userId;
        }
    }
}
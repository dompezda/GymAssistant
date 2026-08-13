using Microsoft.AspNetCore.Identity;

namespace GymAssistant.Api.Models;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public List<RefreshToken> RefreshTokens { get; set; } = new();
}
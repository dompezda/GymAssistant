namespace GymAssistant.Api.Authentication;

public sealed class RefreshTokenOptions
{
    public const string SectionName = "RefreshToken";
    public int LifetimeDays {get;init;}
}
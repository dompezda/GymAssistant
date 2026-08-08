namespace GymAssistant.Api.Authentication;
public interface ICurrentUser
{
    Guid UserId { get; }
}
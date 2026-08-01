namespace GymAssistant.Api.Authentication;

public sealed record AccessTokenResult(
    string AccessToken,
    DateTime ExpiresAtUtc
);
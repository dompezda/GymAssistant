using GymAssistant.Api.Authentication;
using GymAssistant.Api.Models;
public static class AuthHelper
{
    public static void AuthorizeClient(HttpClient client, ApplicationUser user, IAccessTokenService tokenService)
    {
        var token = tokenService.CreateToken(user);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
    }
    
}
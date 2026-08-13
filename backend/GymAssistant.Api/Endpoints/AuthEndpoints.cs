using System.IdentityModel.Tokens.Jwt;
using GymAssistant.Api.Authentication;
using GymAssistant.Api.Data;
using GymAssistant.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace GymAssistant.Api.Endpoints;

public static class AuthEndpoints
{

    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/login", async (
            LoginRequest request,
            AppDbContext dbContext,
            HttpResponse response,
            UserManager<ApplicationUser> userManager,
            IRefreshTokenService refreshTokenService,
            IAccessTokenService tokenService) => {
            if(string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest("Email and password are required.");
            }
            var user = await userManager.FindByEmailAsync(request.Email);
            if(user is null)
            {
                return Results.Unauthorized();
            }

            var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
            if(!passwordValid)
            {
                return Results.Unauthorized();
            }
            var refreshToken = refreshTokenService.GenerateRefreshToken();
            var now = DateTime.UtcNow;
            var expires = now.AddDays(7);
            dbContext.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshTokenService.HashRefreshToken(refreshToken),
                CreatedAtUtc = now,
                ExpiresAtUtc = expires           
            });
            await dbContext.SaveChangesAsync();
            var accessToken = tokenService.CreateToken(user);
            response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expires
            });
            return Results.Ok(new LoginResponse(accessToken.AccessToken, accessToken.ExpiresAtUtc));
        });
        group.MapGet("/me",async (
        UserManager<ApplicationUser> userManager,
        ICurrentUser currentUser) =>
    {
        var user = await userManager.FindByIdAsync(
            currentUser.UserId.ToString());

        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            return Results.Unauthorized();
        }

        return Results.Ok(
            new AuthMeResponse(user.Id, user.Email));
    })
    .RequireAuthorization();
    }
}

public record AuthMeResponse(
    Guid Id,
    string Email
);

public record LoginRequest(
    string Email,
    string Password
);
public record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc
);
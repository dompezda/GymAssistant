using System.IdentityModel.Tokens.Jwt;
using System.Text;
using GymAssistant.Api.Authentication;
using GymAssistant.Api.Data;
using GymAssistant.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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
            IOptions<RefreshTokenOptions> refreshTokenOptions,
            IAccessTokenService tokenService) => {
            if(string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest("Email i hasło są wymagane.");
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
            var expires = now.AddDays(refreshTokenOptions.Value.LifetimeDays);
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


        group.MapPost("/logout", async (
            AppDbContext dbContext,
            IRefreshTokenService refreshTokenService,
            HttpRequest request,
            HttpResponse response
        )=>
        {
            if (!request.Cookies.TryGetValue("refreshToken", out var refreshToken)
            || string.IsNullOrWhiteSpace(refreshToken))
            {
                response.Cookies.Delete("refreshToken");
                return Results.NoContent();
            }
            var now = DateTime.UtcNow;
            var hashedToken = refreshTokenService.HashRefreshToken(refreshToken);
            var activeToken = await dbContext.RefreshTokens
                .FirstOrDefaultAsync(x=>x.TokenHash.SequenceEqual(hashedToken) && x.RevokedAtUtc == null);
            if(activeToken!=null)
            {
                activeToken.RevokedAtUtc = now;
                await dbContext.SaveChangesAsync();
            }
            
            response.Cookies.Delete("refreshToken");
            return Results.NoContent();
        });

        group.MapPost("/refresh", async (
            HttpRequest request,
            HttpResponse response,
            IRefreshTokenService refreshTokenService,
            AppDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            IAccessTokenService accessTokenService,
            IOptions<RefreshTokenOptions> refreshTokenOptions) =>
            {
            if (!request.Cookies.TryGetValue("refreshToken", out var refreshToken)
            || string.IsNullOrWhiteSpace(refreshToken))
            {
                return Results.Unauthorized();
            }
            var hashedToken = refreshTokenService.HashRefreshToken(refreshToken);
            var now = DateTime.UtcNow;       
            var storedToken = await dbContext.RefreshTokens
                .FirstOrDefaultAsync(x=>x.TokenHash.SequenceEqual(hashedToken) 
                && x.ExpiresAtUtc > now 
                && x.RevokedAtUtc == null);
            if(storedToken is null)
            {
                return Results.Unauthorized();
            }

            var user = await userManager.FindByIdAsync(storedToken.UserId.ToString());
            if (user is null)
            {
                return Results.Unauthorized();
            }
            storedToken.RevokedAtUtc = now;
            var newRefreshToken = refreshTokenService.GenerateRefreshToken();
            var refreshTokenExpires = now.AddDays(refreshTokenOptions.Value.LifetimeDays);

            dbContext.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshTokenService.HashRefreshToken(newRefreshToken),
                CreatedAtUtc = now,
                ExpiresAtUtc = refreshTokenExpires
            });

            await dbContext.SaveChangesAsync();

            response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = refreshTokenExpires
            });

            var accessToken = accessTokenService.CreateToken(user);
            return Results.Ok(new LoginResponse(accessToken.AccessToken, accessToken.ExpiresAtUtc));
            });


        group.MapPost("/forgot-password",async(
        ForgotPasswordRequest request,
        IEmailSender emailSender,
        IConfiguration configuration,
        UserManager<ApplicationUser> userManager
        ) =>
        {
            if(string.IsNullOrWhiteSpace(request.Email))
                return Results.BadRequest("Podanie adresu Email jest wymagane");
            
            
            var user = await userManager.FindByEmailAsync(request.Email);
            if(user !=null)
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var url = configuration["Frontend:Origin"];
                var resetLink =
                    $"{url}/reset-password?userId={Uri.EscapeDataString(user.Id.ToString())}&token={Uri.EscapeDataString(token)}";
                await emailSender.SendPasswordResetEmailAsync(user.Email!,resetLink);
            }
            return Results.Ok("Wysłano powiadomienie email, jeśli konto istnieje");        
        });

        group.MapPost("/reset-password", async (
            ResetPasswordRequest request,
            UserManager<ApplicationUser> userManager
        ) =>
        {
            if(string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.Token))
                return Results.BadRequest("UserId, token i hasło są wymagane.");
            var user = await userManager.FindByIdAsync(request.UserId);
            if(user == null)
                return Results.BadRequest("Nieprawidłowy lub wygasły link resetowania hasła.");

            var res = await userManager.ResetPasswordAsync(user,request.Token,request.Password);
            if (!res.Succeeded)
            {
                return Results.BadRequest(res.Errors.Select(e => new
                {
                    e.Code,
                    e.Description
                }));
            }

            return Results.Ok();
        });


        group.MapPost("/register", async (
            RegisterRequest request,
            UserManager<ApplicationUser> userManager) =>
        {
            if(request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new[] { "Email i hasło są wymagane." });
            }
            var result = await userManager.CreateAsync(new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email
            }, request.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors
                    .Where(error => error.Code != "DuplicateUserName")
                    .Select(error => error.Description);

                return Results.BadRequest(errors);
            }

            return Results.Ok();
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


public record ForgotPasswordRequest
(
    string Email
);

public record ResetPasswordRequest(
    string UserId,
    string Token,
    string Password
);

public record RegisterRequest
(
    string Email,
    string Password
);
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
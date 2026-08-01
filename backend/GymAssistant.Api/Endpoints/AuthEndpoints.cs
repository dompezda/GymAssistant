using System.Runtime.CompilerServices;
using GymAssistant.Api.Data;
using GymAssistant.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace GymAssistant.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        // group.MapPost("/login", ...);
        // group.MapGet("/me", ...);
    }
}

public record LoginRequest(
    string Email,
    string Password
);
public record LoginResponse(
    string AccessToken,
    DateTime ExpiresAt
);
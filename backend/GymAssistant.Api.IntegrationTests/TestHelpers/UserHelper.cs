using GymAssistant.Api.Models;
using Microsoft.AspNetCore.Identity;

public static class UserHelper
{
    public static async Task<ApplicationUser> CreateUserAsync(UserManager<ApplicationUser> userManager)
    {
        var guid = Guid.NewGuid().ToString();
        var user = new ApplicationUser
        {
            UserName = guid,
            Email = $"{guid}@example.com"
        };
        var createUser = await userManager.CreateAsync(user, "Password123!");
        if (!createUser.Succeeded)
        {
            throw new InvalidOperationException("Nie udało się utworzyć użytkownika.");
        }
        return user;
    }
}
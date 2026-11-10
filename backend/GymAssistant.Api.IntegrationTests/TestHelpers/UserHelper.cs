using GymAssistant.Api.Models;
using Microsoft.AspNetCore.Identity;

public static class UserHelper
{
    public static async Task<ApplicationUser> CreateUserAsync(
    UserManager<ApplicationUser> userManager,
    string password = "Password123!")
    {
        var guid = Guid.NewGuid().ToString();

        var user = new ApplicationUser
        {
            UserName = guid,
            Email = $"{guid}@example.com"
        };

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Nie udało się utworzyć użytkownika: " +
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return user;
    }
}
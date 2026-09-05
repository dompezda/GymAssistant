using System.Net;
using System.Net.Http.Json;
using GymAssistant.Api.Authentication;
using GymAssistant.Api.Data;
using GymAssistant.Api.Endpoints;
using GymAssistant.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
public class ExercisesEndpointsTests
    : IClassFixture<GymAssistantWebApplicationFactory>
{
    
    private readonly HttpClient _client;
    private readonly GymAssistantWebApplicationFactory _factory;
    public ExercisesEndpointsTests(GymAssistantWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        
    }

    [Fact]
    public async Task GetExercises_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/exercises");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetExercises_ShouldNotReturnExercisesOwnedByOtherUser()
    {
        //Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user1 = new ApplicationUser
        {
            UserName = "testuser",
            Email = "testuser@example.com"
        };
        var user2 = new ApplicationUser
        {
            UserName = "otheruser",
            Email = "otheruser@example.com"
        };
        var res1 = await userManager.CreateAsync(user1, "Test@123");
        

        var res2 = await userManager.CreateAsync(user2, "Other@123");
        Assert.True(res1.Succeeded, "Failed to create test user.");
        Assert.True(res2.Succeeded, "Failed to create other user.");
        var exercise = new Exercise
        {
            Name = "Test Exercise",
            OwnerUserId = user1.Id
        };
        var exercise2 = new Exercise
        {
            Name = "Other User Exercise",
            OwnerUserId = user2.Id
        };

        dbContext.Exercises.Add(exercise);
        dbContext.Exercises.Add(exercise2);
        await dbContext.SaveChangesAsync();
        var token = scope.ServiceProvider.GetRequiredService<IAccessTokenService>().CreateToken(user1);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

        //Act
        var response = await _client.GetAsync("/api/exercises");

        //Assert
        response.EnsureSuccessStatusCode();
        var exercises = await response.Content.ReadFromJsonAsync<List<ExerciseListItemResponse>>();
        Assert.NotNull(exercises);
        Assert.Contains(exercises, e => e.Id == exercise.Id);
        Assert.DoesNotContain(exercises, e => e.Id == exercise2.Id);
    }

}
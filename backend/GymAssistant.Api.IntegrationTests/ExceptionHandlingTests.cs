using System.Net;
using System.Net.Http.Json;
using GymAssistant.Api.Authentication;
using GymAssistant.Api.Endpoints;
using GymAssistant.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public class ExceptionHandlingTests: IClassFixture<GymAssistantWebApplicationFactory>
{

    private readonly GymAssistantWebApplicationFactory _factory;
    public ExceptionHandlingTests(GymAssistantWebApplicationFactory factory)
    {
        _factory = factory;
        
    }
        
    [Fact]
    public async Task UnhandledException_ShouldReturnProblemDetailsWithoutSensitiveInformation()
    {

        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAccessTokenService>();
                services.AddSingleton<IAccessTokenService,ThrowingAccessTokenService>();
            });
        });
        await using var scope = factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        using var client = factory.CreateClient();
        var password = "Password123!";
        var user = await UserHelper.CreateUserAsync(userManager, password);

        using var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(user.Email!, password));

        Assert.Equal(HttpStatusCode.InternalServerError, loginResponse.StatusCode);
        var problem = await loginResponse.Content
        .ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem); 
        Assert.Equal("application/problem+json",loginResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(500,problem.Status);
        Assert.True(problem.Extensions.TryGetValue("traceId", out var traceId));
        Assert.NotNull(traceId);
        Assert.False(string.IsNullOrWhiteSpace(traceId.ToString()));
        var body = await loginResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Simulated service failure", body);
        Assert.DoesNotContain("InvalidOperationException", body);
    }

}
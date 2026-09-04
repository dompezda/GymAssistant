using System.Net;
public class ExercisesEndpointsTests
    : IClassFixture<GymAssistantWebApplicationFactory>
{
    
    private readonly HttpClient _client;

    public ExercisesEndpointsTests(GymAssistantWebApplicationFactory factory)
    {
        
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

}
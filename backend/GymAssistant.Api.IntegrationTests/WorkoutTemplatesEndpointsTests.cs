using System.Net.Http.Json;
using GymAssistant.Api.Authentication;
using GymAssistant.Api.Data;
using GymAssistant.Api.Endpoints;
using GymAssistant.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class WorkoutTemplatesEndpointsTests : IClassFixture<GymAssistantWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly GymAssistantWebApplicationFactory _factory;
    public WorkoutTemplatesEndpointsTests(GymAssistantWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    [Fact]
    public async Task CreateWorkoutTemplate_ShouldCreateTemplateOwnedByCurrentUser()
    {
        //Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await UserHelper.CreateUserAsync(userManager);
        var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);
        AuthHelper.AuthorizeClient(_client, user, scope.ServiceProvider.GetRequiredService<IAccessTokenService>());

        //Act
        var request = new CreateWorkoutTemplateRequest
        (
            "Test Workout Template",
            "Some notes",
            new List<CreateWorkoutTemplateExerciseRequest>
            {
                new CreateWorkoutTemplateExerciseRequest
                (
                    exercise.Id,
                    1,
                    1
                )
            }
        );
        var response = await _client.PostAsJsonAsync("/api/workout-templates", request);

        //Assert
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        var resContent = await response.Content.ReadFromJsonAsync<CreateWorkoutTemplateResponse>();
        Assert.NotNull(resContent);

        var template = await db.WorkoutTemplates.FirstOrDefaultAsync(x => x.Id == resContent.Id);
        Assert.NotNull(template);
        Assert.Equal(user.Id, template.UserId);
    }

    [Fact]
    public async Task CreateWorkoutTemplate_WithoutTitle_ShouldReturnBadRequest()
    {
        //arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await UserHelper.CreateUserAsync(userManager);
        var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);
        AuthHelper.AuthorizeClient(_client, user, scope.ServiceProvider.GetRequiredService<IAccessTokenService>());

        //act
        var request = new CreateWorkoutTemplateRequest
        (
            "",
            "Some notes",
            new List<CreateWorkoutTemplateExerciseRequest>
            {
                new CreateWorkoutTemplateExerciseRequest
                (
                    exercise.Id,
                    1,
                    1
                )
            }
        );
        var response = await _client.PostAsJsonAsync("/api/workout-templates", request);

        //assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateWorkoutTemplate_WithoutExercises_ShouldReturnBadRequest()
    {
        //arrange
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await UserHelper.CreateUserAsync(userManager);
        AuthHelper.AuthorizeClient(_client, user, scope.ServiceProvider.GetRequiredService<IAccessTokenService>());

        //act
        var request = new CreateWorkoutTemplateRequest
        (
            "Test Workout Template",
            "Some notes",
            new List<CreateWorkoutTemplateExerciseRequest>()
        );
        var response = await _client.PostAsJsonAsync("/api/workout-templates", request);

        //assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateWorkoutTemplate_WithNonPositivePlannedSets_ShouldReturnBadRequest()
    {
        //arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await UserHelper.CreateUserAsync(userManager);
        var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);
        AuthHelper.AuthorizeClient(_client, user, scope.ServiceProvider.GetRequiredService<IAccessTokenService>());

        //act
        var request = new CreateWorkoutTemplateRequest
        (
            "Test Workout Template",
            "Some notes",
            new List<CreateWorkoutTemplateExerciseRequest>
            {
                new CreateWorkoutTemplateExerciseRequest
                (
                    exercise.Id,
                    -1,
                    1
                )
            }
        );
        var response = await _client.PostAsJsonAsync("/api/workout-templates", request);

        //assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateWorkoutTemplate_WithOtherUsersExercise_ShouldReturnBadRequest()
    {
        //arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user1 = await UserHelper.CreateUserAsync(userManager);
        var user2 = await UserHelper.CreateUserAsync(userManager);
        var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user2);
        AuthHelper.AuthorizeClient(_client, user1, scope.ServiceProvider.GetRequiredService<IAccessTokenService>());

        //act
        var request = new CreateWorkoutTemplateRequest
        (
            "Test Workout Template",
            "Some notes",
            new List<CreateWorkoutTemplateExerciseRequest>
            {
                new CreateWorkoutTemplateExerciseRequest
                (
                    exercise.Id,
                    3,
                    1
                )
            }
        );
        var response = await _client.PostAsJsonAsync("/api/workout-templates", request);

        //assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateWorkoutTemplate_WithSystemExercise_ShouldCreateTemplate()
    {
        //arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await UserHelper.CreateUserAsync(userManager);
        var systemExercise = await ExerciseHelper.CreateSystemExerciseAsync(db);
        AuthHelper.AuthorizeClient(_client, user, scope.ServiceProvider.GetRequiredService<IAccessTokenService>());

        //act
        var request = new CreateWorkoutTemplateRequest
        (
            "Test Workout Template",
            "Some notes",
            new List<CreateWorkoutTemplateExerciseRequest>
            {
                new CreateWorkoutTemplateExerciseRequest
                (
                    systemExercise.Id,
                    3,
                    1
                )
            }
        );
        var response = await _client.PostAsJsonAsync("/api/workout-templates", request);

        //assert
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateWorkoutTemplate_WithDuplicateExercises_ShouldReturnBadRequest()
    {
        //arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await UserHelper.CreateUserAsync(userManager);
        var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);
        AuthHelper.AuthorizeClient(_client, user, scope.ServiceProvider.GetRequiredService<IAccessTokenService>());

        //act
        var request = new CreateWorkoutTemplateRequest
        (
            "Test Workout Template",
            "Some notes",
            new List<CreateWorkoutTemplateExerciseRequest>
            {
                new CreateWorkoutTemplateExerciseRequest
                (
                    exercise.Id,
                    3,
                    1
                ),
                new CreateWorkoutTemplateExerciseRequest
                (
                    exercise.Id,
                    2,
                    2
                )
            }
        );
        var response = await _client.PostAsJsonAsync("/api/workout-templates", request);

        //assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<string>();
        Assert.Equal("Plan treningowy nie może zawierać tego samego ćwiczenia więcej niż raz.", error);
    }

    [Fact]
    public async Task CreateWorkoutTemplate_ShouldNormalizeExerciseOrder()
    {
        //arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await UserHelper.CreateUserAsync(userManager);
        var exercise1 = await ExerciseHelper.CreateUserExerciseAsync(db, user);
        var exercise2 = await ExerciseHelper.CreateUserExerciseAsync(db, user);
        var exercise3 = await ExerciseHelper.CreateUserExerciseAsync(db, user);
        AuthHelper.AuthorizeClient(_client, user, scope.ServiceProvider.GetRequiredService<IAccessTokenService>());

        //act
        var request = new CreateWorkoutTemplateRequest
        (
            "Test Workout Template",
            "Some notes",
            new List<CreateWorkoutTemplateExerciseRequest>
            {
                new CreateWorkoutTemplateExerciseRequest
                (
                    exercise1.Id,
                    3,
                    5
                ),
                new CreateWorkoutTemplateExerciseRequest
                (
                    exercise2.Id,
                    2,
                    1
                ),
                new CreateWorkoutTemplateExerciseRequest
                (
                    exercise3.Id,
                    4,
                    3
                )
            }
        );

        var response = await _client.PostAsJsonAsync("/api/workout-templates", request);

        //assert
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        var resContent = await response.Content.ReadFromJsonAsync<CreateWorkoutTemplateResponse>();
        Assert.NotNull(resContent);

        var templateFromDb = await db.WorkoutTemplates
            .Include(x => x.Exercises)
            .FirstOrDefaultAsync(x => x.Id == resContent.Id);

        Assert.NotNull(templateFromDb);
        var orderedExercises = templateFromDb.Exercises.OrderBy(x => x.Order).ToList();

        Assert.Equal(
            new[] { exercise2.Id, exercise3.Id, exercise1.Id },
            orderedExercises.Select(x => x.ExerciseId));

        Assert.Equal(
            new[] { 1, 2, 3 },
            orderedExercises.Select(x => x.Order));
    }

    [Fact]
    public async Task GetWorkoutTemplates_ShouldReturnOnlyCurrentUsersTemplates()
    {
        //arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user1 = await UserHelper.CreateUserAsync(userManager);
        var user2 = await UserHelper.CreateUserAsync(userManager);
        var exercise1 = await ExerciseHelper.CreateUserExerciseAsync(db, user1);
        var exercise2 = await ExerciseHelper.CreateUserExerciseAsync(db, user2);
        var template1 = await WorkoutTemplateHelper.CreateWorkoutTemplateAsync(db, user1, [exercise1]);
        await WorkoutTemplateHelper.CreateWorkoutTemplateAsync(db, user2, [exercise2]);
        AuthHelper.AuthorizeClient(_client, user1, scope.ServiceProvider.GetRequiredService<IAccessTokenService>());
        //act
        var response = await _client.GetAsync("/api/workout-templates");
        //assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var templates = await response.Content.ReadFromJsonAsync<List<WorkoutTemplateListItemResponse>>();
        Assert.NotNull(templates);
        Assert.Single(templates);
        Assert.Equal(template1.Title, templates[0].Title);
    }

    [Fact]
    public async Task GetWorkoutTemplate_ShouldReturnNotFound_ForOtherUsersTemplate()
    {
        //arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user1 = await UserHelper.CreateUserAsync(userManager);
        var user2 = await UserHelper.CreateUserAsync(userManager);
        var exercise = await ExerciseHelper.CreateSystemExerciseAsync(db);
        var template = await WorkoutTemplateHelper.CreateWorkoutTemplateAsync(db, user2, [exercise]);

        AuthHelper.AuthorizeClient(_client, user1, scope.ServiceProvider.GetRequiredService<IAccessTokenService>());

        //act
        var response = await _client.GetAsync($"/api/workout-templates/{template.Id}");

        //assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

}
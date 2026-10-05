using System.Net.Http.Headers;
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
        var guid = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = $"user{guid}",
            Email = $"test{guid}@example.com"
        };
        var userCreate = await userManager.CreateAsync(user, "Password123!");
        Assert.True(userCreate.Succeeded, "Failed to create user for test.");

        var exercise = new Exercise
        {
            Name = "Test Exercise",
            OwnerUserId = user.Id,
            IsSystem = false
        };
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var token = scope.ServiceProvider.GetRequiredService<IAccessTokenService>().CreateToken(user);
        var authHeader = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        _client.DefaultRequestHeaders.Authorization = authHeader;
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
        var template = await db.WorkoutTemplates.FirstOrDefaultAsync(x=>x.Id == resContent.Id);
        Assert.NotNull(template);
        Assert.Equal(user.Id, template.UserId);
    }

    [Fact]
    public async Task CreateWorkoutTemplate_WithoutTitle_ShouldReturnBadRequest()
    {
        //arrange
        var guid = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = $"user{guid}",
            Email = $"test{guid}@example.com"
        };
        var userCreate = await userManager.CreateAsync(user, "Password123!");
        Assert.True(userCreate.Succeeded, "Failed to create user for test.");
        var exercise = new Exercise
        {
            Name = "Test Exercise",
            OwnerUserId = user.Id,
            IsSystem = false
        };
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var token = scope.ServiceProvider.GetRequiredService<IAccessTokenService>().CreateToken(user);
        var authHeader = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        _client.DefaultRequestHeaders.Authorization = authHeader;
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
        var guid = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = $"user{guid}",
            Email = $"email{guid}@example.com"
        };
        var userCreate = await userManager.CreateAsync(user, "Password123!");
        Assert.True(userCreate.Succeeded, "Failed to create user for test.");
        var token = scope.ServiceProvider.GetRequiredService<IAccessTokenService>().CreateToken(user);
        var authHeader = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        _client.DefaultRequestHeaders.Authorization = authHeader;
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
        var guid = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = $"user{guid}",
            Email = $"email{guid}@example.com"
        };
        var userCreate = await userManager.CreateAsync(user, "Password123!");
        Assert.True(userCreate.Succeeded, "Failed to create user for test.");

        var exercise = new Exercise
        {
            Name = $"Test Exercise {guid}",
            OwnerUserId = user.Id,
            IsSystem = false
        };
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        
        var token = scope.ServiceProvider.GetRequiredService<IAccessTokenService>().CreateToken(user);
        var authHeader = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        _client.DefaultRequestHeaders.Authorization = authHeader;
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
        var guid = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user1 = new ApplicationUser
        {
            UserName = $"user1{guid}",
            Email = $"email1{guid}@example.com"
        };
        var user1Create = await userManager.CreateAsync(user1, "Password123!");
        Assert.True(user1Create.Succeeded, "Failed to create user for test.");

        var user2 = new ApplicationUser
        {
            UserName = $"user2{guid}",
            Email = $"email2{guid}@example.com"
        };
        var user2Create = await userManager.CreateAsync(user2, "Password123!");
        Assert.True(user2Create.Succeeded, "Failed to create user for test.");

        var exercise = new Exercise
        {
            Name = $"Test Exercise {guid}",
            OwnerUserId = user2.Id,
            IsSystem = false
        };
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();
        var token = scope.ServiceProvider.GetRequiredService<IAccessTokenService>().CreateToken(user1);
        var authHeader = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        _client.DefaultRequestHeaders.Authorization = authHeader;
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
        var guid = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = $"user{guid}",
            Email = $"email{guid}@example.com"
        };
        var userCreate = await userManager.CreateAsync(user, "Password123!");
        Assert.True(userCreate.Succeeded, "Failed to create user for test.");

        var systemExercise = new Exercise
        {
            Name = $"System Exercise {guid}",
            OwnerUserId = null,
            IsSystem = true
        };
        db.Exercises.Add(systemExercise);
        await db.SaveChangesAsync();
        var token = scope.ServiceProvider.GetRequiredService<IAccessTokenService>().CreateToken(user);
        var authHeader = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        _client.DefaultRequestHeaders.Authorization = authHeader;
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
        var guid = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = $"user{guid}",
            Email = $"email{guid}@example.com"
        };

        var userCreate = await userManager.CreateAsync(user, "Password123!");
        Assert.True(userCreate.Succeeded, "Failed to create user for test.");

        var exercise1 = new Exercise
        {
            Name = $"Test Exercise {guid}",
            OwnerUserId = user.Id,
            IsSystem = false
        };
        db.Exercises.Add(exercise1);
        await db.SaveChangesAsync();

        var token = scope.ServiceProvider.GetRequiredService<IAccessTokenService>().CreateToken(user);
        var authHeader = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        _client.DefaultRequestHeaders.Authorization = authHeader;
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
                    1
                ),
                new CreateWorkoutTemplateExerciseRequest
                (
                    exercise1.Id, // Duplicate exercise
                    2,
                    2
                )
            }
        );
        var response = await _client.PostAsJsonAsync("/api/workout-templates", request);
        //assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<string>();
        Assert.Equal("Plan treningowy nie może zawierać tego samego ćwiczenia więcej niż raz.",error);
    }

    [Fact]
    public async Task CreateWorkoutTemplate_ShouldNormalizeExerciseOrder()
    {
        //arrange
        var guid = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = $"user{guid}",
            Email = $"email{guid}@example.com"
        };
        var createUserResult = await userManager.CreateAsync(user, "Password123!");
        Assert.True(createUserResult.Succeeded, "Failed to create user for test.");

        var exercise1 = new Exercise
        {
            Name = $"Test Exercise 1 {guid}",
            OwnerUserId = user.Id,
            IsSystem = false
        };
        db.Exercises.Add(exercise1);
        var exercise2 = new Exercise
        {
            Name = $"Test Exercise 2 {guid}",
            OwnerUserId = user.Id,
            IsSystem = false
        };
        db.Exercises.Add(exercise2);
        var exercise3 = new Exercise
        {
            Name = $"Test Exercise 3 {guid}",
            OwnerUserId = user.Id,
            IsSystem = false
        };
        db.Exercises.Add(exercise3);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var token = scope.ServiceProvider.GetRequiredService<IAccessTokenService>().CreateToken(user);
        var authHeader = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        _client.DefaultRequestHeaders.Authorization = authHeader;

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
                    5 // Out of order
                ),
                new CreateWorkoutTemplateExerciseRequest
                (
                    exercise2.Id,
                    2,
                    1 // Out of order
                ),
                new CreateWorkoutTemplateExerciseRequest
                (
                    exercise3.Id,
                    4,
                    3 // Out of order
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
        //arrage
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var guid = Guid.NewGuid();

        var user1 = new ApplicationUser
        {
            UserName = $"user1{guid}",
            Email = $"email{guid}@example.com"
        };

        var createUser1Result = await userManager.CreateAsync(user1, "Password123!");
        Assert.True(createUser1Result.Succeeded, "Failed to create user1 for test.");

        var user2 = new ApplicationUser
        {
            UserName = $"user2{guid}",
            Email = $"email2{guid}@example.com"
        };
        var createUser2Result = await userManager.CreateAsync(user2, "Password123!");
        Assert.True(createUser2Result.Succeeded, "Failed to create user2 for test.");

        var exercise1 = new Exercise
        {
            Name = $"Test Exercise 1 {guid}",
            OwnerUserId = user1.Id,
            IsSystem = false
        };
        var exercise2 = new Exercise
        {
            Name = $"Test Exercise 2 {guid}",
            OwnerUserId = user2.Id,
            IsSystem = false
        };
        db.Exercises.AddRange(exercise1, exercise2);
        await db.SaveChangesAsync();

        var template1 = new WorkoutTemplate
        {
            Title = "User 1 Template",
            UserId = user1.Id,
            Exercises = new List<WorkoutTemplateExercise>
            {
                new WorkoutTemplateExercise
                {
                    ExerciseId = exercise1.Id,
                    Order = 1,
                    PlannedSets = 3
                }
            }
        };
        var template2 = new WorkoutTemplate
        {
            Title = "User 2 Template",
            UserId = user2.Id,
            Exercises = new List<WorkoutTemplateExercise>
            {
                new WorkoutTemplateExercise
                {
                    ExerciseId = exercise2.Id,
                    Order = 1,
                    PlannedSets = 4
                }
            }
        };
        db.WorkoutTemplates.AddRange(template1, template2);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var token = scope.ServiceProvider.GetRequiredService<IAccessTokenService>().CreateToken(user1);
        var authHeader = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        _client.DefaultRequestHeaders.Authorization = authHeader;
        
        //act
        var response = await _client.GetAsync("/api/workout-templates");
        //assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var templates = await response.Content.ReadFromJsonAsync<List<WorkoutTemplateListItemResponse>>();Assert.NotNull(templates);
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
        var guid = Guid.NewGuid();

        var user1 = new ApplicationUser
        {
            UserName = $"user1{guid}",
            Email = $"email1{guid}@example.com"
        };
        var createUser1Result = await userManager.CreateAsync(user1, "Password123!");
        Assert.True(createUser1Result.Succeeded, "Failed to create user1 for test.");

        var user2 = new ApplicationUser
        {
            UserName = $"user2{guid}",
            Email = $"email2{guid}@example.com"
        };
        var createUser2Result = await userManager.CreateAsync(user2, "Password123!");
        Assert.True(createUser2Result.Succeeded, "Failed to create user2 for test.");

        var exercise1 = new Exercise
        {
            Name = $"Test Exercise 1 {guid}",
            OwnerUserId = null,
            IsSystem = true
        };
        await db.Exercises.AddAsync(exercise1);
        await db.SaveChangesAsync();

        var template = new WorkoutTemplate
        {
            Title = "User 2 Template",
            UserId = user2.Id,
            Exercises = new List<WorkoutTemplateExercise>
            {
                new WorkoutTemplateExercise
                {
                    ExerciseId = exercise1.Id,
                    Order = 1,
                    PlannedSets = 4
                }
            }
        };
        db.WorkoutTemplates.Add(template);
        await db.SaveChangesAsync();

        var token = scope.ServiceProvider.GetRequiredService<IAccessTokenService>().CreateToken(user1);
        var authHeader = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        _client.DefaultRequestHeaders.Authorization = authHeader;
        //act
        var response = await _client.GetAsync($"/api/workout-templates/{template.Id}");
        //assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
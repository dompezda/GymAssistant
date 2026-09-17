using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GymAssistant.Api.Authentication;
using GymAssistant.Api.Data;
using GymAssistant.Api.Endpoints;
using GymAssistant.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        var suffix = Guid.NewGuid().ToString();
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user1 = new ApplicationUser
        {
            UserName = $"{suffix}-1",
            Email = $"testuser-{suffix}@example.com"
        };
        var user2 = new ApplicationUser
        {
            UserName = $"{suffix}-2",
            Email = $"otheruser-{suffix}@example.com"
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
        var exercises = await response.Content.ReadFromJsonAsync<List<ExerciseResponse>>();
        Assert.NotNull(exercises);
        Assert.Contains(exercises, e => e.Id == exercise.Id);
        Assert.DoesNotContain(exercises, e => e.Id == exercise2.Id);
    }

    [Fact]
    public async Task GetExercises_ShouldReturnCurrentUserAndSystemExercises()
    {
     //Arrange
     HttpClient client = _factory.CreateClient();
     using var scope = _factory.Services.CreateScope();
     var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();  
     var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
     var suffix = Guid.NewGuid().ToString();
     var user = new ApplicationUser
     {
         UserName = "user1" + suffix,
         Email = $"user1-{suffix}@app.com"
     };
     var user2 = new ApplicationUser
     {
         UserName = "user2" + suffix,
         Email = $"user2-{suffix}@app.com"
     };
     var u1 = await userManager.CreateAsync(user, "Test@123");
     var u2 = await userManager.CreateAsync(user2, "Test@123");
     Assert.True(u1.Succeeded, "Failed to create test user1.");
     Assert.True(u2.Succeeded, "Failed to create test user2.");

     var exercise1 = new Exercise
     {
         Name = "Test Exercise 1",
         OwnerUserId = user.Id,
         IsSystem = false
     };
     var exercise2 = new Exercise
     {
         Name = "Test Exercise 2",
         OwnerUserId = user2.Id,
         IsSystem = false
     };
     var exercise3 = new Exercise
     {
         Name = "System Exercise",
         OwnerUserId = null,
         IsSystem = true
     };
    dbContext.Exercises.Add(exercise1);
    dbContext.Exercises.Add(exercise2);
    dbContext.Exercises.Add(exercise3);
    await dbContext.SaveChangesAsync();

    var token = scope.ServiceProvider.GetRequiredService<IAccessTokenService>().CreateToken(user).AccessToken;
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    
    
     //Act
     var response = await client.GetAsync("/api/exercises");
     Assert.Equal(HttpStatusCode.OK, response.StatusCode);
     var exercises = await response.Content.ReadFromJsonAsync<List<ExerciseResponse>>();
     //Assert 
     Assert.NotNull(exercises); 
     Assert.NotEmpty(exercises);
     Assert.Contains(exercises, e => e.Id == exercise1.Id);
     Assert.DoesNotContain(exercises, e => e.Id == exercise2.Id);
     Assert.Contains(exercises, e => e.Id == exercise3.Id);
    }


    [Fact]
    public async Task CreateExercise_ShouldCreateExerciseOwnedByCurrentUser()
    {
        // Arrange
        var client = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString();
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new ApplicationUser
        {
            UserName = $"{suffix}",
            Email = $"{suffix}@app.com"
        };
        var result = await userManager.CreateAsync(user, "Test@123");
        Assert.True(result.Succeeded, "Failed to create test user.");
        CreateExerciseRequest request = new CreateExerciseRequest
        (
            Guid.NewGuid().ToString(),
            [(int)MuscleGroup.Chest]
        );

        var token = scope.ServiceProvider.GetRequiredService<IAccessTokenService>().CreateToken(user).AccessToken;
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/api/exercises", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdExercise = await response.Content.ReadFromJsonAsync<ExerciseResponse>();

        // Assert
        Assert.NotNull(createdExercise);
        var exerciseInDb = await dbContext.Exercises.FindAsync(createdExercise.Id);
        Assert.NotNull(exerciseInDb);
        Assert.Equal(user.Id, exerciseInDb.OwnerUserId);
        Assert.False(exerciseInDb.IsSystem);
    }

    [Fact]
    public async Task CreateExercise_ShouldReturnBadRequest_WhenNameIsEmpty()
    {
        // Arrange
        var client = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString();
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = $"{suffix}",
            Email = $"{suffix}1@gmail.com"
        };
        var result = await userManager.CreateAsync(user, "Test@123");
        Assert.True(result.Succeeded, "Failed to create test user.");
        var token = scope.ServiceProvider.GetRequiredService<IAccessTokenService>().CreateToken(user).AccessToken;
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new CreateExerciseRequest
        (
            "",
            [(int)MuscleGroup.Chest]
        );
        //Act
        var response = await client.PostAsJsonAsync("/api/exercises", request);

        //Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errorMessage = await response.Content.ReadFromJsonAsync<string>();
        Assert.Equal("Nazwa ćwiczenia jest wymagana.", errorMessage);

    }

    [Fact]
    public async Task EditExercise_ShouldUpdateCurrentUsersExercise()
    {

        //Arrange
        var suffix = Guid.NewGuid().ToString();
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var client = _factory.CreateClient();
        var user = new ApplicationUser
        {
            UserName = $"edit-{suffix}",
            Email = $"edit-{suffix}@gmail.com"  
        };
        var result = await userManager.CreateAsync(user);
        Assert.True(result.Succeeded);

        var exercise1 = new Exercise
        {
            Name = $"{suffix}-exercise1",
            OwnerUserId = user.Id,
            IsSystem = false,
            ExerciseMuscleGroups =
            [
                new ExerciseMuscleGroup()
                {
                    MuscleGroup = MuscleGroup.Chest
                },
                new ExerciseMuscleGroup()
                {
                    MuscleGroup = MuscleGroup.FullBody
                }
            ]
        };
        db.Exercises.Add(exercise1);
        await db.SaveChangesAsync();
        var request = new EditExerciseRequest($"{suffix}",new[]
        {
            (int)MuscleGroup.Chest,
            (int)MuscleGroup.Back,
            (int)MuscleGroup.Legs
        });

        var token = scope.ServiceProvider
            .GetRequiredService<IAccessTokenService>()
            .CreateToken(user)
            .AccessToken;

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        //Act
        var response = await client.PatchAsJsonAsync($"/api/exercises/{exercise1.Id}", request);


        db.ChangeTracker.Clear();


        //Assert
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        var ex = await db.Exercises
            .Include(x => x.ExerciseMuscleGroups)
            .FirstOrDefaultAsync(x => x.Id == exercise1.Id);
        Assert.NotNull(ex);
        Assert.Equal(request.Name,ex.Name);

        var actualMuscleGroups = ex.ExerciseMuscleGroups
            .Select(x => (int)x.MuscleGroup)
            .OrderBy(x => x)
            .ToArray();
        var expectedMuscleGroups = request.MuscleGroups
            .OrderBy(x => x)
            .ToArray();
        Assert.Equal(expectedMuscleGroups, actualMuscleGroups);
    }

    [Fact]
    public async Task EditOtherUserExercise_ShouldReturnNotFound()
    {

        //Arrange
        var suffix = Guid.NewGuid().ToString();
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var client = _factory.CreateClient();
        var user1 = new ApplicationUser()
        {
            UserName = $"{suffix}-user1",
            Email = $"{suffix}-user1@gmail.com"
        };
        var user2 = new ApplicationUser()
        {
            UserName = $"{suffix}-user2",
            Email = $"{suffix}-user2@gmail.com"
        };
        var user1Result = await userManager.CreateAsync(user1);
        var user2Result = await userManager.CreateAsync(user2);
        Assert.True(user1Result.Succeeded);
        Assert.True(user2Result.Succeeded);

        var exercise1 = new Exercise()
        {
            Name = $"{suffix}-exercise1",
            OwnerUserId = user1.Id,
            ExerciseMuscleGroups =
            [
                new ExerciseMuscleGroup
                {
                    MuscleGroup = MuscleGroup.Chest    
                },
                new ExerciseMuscleGroup
                {
                    MuscleGroup = MuscleGroup.Back
                }
            ]
        };
        db.Exercises.Add(exercise1);
        await db.SaveChangesAsync();
        
        string originalName = exercise1.Name;
        var originalGroups = exercise1.ExerciseMuscleGroups
            .Select(x => x.MuscleGroup)
            .OrderBy(x => x)
            .ToArray();
        var token = scope.ServiceProvider
            .GetRequiredService<IAccessTokenService>()
            .CreateToken(user2)
            .AccessToken;

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        //Act
        var request = new EditExerciseRequest($"{suffix}",new int[]
        {
            (int)MuscleGroup.FullBody
        });

        var response = await client.PatchAsJsonAsync($"/api/exercises/{exercise1.Id}",request);
        Assert.Equal(HttpStatusCode.NotFound,response.StatusCode);
        
        db.ChangeTracker.Clear();

        var ex = await db.Exercises
            .Include(x => x.ExerciseMuscleGroups)
            .FirstOrDefaultAsync(x => x.Id == exercise1.Id);
        Assert.NotNull(ex);
        Assert.Equal(originalName,ex.Name);
        var actualGroups = ex.ExerciseMuscleGroups
            .Select(x => x.MuscleGroup)
            .OrderBy(x => x)
            .ToArray();

        Assert.Equal(originalGroups, actualGroups);
    
    }


    [Fact]
    public async Task EditSystemExercise_ShouldReturnNotFound()
    {
        //Arrange
        var suffix = Guid.NewGuid().ToString();
        using var scope = _factory.Services.CreateScope();
        var client = _factory.CreateClient();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var user = new ApplicationUser()
        {
            UserName=$"{suffix}-user1",
            Email=$"{suffix}@gmail.com"
        };
        var result = await userManager.CreateAsync(user);
        Assert.True(result.Succeeded);

        var exercise = new Exercise()
        {
            Name = $"{suffix}-exercise1",
            IsSystem = true,
            OwnerUserId=null,
            ExerciseMuscleGroups =
            [
                new ExerciseMuscleGroup
                {
                    MuscleGroup = MuscleGroup.Chest    
                },
                new ExerciseMuscleGroup
                {
                    MuscleGroup = MuscleGroup.Back
                }
            ]
        };
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();

        var token = scope.ServiceProvider.GetRequiredService<IAccessTokenService>()
            .CreateToken(user)
            .AccessToken;
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);


        var originalName = exercise.Name;

        var originalGroups = exercise.ExerciseMuscleGroups
            .Select(x => x.MuscleGroup)
            .OrderBy(x => x)
            .ToArray();
        var request = new EditExerciseRequest($"{suffix}=EditMyExercise",
        [
         (int)MuscleGroup.Chest,
         (int)MuscleGroup.Core   
        ]);
        
        //Act
        var response = await client.PatchAsJsonAsync($"/api/exercises/{exercise.Id}",request);


        //Assert
        Assert.Equal(HttpStatusCode.NotFound,response.StatusCode);
        db.ChangeTracker.Clear();

        var ex = await db.Exercises
            .Include(x=>x.ExerciseMuscleGroups)
            .FirstOrDefaultAsync(x=>x.Id== exercise.Id);
        Assert.NotNull(ex);
        var currentGroups = ex!.ExerciseMuscleGroups
            .Select(x => x.MuscleGroup)
            .OrderBy(x => x)
            .ToArray();

        Assert.Equal(originalName, ex.Name);
        Assert.Equal(originalGroups, currentGroups);
        
    }
}
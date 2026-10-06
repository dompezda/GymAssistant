using System.Net;
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
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user1 = await UserHelper.CreateUserAsync(userManager);
        var user2 = await UserHelper.CreateUserAsync(userManager);
        var exercise = await ExerciseHelper.CreateUserExerciseAsync(dbContext, user1);
        var exercise2 = await ExerciseHelper.CreateUserExerciseAsync(dbContext, user2);
        AuthHelper.AuthorizeClient(_client, user1, scope.ServiceProvider.GetRequiredService<IAccessTokenService>());
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
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await UserHelper.CreateUserAsync(userManager);
        var user2 = await UserHelper.CreateUserAsync(userManager);
        var exercise1 = await ExerciseHelper.CreateUserExerciseAsync(dbContext, user);
        var exercise2 = await ExerciseHelper.CreateUserExerciseAsync(dbContext, user2);
        var exercise3 = await ExerciseHelper.CreateSystemExerciseAsync(dbContext);

        AuthHelper.AuthorizeClient(_client, user, scope.ServiceProvider.GetRequiredService<IAccessTokenService>());

        //Act
        var response = await _client.GetAsync("/api/exercises");

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var exercises = await response.Content.ReadFromJsonAsync<List<ExerciseResponse>>();
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
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await UserHelper.CreateUserAsync(userManager);
        var request = new CreateExerciseRequest
        (
            Guid.NewGuid().ToString(),
            [(int)MuscleGroup.Chest]
        );

        AuthHelper.AuthorizeClient(_client, user, scope.ServiceProvider.GetRequiredService<IAccessTokenService>());

        // Act
        var response = await _client.PostAsJsonAsync("/api/exercises", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdExercise = await response.Content.ReadFromJsonAsync<ExerciseResponse>();
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
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await UserHelper.CreateUserAsync(userManager);
        AuthHelper.AuthorizeClient(_client, user, scope.ServiceProvider.GetRequiredService<IAccessTokenService>());

        var request = new CreateExerciseRequest
        (
            "",
            [(int)MuscleGroup.Chest]
        );

        //Act
        var response = await _client.PostAsJsonAsync("/api/exercises", request);

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

        var user = await UserHelper.CreateUserAsync(userManager);
        var exercise1 = await ExerciseHelper.CreateUserExerciseAsync(db, user, new[]
        {
            MuscleGroup.Chest,
            MuscleGroup.FullBody
        });
        await db.SaveChangesAsync();

        var request = new EditExerciseRequest($"{suffix}", new[]
        {
            (int)MuscleGroup.Chest,
            (int)MuscleGroup.Back,
            (int)MuscleGroup.Legs
        });

        AuthHelper.AuthorizeClient(_client, user, scope.ServiceProvider.GetRequiredService<IAccessTokenService>());

        //Act
        var response = await _client.PatchAsJsonAsync($"/api/exercises/{exercise1.Id}", request);

        db.ChangeTracker.Clear();

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ex = await db.Exercises
            .Include(x => x.ExerciseMuscleGroups)
            .FirstOrDefaultAsync(x => x.Id == exercise1.Id);
        Assert.NotNull(ex);
        Assert.Equal(request.Name, ex.Name);

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

        var user1 = await UserHelper.CreateUserAsync(userManager);
        var user2 = await UserHelper.CreateUserAsync(userManager);
        var exercise1 = await ExerciseHelper.CreateUserExerciseAsync(db, user1);
        exercise1.ExerciseMuscleGroups =
        [
            new ExerciseMuscleGroup
            {
                MuscleGroup = MuscleGroup.Chest
            },
            new ExerciseMuscleGroup
            {
                MuscleGroup = MuscleGroup.Back
            }
        ];
        await db.SaveChangesAsync();

        string originalName = exercise1.Name;
        var originalGroups = exercise1.ExerciseMuscleGroups
            .Select(x => x.MuscleGroup)
            .OrderBy(x => x)
            .ToArray();

        AuthHelper.AuthorizeClient(_client, user2, scope.ServiceProvider.GetRequiredService<IAccessTokenService>());

        //Act
        var request = new EditExerciseRequest($"{suffix}", new int[]
        {
            (int)MuscleGroup.FullBody
        });
        var response = await _client.PatchAsJsonAsync($"/api/exercises/{exercise1.Id}", request);

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        db.ChangeTracker.Clear();

        var ex = await db.Exercises
            .Include(x => x.ExerciseMuscleGroups)
            .FirstOrDefaultAsync(x => x.Id == exercise1.Id);
        Assert.NotNull(ex);
        Assert.Equal(originalName, ex.Name);

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
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await UserHelper.CreateUserAsync(userManager);
        var exercise = await ExerciseHelper.CreateSystemExerciseAsync(db);
        exercise.ExerciseMuscleGroups =
        [
            new ExerciseMuscleGroup
            {
                MuscleGroup = MuscleGroup.Chest
            },
            new ExerciseMuscleGroup
            {
                MuscleGroup = MuscleGroup.Back
            }
        ];
        await db.SaveChangesAsync();

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

        AuthHelper.AuthorizeClient(_client, user, scope.ServiceProvider.GetRequiredService<IAccessTokenService>());

        //Act
        var response = await _client.PatchAsJsonAsync($"/api/exercises/{exercise.Id}", request);

        //Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        db.ChangeTracker.Clear();

        var ex = await db.Exercises
            .Include(x => x.ExerciseMuscleGroups)
            .FirstOrDefaultAsync(x => x.Id == exercise.Id);
        Assert.NotNull(ex);

        var currentGroups = ex.ExerciseMuscleGroups
            .Select(x => x.MuscleGroup)
            .OrderBy(x => x)
            .ToArray();

        Assert.Equal(originalName, ex.Name);
        Assert.Equal(originalGroups, currentGroups);
    }

    [Fact]
    public async Task PatchMuscleGroups_WithSameGroups_ShouldNotCreateDuplicates()
    {
        //Arrange
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await UserHelper.CreateUserAsync(userManager);
        var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);
        exercise.ExerciseMuscleGroups =
        [
            new ExerciseMuscleGroup
            {
                MuscleGroup = MuscleGroup.Chest
            }
        ];
        await db.SaveChangesAsync();

        AuthHelper.AuthorizeClient(_client, user, scope.ServiceProvider.GetRequiredService<IAccessTokenService>());

        //Act
        var request = new EditExerciseRequest(exercise.Name,
        [
            (int)MuscleGroup.Chest
        ]);
        var response = await _client.PatchAsJsonAsync($"/api/exercises/{exercise.Id}", request);

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        db.ChangeTracker.Clear();

        var currentExercise = await db.Exercises
            .Include(x => x.ExerciseMuscleGroups)
            .FirstOrDefaultAsync(x => x.Id == exercise.Id);
        Assert.NotNull(currentExercise);

        var relation = Assert.Single(currentExercise.ExerciseMuscleGroups);
        Assert.Equal(MuscleGroup.Chest, relation.MuscleGroup);
    }

    [Fact]
    public async Task PatchMuscleGroups_WithNewGroup_ShouldAddOnlyMissingGroup()
    {
        //Arrange
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await UserHelper.CreateUserAsync(userManager);
        var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);
        exercise.ExerciseMuscleGroups =
        [
            new ExerciseMuscleGroup
            {
                MuscleGroup = MuscleGroup.Chest
            }
        ];
        await db.SaveChangesAsync();

        AuthHelper.AuthorizeClient(_client, user, scope.ServiceProvider.GetRequiredService<IAccessTokenService>());

        var request = new EditExerciseRequest(exercise.Name,
        [
            (int)MuscleGroup.Chest,
            (int)MuscleGroup.Triceps
        ]);

        //Act
        var response = await _client.PatchAsJsonAsync($"/api/exercises/{exercise.Id}", request);

        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        db.ChangeTracker.Clear();

        var ex = await db.Exercises
            .Include(x => x.ExerciseMuscleGroups)
            .FirstOrDefaultAsync(x => x.Id == exercise.Id);
        Assert.NotNull(ex);

        var actualMuscleGroups = ex.ExerciseMuscleGroups
            .Select(x => (int)x.MuscleGroup)
            .OrderBy(x => x);

        var expectedMuscleGroups = request.MuscleGroups.OrderBy(x => x);

        Assert.Equal(expectedMuscleGroups, actualMuscleGroups);
    }

    [Fact]
    public async Task GetExerciseHistory_ForSystemExercise_ShouldReturnOnlyCurrentUsersWorkouts()
    {
        //arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await UserHelper.CreateUserAsync(userManager);
        var user2 = await UserHelper.CreateUserAsync(userManager);
        var exercise = await ExerciseHelper.CreateSystemExerciseAsync(db);
        exercise.ExerciseMuscleGroups =
        [
            new ExerciseMuscleGroup
            {
                MuscleGroup = MuscleGroup.Back
            },
            new ExerciseMuscleGroup
            {
                MuscleGroup = MuscleGroup.Biceps
            }
        ];
        await db.SaveChangesAsync();

        var workout = new WorkoutSession
        {
            User = user,
            WorkoutTemplate = null,
            Exercises = new List<WorkoutSessionExercise>
            {
                new WorkoutSessionExercise
                {
                    Exercise = exercise,
                    Order = 1,
                    Sets = new List<WorkoutSessionSet>
                    {
                        new WorkoutSessionSet
                        {
                            Reps = 5,
                            SetNumber = 1,
                            Weight = 8
                        },
                        new WorkoutSessionSet
                        {
                            Reps = 12,
                            SetNumber = 2,
                            Weight = 18
                        }
                    }
                }
            }
        };
        db.WorkoutSessions.Add(workout);
        await db.SaveChangesAsync();

        var workoutUser2 = new WorkoutSession
        {
            User = user2,
            WorkoutTemplate = null,
            Exercises = new List<WorkoutSessionExercise>
            {
                new WorkoutSessionExercise
                {
                    Exercise = exercise,
                    Order = 1,
                    Sets = new List<WorkoutSessionSet>
                    {
                        new WorkoutSessionSet
                        {
                            Reps = 100,
                            SetNumber = 1,
                            Weight = 200
                        },
                        new WorkoutSessionSet
                        {
                            Reps = 120,
                            SetNumber = 2,
                            Weight = 180
                        }
                    }
                }
            }
        };
        db.WorkoutSessions.Add(workoutUser2);
        await db.SaveChangesAsync();

        AuthHelper.AuthorizeClient(_client, user, scope.ServiceProvider.GetRequiredService<IAccessTokenService>());

        //act
        db.ChangeTracker.Clear();
        var response = await _client.GetAsync($"/api/exercises/{exercise.Id}/history");

        //assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var res = await response.Content.ReadFromJsonAsync<ExerciseHistoryResponse>();
        Assert.NotNull(res);
        Assert.NotEmpty(res.Workouts);
        Assert.Single(res.Workouts);
        Assert.Equal(workout.Id, res.Workouts.Single().Id);
        Assert.DoesNotContain(res.Workouts, x => x.Id == workoutUser2.Id);
        Assert.Equal(workout.Id, res.Workouts[0].Id);
    }

    [Fact]
    public async Task GetExerciseHistory_ForOtherUsersExercise_ShouldReturnNotFound()
    {
        //arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await UserHelper.CreateUserAsync(userManager);
        var user2 = await UserHelper.CreateUserAsync(userManager);
        var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user2);
        exercise.ExerciseMuscleGroups =
        [
            new ExerciseMuscleGroup
            {
                MuscleGroup = MuscleGroup.Back
            },
            new ExerciseMuscleGroup
            {
                MuscleGroup = MuscleGroup.Biceps
            }
        ];
        await db.SaveChangesAsync();

        AuthHelper.AuthorizeClient(_client, user, scope.ServiceProvider.GetRequiredService<IAccessTokenService>());

        //act
        db.ChangeTracker.Clear();
        var response = await _client.GetAsync($"/api/exercises/{exercise.Id}/history");

        //assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

}
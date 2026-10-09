using System.Net;
using System.Net.Http.Json;
using GymAssistant.Api.Authentication;
using GymAssistant.Api.Data;
using GymAssistant.Api.Endpoints;
using GymAssistant.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class WorkoutSessionsEndpointsTests : IClassFixture<GymAssistantWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly GymAssistantWebApplicationFactory _factory;
    public WorkoutSessionsEndpointsTests(GymAssistantWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    [Fact]
    public async Task GetWorkoutSession_ShouldReturnOk_WhenSessionExists()
    {
        //Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

        var user = await UserHelper.CreateUserAsync(userManager);
        var exercise =await ExerciseHelper.CreateUserExerciseAsync(db,user);

        var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(db,user,null,[exercise]);
        AuthHelper.AuthorizeClient(_client,user,tokenService);

        //Act
        var res = await _client.GetAsync($"/api/workout-sessions/{session.Id}");
        //Assert
        Assert.Equal(HttpStatusCode.OK,res.StatusCode);
        var sessionFromRes = await res.Content.ReadFromJsonAsync<WorkoutSessionResponseDto>();
        Assert.NotNull(sessionFromRes);
        Assert.Equal(session.Id,sessionFromRes.Id);
        Assert.Null(sessionFromRes.WorkoutTemplateId);
        var exerciseFromRes = Assert.Single(sessionFromRes.Exercises);
        Assert.Equal(exercise.Id,exerciseFromRes.ExerciseId);
        Assert.Equal(exercise.Name,exerciseFromRes.ExerciseName);
        Assert.Equal(1, exerciseFromRes.Order);
        Assert.Collection(exerciseFromRes.Sets,
            first =>
            {
                Assert.Equal(1, first.SetNumber);
                Assert.Equal(1, first.Reps);
                Assert.Equal(11m, first.Weight);
            },
            second =>
            {
                Assert.Equal(2, second.SetNumber);
                Assert.Equal(2, second.Reps);
                Assert.Equal(2m, second.Weight);
        });    
    }

    [Fact]
    public async Task GetWorkoutSessions_ShouldReturnOnlyCurrentUsersSessions()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

        var user1 = await UserHelper.CreateUserAsync(userManager);
        var user2 = await UserHelper.CreateUserAsync(userManager);

        var exercise = await ExerciseHelper.CreateSystemExerciseAsync(db);

        var session1 = await WorkoutSessionHelper.CreateWorkoutSessionAsync(
            db, user1, null, [exercise]);

        var session2 = await WorkoutSessionHelper.CreateWorkoutSessionAsync(
            db, user2, null, [exercise]);

        session1.FinishedAt = DateTime.UtcNow;
        session2.FinishedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        AuthHelper.AuthorizeClient(_client, user1, tokenService);

        // Act
        var response = await _client.GetAsync("/api/workout-sessions");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var sessions = await response.Content
            .ReadFromJsonAsync<List<WorkoutSessionListItemResponse>>();

        Assert.NotNull(sessions);

        var returnedSession = Assert.Single(sessions);

        Assert.Equal(session1.Id, returnedSession.Id);
        Assert.DoesNotContain(sessions, x => x.Id == session2.Id);
    }
    [Fact]
    public async Task GetWorkoutSessions_ShouldNotReturnUnfinishedSessions()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

        var user = await UserHelper.CreateUserAsync(userManager);
        var exercise = await ExerciseHelper.CreateSystemExerciseAsync(db);

        var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(
            db, user, null, [exercise]);

        AuthHelper.AuthorizeClient(_client, user, tokenService);

        // Act
        var response = await _client.GetAsync("/api/workout-sessions");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var sessions = await response.Content
            .ReadFromJsonAsync<List<WorkoutSessionListItemResponse>>();

        Assert.NotNull(sessions);
        Assert.Empty(sessions);
    }

    [Fact]
    public async Task GetWorkoutSession_ShouldReturnNotFound_ForOtherUsersSession()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

        var user1 = await UserHelper.CreateUserAsync(userManager);
        var user2 = await UserHelper.CreateUserAsync(userManager);

        var exercise = await ExerciseHelper.CreateSystemExerciseAsync(db);

        await WorkoutSessionHelper.CreateWorkoutSessionAsync(
            db, user1, null, [exercise]);

        var session2 = await WorkoutSessionHelper.CreateWorkoutSessionAsync(
            db, user2, null, [exercise]);

        AuthHelper.AuthorizeClient(_client, user1, tokenService);

        // Act
        var response = await _client.GetAsync(
            $"/api/workout-sessions/{session2.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetWorkoutSession_ShouldReturnNotFound_WhenSessionDoesNotExist()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

        var user = await UserHelper.CreateUserAsync(userManager);
        AuthHelper.AuthorizeClient(_client, user, tokenService);

        var nonExistingSessionId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync(
            $"/api/workout-sessions/{nonExistingSessionId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateWorkoutSession_ShouldReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

        var user = await UserHelper.CreateUserAsync(userManager);
        var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);

        AuthHelper.AuthorizeClient(_client, user, tokenService);

        var request = new CreateWorkoutSessionRequest(
            null,
            new List<CreateWorkoutSessionExerciseRequest>
            {
                new CreateWorkoutSessionExerciseRequest(
                    exercise.Id,
                    new List<CreateWorkoutSessionSetRequest>
                    {
                        new CreateWorkoutSessionSetRequest(
                            1,
                            10m,
                            8
                        )
                    },
                    Guid.NewGuid()
                )
            }
        );

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/workout-sessions",
            request
        );

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseDto = await response.Content
            .ReadFromJsonAsync<CreateWorkoutSessionResponse>();

        Assert.NotNull(responseDto);
        Assert.NotEqual(Guid.Empty, responseDto.Id);

        db.ChangeTracker.Clear();

        var sessionFromDb = await db.WorkoutSessions
            .Include(x => x.Exercises)
                .ThenInclude(x => x.Sets)
            .SingleAsync(x => x.Id == responseDto.Id);

        Assert.Equal(user.Id, sessionFromDb.UserId);
        Assert.Null(sessionFromDb.WorkoutTemplateId);
        Assert.Null(sessionFromDb.FinishedAt);

        var exerciseFromDb = Assert.Single(sessionFromDb.Exercises);

        Assert.Equal(exercise.Id, exerciseFromDb.ExerciseId);
        Assert.Equal(exercise.Name, exerciseFromDb.ExerciseNameSnapshot);
        Assert.Equal(1, exerciseFromDb.Order);

        var setFromDb = Assert.Single(exerciseFromDb.Sets);

        Assert.Equal(1, setFromDb.SetNumber);
        Assert.Equal(10m, setFromDb.Weight);
        Assert.Equal(8, setFromDb.Reps);
    }


    [Fact]
    public async Task CreateWorkoutSession_WithoutExercises_ShouldReturnBadRequest()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

        var user = await UserHelper.CreateUserAsync(userManager);
        AuthHelper.AuthorizeClient(_client, user, tokenService);

        var request = new CreateWorkoutSessionRequest(
            null,
            new List<CreateWorkoutSessionExerciseRequest>()
        );

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/workout-sessions",
            request
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorMessage = await response.Content.ReadFromJsonAsync<string>();

        Assert.Equal(
            "Trening musi zawierać przynajmniej jedno ćwiczenie.",
            errorMessage
        );
    }

    [Fact]
    public async Task CreateWorkoutSession_WithOtherUsersTemplate_ShouldReturnBadRequest()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

        var user1 = await UserHelper.CreateUserAsync(userManager);
        var user2 = await UserHelper.CreateUserAsync(userManager);

        var exercise = await ExerciseHelper.CreateSystemExerciseAsync(db);

        var template = await WorkoutTemplateHelper.CreateWorkoutTemplateAsync(
            db, user2, [exercise]);

        AuthHelper.AuthorizeClient(_client, user1, tokenService);

        var request = new CreateWorkoutSessionRequest(
            template.Id,
            new List<CreateWorkoutSessionExerciseRequest>
            {
                new CreateWorkoutSessionExerciseRequest(
                    exercise.Id,
                    new List<CreateWorkoutSessionSetRequest>
                    {
                        new CreateWorkoutSessionSetRequest(1, 10m, 8)
                    },
                    Guid.NewGuid()
                )
            }
        );

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/workout-sessions",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorMessage = await response.Content.ReadFromJsonAsync<string>();

        Assert.Equal(
            "Wybrany plan treningowy nie istnieje.",
            errorMessage);
        db.ChangeTracker.Clear();

        var sessionExists = await db.WorkoutSessions
            .AnyAsync(x => x.UserId == user1.Id);

        Assert.False(sessionExists);
    }


    [Fact]
    public async Task CreateWorkoutSession_WithUserAndSystemExercises_ShouldReturnCreated()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

        var user1 = await UserHelper.CreateUserAsync(userManager);
        var user2 = await UserHelper.CreateUserAsync(userManager);

        var systemExercise = await ExerciseHelper.CreateSystemExerciseAsync(db);
        var user1Exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user1);
        var user2Exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user2);

        AuthHelper.AuthorizeClient(_client, user1, tokenService);

        var request = new CreateWorkoutSessionRequest(
            null,
            new List<CreateWorkoutSessionExerciseRequest>
            {
                new CreateWorkoutSessionExerciseRequest(
                    systemExercise.Id,
                    new List<CreateWorkoutSessionSetRequest>
                    {
                        new CreateWorkoutSessionSetRequest(1, 20m, 10)
                    },
                    Guid.NewGuid()
                ),
                new CreateWorkoutSessionExerciseRequest(
                    user1Exercise.Id,
                    new List<CreateWorkoutSessionSetRequest>
                    {
                        new CreateWorkoutSessionSetRequest(1, 30m, 8)
                    },
                    Guid.NewGuid()
                )
            }
        );

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/workout-sessions",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseDto = await response.Content
            .ReadFromJsonAsync<CreateWorkoutSessionResponse>();

        Assert.NotNull(responseDto);

        db.ChangeTracker.Clear();

        var sessionFromDb = await db.WorkoutSessions
            .Include(x => x.Exercises)
                .ThenInclude(x => x.Sets)
            .SingleAsync(x => x.Id == responseDto.Id);

        Assert.Equal(user1.Id, sessionFromDb.UserId);

        Assert.Equal(2, sessionFromDb.Exercises.Count);

        var exercisesFromDb = sessionFromDb.Exercises
            .OrderBy(x => x.Order)
            .ToList();

        Assert.Equal(systemExercise.Id, exercisesFromDb[0].ExerciseId);
        Assert.Equal(user1Exercise.Id, exercisesFromDb[1].ExerciseId);

        Assert.Equal(1, exercisesFromDb[0].Order);
        Assert.Equal(2, exercisesFromDb[1].Order);

        Assert.DoesNotContain(
            sessionFromDb.Exercises,
            x => x.ExerciseId == user2Exercise.Id);
    }

    [Fact]
    public async Task CreateWorkoutSession_WithOtherUsersExercise_ShouldReturnBadRequest()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

        var user1 = await UserHelper.CreateUserAsync(userManager);
        var user2 = await UserHelper.CreateUserAsync(userManager);

        var systemExercise = await ExerciseHelper.CreateSystemExerciseAsync(db);
        var user1Exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user1);
        var user2Exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user2);

        AuthHelper.AuthorizeClient(_client, user1, tokenService);

        var request = new CreateWorkoutSessionRequest(
            null,
            new List<CreateWorkoutSessionExerciseRequest>
            {
                new CreateWorkoutSessionExerciseRequest(
                    systemExercise.Id,
                    new List<CreateWorkoutSessionSetRequest>
                    {
                        new CreateWorkoutSessionSetRequest(1, 20m, 10)
                    },
                    Guid.NewGuid()
                ),
                new CreateWorkoutSessionExerciseRequest(
                    user1Exercise.Id,
                    new List<CreateWorkoutSessionSetRequest>
                    {
                        new CreateWorkoutSessionSetRequest(1, 30m, 8)
                    },
                    Guid.NewGuid()
                ),
                new CreateWorkoutSessionExerciseRequest(
                    user2Exercise.Id,
                    new List<CreateWorkoutSessionSetRequest>
                    {
                        new CreateWorkoutSessionSetRequest(1, 40m, 6)
                    },
                    Guid.NewGuid()
                )
            }
        );

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/workout-sessions",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorMessage = await response.Content.ReadFromJsonAsync<string>();

        Assert.Equal(
            "Jedno lub więcej wybranych ćwiczeń nie istnieje.",
            errorMessage);

        db.ChangeTracker.Clear();

        var sessionExists = await db.WorkoutSessions
            .AnyAsync(x => x.UserId == user1.Id);

        Assert.False(sessionExists);
    }
    [Fact]
    public async Task CreateWorkoutSession_WithDuplicateExercises_ShouldReturnBadRequest()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

        var user = await UserHelper.CreateUserAsync(userManager);
        var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);

        AuthHelper.AuthorizeClient(_client, user, tokenService);

        var request = new CreateWorkoutSessionRequest(
            null,
            new List<CreateWorkoutSessionExerciseRequest>
            {
                new CreateWorkoutSessionExerciseRequest(
                    exercise.Id,
                    new List<CreateWorkoutSessionSetRequest>
                    {
                        new CreateWorkoutSessionSetRequest(1, 20m, 10)
                    },
                    Guid.NewGuid()
                ),
                new CreateWorkoutSessionExerciseRequest(
                    exercise.Id,
                    new List<CreateWorkoutSessionSetRequest>
                    {
                        new CreateWorkoutSessionSetRequest(1, 30m, 8)
                    },
                    Guid.NewGuid()
                )
            }
        );

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/workout-sessions",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        db.ChangeTracker.Clear();

        var sessionExists = await db.WorkoutSessions
            .AnyAsync(x => x.UserId == user.Id);

        Assert.False(sessionExists);
    }


    [Theory]
    [InlineData(-1, 20)]
    [InlineData(10, -20)]
    public async Task CreateWorkoutSession_WithNegativeRepsOrWeight_ShouldReturnBadRequest(
        int reps,
        int weight)
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

        var user = await UserHelper.CreateUserAsync(userManager);
        var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);

        AuthHelper.AuthorizeClient(_client, user, tokenService);

        var request = new CreateWorkoutSessionRequest(
            null,
            new List<CreateWorkoutSessionExerciseRequest>
            {
                new CreateWorkoutSessionExerciseRequest(
                    exercise.Id,
                    new List<CreateWorkoutSessionSetRequest>
                    {
                        new CreateWorkoutSessionSetRequest(1, weight, reps)
                    },
                    Guid.NewGuid()
                )
            }
        );

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/workout-sessions",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        db.ChangeTracker.Clear();

        var sessionExists = await db.WorkoutSessions
            .AnyAsync(x => x.UserId == user.Id);

        Assert.False(sessionExists);
    }

}
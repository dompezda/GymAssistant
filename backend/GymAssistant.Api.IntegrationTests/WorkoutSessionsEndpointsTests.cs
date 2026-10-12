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

[Fact]
public async Task AddExerciseToWorkoutSession_ShouldReturnOk_WhenRequestIsValid()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user = await UserHelper.CreateUserAsync(userManager);
    var exercise1 = await ExerciseHelper.CreateUserExerciseAsync(db, user);
    var exercise2 = await ExerciseHelper.CreateUserExerciseAsync(db, user);

    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(
        db, user, null, [exercise1]);

    var originalExerciseId = Assert.Single(session.Exercises).Id;

    AuthHelper.AuthorizeClient(_client, user, tokenService);

    var request = new AddExerciseToWorkoutSessionRequest(exercise2.Id);

    // Act
    var response = await _client.PostAsJsonAsync(
        $"/api/workout-sessions/{session.Id}/exercises", request);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    var dto = await response.Content
        .ReadFromJsonAsync<WorkoutSessionExerciseResponseDto>();

    Assert.NotNull(dto);
    Assert.NotEqual(Guid.Empty, dto.Id);
    Assert.Equal(exercise2.Id, dto.ExerciseId);
    Assert.Equal(exercise2.Name, dto.ExerciseName);
    Assert.Equal(2, dto.Order);

    var responseSet = Assert.Single(dto.Sets);
    Assert.NotEqual(Guid.Empty, responseSet.Id);
    Assert.Equal(1, responseSet.SetNumber);
    Assert.Null(responseSet.Reps);
    Assert.Null(responseSet.Weight);

    db.ChangeTracker.Clear();

    var sessionFromDb = await db.WorkoutSessions
        .Include(x => x.Exercises)
            .ThenInclude(x => x.Sets)
        .SingleAsync(x => x.Id == session.Id);

    var exercises = sessionFromDb.Exercises
        .OrderBy(x => x.Order)
        .ToList();

    Assert.Collection(
        exercises,
        first =>
        {
            Assert.Equal(originalExerciseId, first.Id);
            Assert.Equal(exercise1.Id, first.ExerciseId);
            Assert.Equal(1, first.Order);
        },
        second =>
        {
            Assert.Equal(dto.Id, second.Id);
            Assert.Equal(exercise2.Id, second.ExerciseId);
            Assert.Equal(exercise2.Name, second.ExerciseNameSnapshot);
            Assert.Equal(2, second.Order);

            var set = Assert.Single(second.Sets);
            Assert.Equal(responseSet.Id, set.Id);
            Assert.Equal(1, set.SetNumber);
            Assert.Null(set.Reps);
            Assert.Null(set.Weight);
        }
    );
}

[Theory]
[InlineData(true)]
[InlineData(false)]
public async Task AddExerciseToWorkoutSession_ShouldReturnBadRequest_WhenIdIsEmpty(
    bool emptySessionId)
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user = await UserHelper.CreateUserAsync(userManager);
    var exercise1 = await ExerciseHelper.CreateUserExerciseAsync(db, user);
    var exercise2 = await ExerciseHelper.CreateUserExerciseAsync(db, user);

    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(
        db, user, null, [exercise1]);

    AuthHelper.AuthorizeClient(_client, user, tokenService);

    var sessionId = emptySessionId ? Guid.Empty : session.Id;
    var exerciseId = emptySessionId ? exercise2.Id : Guid.Empty;

    var request = new AddExerciseToWorkoutSessionRequest(exerciseId);

    // Act
    var response = await _client.PostAsJsonAsync(
        $"/api/workout-sessions/{sessionId}/exercises", request);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    db.ChangeTracker.Clear();

    var sessionFromDb = await db.WorkoutSessions
        .Include(x => x.Exercises)
        .SingleAsync(x => x.Id == session.Id);

    var remainingExercise = Assert.Single(sessionFromDb.Exercises);

    Assert.Equal(exercise1.Id, remainingExercise.ExerciseId);
}

[Fact]
public async Task AddExerciseToWorkoutSession_ShouldReturnNotFound_WhenSessionDoesNotExist()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user = await UserHelper.CreateUserAsync(userManager);
    var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);

    AuthHelper.AuthorizeClient(_client, user, tokenService);

    var nonExistingSessionId = Guid.NewGuid();
    var request = new AddExerciseToWorkoutSessionRequest(exercise.Id);

    // Act
    var response = await _client.PostAsJsonAsync(
        $"/api/workout-sessions/{nonExistingSessionId}/exercises", request);

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

    var sessionExerciseExists = await db.WorkoutSessionExercises
        .AnyAsync(x => x.ExerciseId == exercise.Id);

    Assert.False(sessionExerciseExists);
}

[Fact]
public async Task AddExerciseToWorkoutSession_ShouldReturnNotFound_ForOtherUsersSession()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user1 = await UserHelper.CreateUserAsync(userManager);
    var user2 = await UserHelper.CreateUserAsync(userManager);

    var exercise1 = await ExerciseHelper.CreateUserExerciseAsync(db, user2);
    var exerciseToAdd = await ExerciseHelper.CreateSystemExerciseAsync(db);

    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(
        db, user2, null, [exercise1]);

    var originalSessionExerciseId = Assert.Single(session.Exercises).Id;

    AuthHelper.AuthorizeClient(_client, user1, tokenService);

    var request = new AddExerciseToWorkoutSessionRequest(exerciseToAdd.Id);

    // Act
    var response = await _client.PostAsJsonAsync(
        $"/api/workout-sessions/{session.Id}/exercises", request);

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

    db.ChangeTracker.Clear();

    var sessionFromDb = await db.WorkoutSessions
        .Include(x => x.Exercises)
        .SingleAsync(x => x.Id == session.Id);

    Assert.Equal(user2.Id, sessionFromDb.UserId);

    var remainingExercise = Assert.Single(sessionFromDb.Exercises);
    Assert.Equal(originalSessionExerciseId, remainingExercise.Id);
    Assert.Equal(exercise1.Id, remainingExercise.ExerciseId);
}

[Fact]
public async Task AddExerciseToWorkoutSession_ShouldReturnNotFound_ForOtherUsersExercise()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user1 = await UserHelper.CreateUserAsync(userManager);
    var user2 = await UserHelper.CreateUserAsync(userManager);

    var exercise1 = await ExerciseHelper.CreateUserExerciseAsync(db, user1);
    var otherUsersExercise = await ExerciseHelper.CreateUserExerciseAsync(db, user2);

    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(
        db, user1, null, [exercise1]);

    AuthHelper.AuthorizeClient(_client, user1, tokenService);

    var request = new AddExerciseToWorkoutSessionRequest(otherUsersExercise.Id);

    // Act
    var response = await _client.PostAsJsonAsync(
        $"/api/workout-sessions/{session.Id}/exercises", request);

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

    db.ChangeTracker.Clear();

    var sessionFromDb = await db.WorkoutSessions
        .Include(x => x.Exercises)
        .SingleAsync(x => x.Id == session.Id);

    var remainingExercise = Assert.Single(sessionFromDb.Exercises);
    Assert.Equal(exercise1.Id, remainingExercise.ExerciseId);
}

[Fact]
public async Task AddExerciseToWorkoutSession_ShouldReturnConflict_WhenExerciseAlreadyExists()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user = await UserHelper.CreateUserAsync(userManager);
    var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);

    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(
        db, user, null, [exercise]);

    var originalExercise = Assert.Single(session.Exercises);
    var originalExerciseId = originalExercise.Id;

    AuthHelper.AuthorizeClient(_client, user, tokenService);

    var request = new AddExerciseToWorkoutSessionRequest(exercise.Id);

    // Act
    var response = await _client.PostAsJsonAsync(
        $"/api/workout-sessions/{session.Id}/exercises", request);

    // Assert
    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

    db.ChangeTracker.Clear();

    var sessionFromDb = await db.WorkoutSessions
        .Include(x => x.Exercises)
        .SingleAsync(x => x.Id == session.Id);

    var remainingExercise = Assert.Single(sessionFromDb.Exercises);

    Assert.Equal(originalExerciseId, remainingExercise.Id);
    Assert.Equal(exercise.Id, remainingExercise.ExerciseId);
    Assert.Equal(1, remainingExercise.Order);
}

[Fact]
public async Task AddExerciseToWorkoutSession_ShouldReturnNotFound_WhenExerciseDoesNotExist()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user = await UserHelper.CreateUserAsync(userManager);
    var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);

    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(
        db, user, null, [exercise]);

    var originalExerciseId = Assert.Single(session.Exercises).Id;

    AuthHelper.AuthorizeClient(_client, user, tokenService);

    var request = new AddExerciseToWorkoutSessionRequest(Guid.NewGuid());

    // Act
    var response = await _client.PostAsJsonAsync(
        $"/api/workout-sessions/{session.Id}/exercises", request);

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

    db.ChangeTracker.Clear();

    var sessionFromDb = await db.WorkoutSessions
        .Include(x => x.Exercises)
        .SingleAsync(x => x.Id == session.Id);

    var remainingExercise = Assert.Single(sessionFromDb.Exercises);

    Assert.Equal(originalExerciseId, remainingExercise.Id);
    Assert.Equal(exercise.Id, remainingExercise.ExerciseId);
}

[Fact]
public async Task AddExerciseToWorkoutSession_ShouldReturnConflict_WhenSessionIsFinished()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user = await UserHelper.CreateUserAsync(userManager);

    var exercise1 = await ExerciseHelper.CreateUserExerciseAsync(db, user);
    var exercise2 = await ExerciseHelper.CreateUserExerciseAsync(db, user);

    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(
        db, user, null, [exercise1]);

    var originalExerciseId = Assert.Single(session.Exercises).Id;

    AuthHelper.AuthorizeClient(_client, user, tokenService);

    var finishResponse = await _client.PatchAsync(
        $"/api/workout-sessions/{session.Id}/finish", null);

    Assert.Equal(HttpStatusCode.OK, finishResponse.StatusCode);

    var request = new AddExerciseToWorkoutSessionRequest(exercise2.Id);

    // Act
    var response = await _client.PostAsJsonAsync(
        $"/api/workout-sessions/{session.Id}/exercises", request);

    // Assert
    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

    db.ChangeTracker.Clear();

    var sessionFromDb = await db.WorkoutSessions
        .Include(x => x.Exercises)
        .SingleAsync(x => x.Id == session.Id);

    Assert.NotNull(sessionFromDb.FinishedAt);

    var remainingExercise = Assert.Single(sessionFromDb.Exercises);

    Assert.Equal(originalExerciseId, remainingExercise.Id);
    Assert.Equal(exercise1.Id, remainingExercise.ExerciseId);
}

[Theory]
    [InlineData(111,111)]
    [InlineData(222,222)]
    [InlineData(0,0.0)]
    public async Task PatchWorkoutSessionExercise_ShouldUpdateExistingSets_WhenRequestIsValid(int reps, int weightInt)
    {
        //Arrange
        var weight = new decimal(weightInt);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

        var user = await UserHelper.CreateUserAsync(userManager);
        var exercise = await ExerciseHelper.CreateUserExerciseAsync(db,user);
        
        var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(db,user,null,[exercise]);
        var sessionExercise = session.Exercises[0];
        AuthHelper.AuthorizeClient(_client,user,tokenService);
        //Act
        var body = new UpdateWorkoutSessionExerciseRequest
        (
            [
                new UpdateWorkoutSessionSetRequest(sessionExercise.Sets[0].Id,1,reps,weight),
                new UpdateWorkoutSessionSetRequest(sessionExercise.Sets[1].Id,2,reps+1,weight+1)
            ]
        );
        var res = await _client.PatchAsJsonAsync($"/api/workout-sessions/{session.Id}/exercises/{sessionExercise.Id}", body);//Assert
        var set1Id = sessionExercise.Sets[0].Id;
        var set2Id = sessionExercise.Sets[1].Id;
        //Assert
        Assert.Equal(HttpStatusCode.OK,res.StatusCode);
        db.ChangeTracker.Clear();
        var workoutSessionExerciseFromDb = await db.WorkoutSessionExercises
            .Include(x=>x.Sets)
            .FirstOrDefaultAsync(x=>x.Id==sessionExercise.Id);
        Assert.NotNull(workoutSessionExerciseFromDb);
        var sets = workoutSessionExerciseFromDb.Sets
            .OrderBy(x => x.SetNumber)
            .ToList();
        Assert.Equal(2, sets.Count);
        var set1 = sets[0];
        var set2 = sets[1];
        Assert.Equal(weight,set1.Weight);
        Assert.Equal(reps,set1.Reps);
        Assert.Equal(weight+1,set2.Weight);
        Assert.Equal(reps+1,set2.Reps);
        Assert.Equal(set1Id,set1.Id);
        Assert.Equal(set2Id,set2.Id);
    }

[Fact]
public async Task PatchWorkoutSessionExercise_ShouldAddNewSet_WhenSetDoesNotExist()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user = await UserHelper.CreateUserAsync(userManager);
    var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);
    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(
        db, user, null, [exercise]);

    var sessionExercise = session.Exercises[0];

    var set1Id = sessionExercise.Sets[0].Id;
    var set2Id = sessionExercise.Sets[1].Id;
    var set3Id = Guid.NewGuid();

    AuthHelper.AuthorizeClient(_client, user, tokenService);

    var body = new UpdateWorkoutSessionExerciseRequest
    (
        [
            new UpdateWorkoutSessionSetRequest(set1Id, 1, 1, 11m),
            new UpdateWorkoutSessionSetRequest(set2Id, 2, 2, 2m),
            new UpdateWorkoutSessionSetRequest(set3Id, 3, 10, 50m)
        ]
    );

    // Act
    var res = await _client.PatchAsJsonAsync(
        $"/api/workout-sessions/{session.Id}/exercises/{sessionExercise.Id}",
        body);

    // Assert
    Assert.Equal(HttpStatusCode.OK, res.StatusCode);

    db.ChangeTracker.Clear();

    var sessionFromDb = await db.WorkoutSessions
        .Include(x => x.Exercises)
            .ThenInclude(x => x.Sets)
        .FirstOrDefaultAsync(x => x.Id == session.Id);

    Assert.NotNull(sessionFromDb);

    var exerciseFromDb = Assert.Single(sessionFromDb.Exercises);

    var sets = exerciseFromDb.Sets
        .OrderBy(x => x.SetNumber)
        .ToList();

    Assert.Equal(3, sets.Count);

    Assert.Equal(set1Id, sets[0].Id);
    Assert.Equal(1, sets[0].SetNumber);
    Assert.Equal(1, sets[0].Reps);
    Assert.Equal(11m, sets[0].Weight);

    Assert.Equal(set2Id, sets[1].Id);
    Assert.Equal(2, sets[1].SetNumber);
    Assert.Equal(2, sets[1].Reps);
    Assert.Equal(2m, sets[1].Weight);

    Assert.Equal(set3Id, sets[2].Id);
    Assert.Equal(3, sets[2].SetNumber);
    Assert.Equal(10, sets[2].Reps);
    Assert.Equal(50m, sets[2].Weight);
}

[Fact]
public async Task PatchWorkoutSessionExercise_ShouldDeleteSet_WhenSetIsNotInRequest()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user = await UserHelper.CreateUserAsync(userManager);
    var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);
    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(
        db, user, null, [exercise]);

    var sessionExercise = session.Exercises[0];

    var set1 = sessionExercise.Sets[0];
    var set1Id = set1.Id;
    var set2Id = sessionExercise.Sets[1].Id;

    AuthHelper.AuthorizeClient(_client, user, tokenService);

    var body = new UpdateWorkoutSessionExerciseRequest
    (
        [
            new UpdateWorkoutSessionSetRequest(
                set1Id,
                set1.SetNumber,
                set1.Reps,
                set1.Weight)
        ]
    );

    // Act
    var res = await _client.PatchAsJsonAsync(
        $"/api/workout-sessions/{session.Id}/exercises/{sessionExercise.Id}",
        body);

    // Assert
    Assert.Equal(HttpStatusCode.OK, res.StatusCode);

    db.ChangeTracker.Clear();

    var sessionFromDb = await db.WorkoutSessions
        .Include(x => x.Exercises)
            .ThenInclude(x => x.Sets)
        .FirstOrDefaultAsync(x => x.Id == session.Id);

    Assert.NotNull(sessionFromDb);

    var exerciseFromDb = Assert.Single(sessionFromDb.Exercises);
    var remainingSet = Assert.Single(exerciseFromDb.Sets);

    Assert.Equal(set1Id, remainingSet.Id);
    Assert.Equal(1, remainingSet.SetNumber);
    Assert.Equal(1, remainingSet.Reps);
    Assert.Equal(11m, remainingSet.Weight);

    var deletedSetExists = await db.WorkoutSessionSets
        .AnyAsync(x => x.Id == set2Id);

    Assert.False(deletedSetExists);
}

[Fact]
public async Task PatchWorkoutSessionExercise_WithoutSets_ShouldReturnBadRequest()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user = await UserHelper.CreateUserAsync(userManager);
    var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);
    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(db, user, null, [exercise]);
    var sessionExercise = session.Exercises[0];

    var originalSets = sessionExercise.Sets
        .OrderBy(x => x.SetNumber)
        .Select(x => new { x.Id, x.SetNumber, x.Reps, x.Weight })
        .ToList();

    AuthHelper.AuthorizeClient(_client, user, tokenService);

    var body = new UpdateWorkoutSessionExerciseRequest([]);

    // Act
    var res = await _client.PatchAsJsonAsync(
        $"/api/workout-sessions/{session.Id}/exercises/{sessionExercise.Id}", body);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

    db.ChangeTracker.Clear();

    var exerciseFromDb = await db.WorkoutSessionExercises
        .Include(x => x.Sets)
        .FirstOrDefaultAsync(x => x.Id == sessionExercise.Id);

    Assert.NotNull(exerciseFromDb);

    var sets = exerciseFromDb.Sets.OrderBy(x => x.SetNumber).ToList();

    Assert.Equal(originalSets.Count, sets.Count);

    for (int i = 0; i < originalSets.Count; i++)
    {
        Assert.Equal(originalSets[i].Id, sets[i].Id);
        Assert.Equal(originalSets[i].SetNumber, sets[i].SetNumber);
        Assert.Equal(originalSets[i].Reps, sets[i].Reps);
        Assert.Equal(originalSets[i].Weight, sets[i].Weight);
    }
}

[Theory]
[InlineData(-1, 10)]
[InlineData(10, -1)]
public async Task PatchWorkoutSessionExercise_WithNegativeRepsOrWeight_ShouldReturnBadRequest(
    int reps, int weightInt)
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user = await UserHelper.CreateUserAsync(userManager);
    var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);
    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(db, user, null, [exercise]);
    var sessionExercise = session.Exercises[0];

    var set1Id = sessionExercise.Sets[0].Id;
    var set2Id = sessionExercise.Sets[1].Id;

    AuthHelper.AuthorizeClient(_client, user, tokenService);

    var body = new UpdateWorkoutSessionExerciseRequest
    (
        [
            new UpdateWorkoutSessionSetRequest(set1Id, 1, reps, weightInt),
            new UpdateWorkoutSessionSetRequest(set2Id, 2, 15, 50m)
        ]
    );

    // Act
    var res = await _client.PatchAsJsonAsync(
        $"/api/workout-sessions/{session.Id}/exercises/{sessionExercise.Id}", body);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

    db.ChangeTracker.Clear();

    var exerciseFromDb = await db.WorkoutSessionExercises
        .Include(x => x.Sets)
        .FirstOrDefaultAsync(x => x.Id == sessionExercise.Id);

    Assert.NotNull(exerciseFromDb);

    var sets = exerciseFromDb.Sets.OrderBy(x => x.SetNumber).ToList();

    Assert.Equal(2, sets.Count);

    Assert.Equal(set1Id, sets[0].Id);
    Assert.Equal(1, sets[0].Reps);
    Assert.Equal(11m, sets[0].Weight);
    Assert.Equal(1, sets[0].SetNumber);

    Assert.Equal(set2Id, sets[1].Id);
    Assert.Equal(2, sets[1].Reps);
    Assert.Equal(2m, sets[1].Weight);
    Assert.Equal(2, sets[1].SetNumber);
}

[Fact]
public async Task PatchWorkoutSessionExercise_ShouldReturnNotFound_ForOtherUsersSession()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user1 = await UserHelper.CreateUserAsync(userManager);
    var user2 = await UserHelper.CreateUserAsync(userManager);

    var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user2);
    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(db, user2, null, [exercise]);
    var sessionExercise = session.Exercises[0];

    var set1Id = sessionExercise.Sets[0].Id;
    var set2Id = sessionExercise.Sets[1].Id;

    // Logujemy user1, ale próbujemy edytować sesję user2
    AuthHelper.AuthorizeClient(_client, user1, tokenService);

    var body = new UpdateWorkoutSessionExerciseRequest
    (
        [
            new UpdateWorkoutSessionSetRequest(set1Id, 1, 100, 200m),
            new UpdateWorkoutSessionSetRequest(set2Id, 2, 100, 200m)
        ]
    );

    // Act
    var res = await _client.PatchAsJsonAsync(
        $"/api/workout-sessions/{session.Id}/exercises/{sessionExercise.Id}", body);

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);

    db.ChangeTracker.Clear();

    var sessionFromDb = await db.WorkoutSessions
        .Include(x => x.Exercises)
            .ThenInclude(x => x.Sets)
        .FirstOrDefaultAsync(x => x.Id == session.Id);

    Assert.NotNull(sessionFromDb);
    Assert.Equal(user2.Id, sessionFromDb.UserId);

    var exerciseFromDb = Assert.Single(sessionFromDb.Exercises);
    var sets = exerciseFromDb.Sets.OrderBy(x => x.SetNumber).ToList();

    Assert.Equal(2, sets.Count);

    Assert.Equal(set1Id, sets[0].Id);
    Assert.Equal(1, sets[0].Reps);
    Assert.Equal(11m, sets[0].Weight);

    Assert.Equal(set2Id, sets[1].Id);
    Assert.Equal(2, sets[1].Reps);
    Assert.Equal(2m, sets[1].Weight);
}

[Fact]
public async Task PatchWorkoutSessionExercise_ShouldReturnNotFound_WhenExerciseDoesNotExistInSession()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user = await UserHelper.CreateUserAsync(userManager);
    var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);
    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(db, user, null, [exercise]);

    AuthHelper.AuthorizeClient(_client, user, tokenService);

    var randomSessionExerciseId = Guid.NewGuid();

    var body = new UpdateWorkoutSessionExerciseRequest
    (
        [
            new UpdateWorkoutSessionSetRequest(Guid.NewGuid(), 1, 10, 50m)
        ]
    );

    // Act
    var res = await _client.PatchAsJsonAsync(
        $"/api/workout-sessions/{session.Id}/exercises/{randomSessionExerciseId}", body);

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
}

[Theory]
[InlineData(10, 20, 30)]
[InlineData(3, 1, 2)]
public async Task PatchWorkoutSessionExercise_ShouldAssignSetNumbersBasedOnRequestOrder(
    int firstSetNumber, int secondSetNumber, int thirdSetNumber)
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user = await UserHelper.CreateUserAsync(userManager);
    var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);
    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(db, user, null, [exercise]);
    var sessionExercise = session.Exercises[0];

    var set1Id = sessionExercise.Sets[0].Id;
    var set2Id = sessionExercise.Sets[1].Id;
    var set3Id = Guid.NewGuid();

    AuthHelper.AuthorizeClient(_client, user, tokenService);

    var body = new UpdateWorkoutSessionExerciseRequest
    (
        [
            new UpdateWorkoutSessionSetRequest(set1Id, firstSetNumber, 10, 50m),
            new UpdateWorkoutSessionSetRequest(set2Id, secondSetNumber, 20, 60m),
            new UpdateWorkoutSessionSetRequest(set3Id, thirdSetNumber, 30, 70m)
        ]
    );

    // Act
    var res = await _client.PatchAsJsonAsync(
        $"/api/workout-sessions/{session.Id}/exercises/{sessionExercise.Id}", body);

    // Assert
    Assert.Equal(HttpStatusCode.OK, res.StatusCode);

    db.ChangeTracker.Clear();

    var exerciseFromDb = await db.WorkoutSessionExercises
        .Include(x => x.Sets)
        .FirstOrDefaultAsync(x => x.Id == sessionExercise.Id);

    Assert.NotNull(exerciseFromDb);

    var sets = exerciseFromDb.Sets.OrderBy(x => x.SetNumber).ToList();

    Assert.Equal(3, sets.Count);

    Assert.Equal(set1Id, sets[0].Id);
    Assert.Equal(1, sets[0].SetNumber);
    Assert.Equal(10, sets[0].Reps);
    Assert.Equal(50m, sets[0].Weight);

    Assert.Equal(set2Id, sets[1].Id);
    Assert.Equal(2, sets[1].SetNumber);
    Assert.Equal(20, sets[1].Reps);
    Assert.Equal(60m, sets[1].Weight);

    Assert.Equal(set3Id, sets[2].Id);
    Assert.Equal(3, sets[2].SetNumber);
    Assert.Equal(30, sets[2].Reps);
    Assert.Equal(70m, sets[2].Weight);
}

[Fact]
public async Task PatchWorkoutSessionExercise_WithEmptySetId_ShouldReturnBadRequest()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user = await UserHelper.CreateUserAsync(userManager);
    var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);
    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(db, user, null, [exercise]);
    var sessionExercise = session.Exercises[0];

    var set1Id = sessionExercise.Sets[0].Id;
    var set2Id = sessionExercise.Sets[1].Id;

    AuthHelper.AuthorizeClient(_client, user, tokenService);

    var body = new UpdateWorkoutSessionExerciseRequest
    (
        [
            new UpdateWorkoutSessionSetRequest(set1Id, 1, 10, 50m),
            new UpdateWorkoutSessionSetRequest(set2Id, 2, 20, 60m),
            new UpdateWorkoutSessionSetRequest(Guid.Empty, 3, 30, 70m)
        ]
    );

    // Act
    var res = await _client.PatchAsJsonAsync(
        $"/api/workout-sessions/{session.Id}/exercises/{sessionExercise.Id}", body);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

    db.ChangeTracker.Clear();

    var exerciseFromDb = await db.WorkoutSessionExercises
        .Include(x => x.Sets)
        .FirstOrDefaultAsync(x => x.Id == sessionExercise.Id);

    Assert.NotNull(exerciseFromDb);

    var sets = exerciseFromDb.Sets.OrderBy(x => x.SetNumber).ToList();

    Assert.Equal(2, sets.Count);

    Assert.Equal(set1Id, sets[0].Id);
    Assert.Equal(1, sets[0].Reps);
    Assert.Equal(11m, sets[0].Weight);

    Assert.Equal(set2Id, sets[1].Id);
    Assert.Equal(2, sets[1].Reps);
    Assert.Equal(2m, sets[1].Weight);
}

[Fact]
public async Task PatchWorkoutSessionExercise_WithDuplicateSetIds_ShouldReturnBadRequest()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user = await UserHelper.CreateUserAsync(userManager);
    var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);
    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(db, user, null, [exercise]);
    var sessionExercise = session.Exercises[0];

    var set1Id = sessionExercise.Sets[0].Id;
    var set2Id = sessionExercise.Sets[1].Id;

    AuthHelper.AuthorizeClient(_client, user, tokenService);

    var body = new UpdateWorkoutSessionExerciseRequest
    (
        [
            new UpdateWorkoutSessionSetRequest(set1Id, 1, 10, 50m),
            new UpdateWorkoutSessionSetRequest(set2Id, 2, 20, 60m),
            new UpdateWorkoutSessionSetRequest(set1Id, 3, 30, 70m)
        ]
    );

    // Act
    var res = await _client.PatchAsJsonAsync(
        $"/api/workout-sessions/{session.Id}/exercises/{sessionExercise.Id}", body);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

    db.ChangeTracker.Clear();

    var exerciseFromDb = await db.WorkoutSessionExercises
        .Include(x => x.Sets)
        .FirstOrDefaultAsync(x => x.Id == sessionExercise.Id);

    Assert.NotNull(exerciseFromDb);

    var sets = exerciseFromDb.Sets.OrderBy(x => x.SetNumber).ToList();

    Assert.Equal(2, sets.Count);

    Assert.Equal(set1Id, sets[0].Id);
    Assert.Equal(1, sets[0].Reps);
    Assert.Equal(11m, sets[0].Weight);

    Assert.Equal(set2Id, sets[1].Id);
    Assert.Equal(2, sets[1].Reps);
    Assert.Equal(2m, sets[1].Weight);
}

[Fact]
public async Task PatchWorkoutSessionExercise_ShouldReturnNotFound_WhenSessionDoesNotExist()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user = await UserHelper.CreateUserAsync(userManager);
    AuthHelper.AuthorizeClient(_client, user, tokenService);

    var sessionId = Guid.NewGuid();
    var sessionExerciseId = Guid.NewGuid();

    var body = new UpdateWorkoutSessionExerciseRequest
    (
        [
            new UpdateWorkoutSessionSetRequest(Guid.NewGuid(), 1, 10, 50m)
        ]
    );

    // Act
    var res = await _client.PatchAsJsonAsync(
        $"/api/workout-sessions/{sessionId}/exercises/{sessionExerciseId}",
        body);

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
}

[Fact]
public async Task FinishWorkoutSession_ShouldReturnNotFound_ForOtherUsersSession()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user1 = await UserHelper.CreateUserAsync(userManager);
    var user2 = await UserHelper.CreateUserAsync(userManager);

    var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user2);
    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(
        db, user2, null, [exercise]);

    AuthHelper.AuthorizeClient(_client, user1, tokenService);

    // Act
    var res = await _client.PatchAsync(
        $"/api/workout-sessions/{session.Id}/finish",
        null);

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);

    db.ChangeTracker.Clear();

    var sessionFromDb = await db.WorkoutSessions
        .FirstOrDefaultAsync(x => x.Id == session.Id);

    Assert.NotNull(sessionFromDb);
    Assert.Equal(user2.Id, sessionFromDb.UserId);
    Assert.Null(sessionFromDb.FinishedAt);
}

[Fact]
public async Task FinishWorkoutSession_WithEmptySessionId_ShouldReturnBadRequest()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user = await UserHelper.CreateUserAsync(userManager);
    AuthHelper.AuthorizeClient(_client, user, tokenService);

    // Act
    var res = await _client.PatchAsync(
        $"/api/workout-sessions/{Guid.Empty}/finish",
        null);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
}

[Fact]
public async Task FinishWorkoutSession_ShouldSetFinishedAt_WhenSessionExists()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user = await UserHelper.CreateUserAsync(userManager);
    var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);
    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(
        db, user, null, [exercise]);

    AuthHelper.AuthorizeClient(_client, user, tokenService);

    Assert.Null(session.FinishedAt);

    var beforeRequest = DateTime.UtcNow;

    // Act
    var res = await _client.PatchAsync(
        $"/api/workout-sessions/{session.Id}/finish",
        null);

    var afterRequest = DateTime.UtcNow;

    // Assert
    Assert.Equal(HttpStatusCode.OK, res.StatusCode);

    db.ChangeTracker.Clear();

    var sessionFromDb = await db.WorkoutSessions
        .FirstOrDefaultAsync(x => x.Id == session.Id);

    Assert.NotNull(sessionFromDb);
    Assert.NotNull(sessionFromDb.FinishedAt);

    Assert.InRange(
        sessionFromDb.FinishedAt.Value,
        beforeRequest,
        afterRequest);
}

[Fact]
public async Task FinishWorkoutSession_ShouldNotChangeFinishedAt_WhenSessionAlreadyFinished()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user = await UserHelper.CreateUserAsync(userManager);
    var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);
    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(
        db, user, null, [exercise]);

    AuthHelper.AuthorizeClient(_client, user, tokenService);

    var url = $"/api/workout-sessions/{session.Id}/finish";

    var firstResponse = await _client.PatchAsync(url, null);
    Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

    db.ChangeTracker.Clear();

    var sessionFromDb = await db.WorkoutSessions
        .FirstAsync(x => x.Id == session.Id);

    Assert.NotNull(sessionFromDb.FinishedAt);

    // Ustawiamy kontrolowaną datę, aby test był deterministyczny.
    var firstFinishedAt = sessionFromDb.FinishedAt.Value.AddMinutes(-10);
    sessionFromDb.FinishedAt = firstFinishedAt;
    await db.SaveChangesAsync();

    // Act
    var secondResponse = await _client.PatchAsync(url, null);

    // Assert
    Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

    db.ChangeTracker.Clear();

    var updatedSession = await db.WorkoutSessions
        .FirstAsync(x => x.Id == session.Id);

    Assert.Equal(firstFinishedAt, updatedSession.FinishedAt);
}

[Fact]
    public async Task PatchWorkoutSessionExercise_ShouldReturnConflict_WhenSessionIsFinished()
    {
        //arrange
        using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

        var user = await UserHelper.CreateUserAsync(userManager);
        var exercise = await ExerciseHelper.CreateUserExerciseAsync(db,user);
        var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(db,user,null,[exercise]);
        var originalSets = session.Exercises[0].Sets
            .OrderBy(x => x.SetNumber)
            .Select(x => new { x.Id, x.SetNumber, x.Reps, x.Weight })
            .ToList();
        AuthHelper.AuthorizeClient(_client,user,tokenService);
        var finishSession = await _client.PatchAsync($"/api/workout-sessions/{session.Id}/finish",null);
        Assert.Equal(HttpStatusCode.OK, finishSession.StatusCode);
        db.ChangeTracker.Clear();
        //act
        var requestBody = new UpdateWorkoutSessionExerciseRequest
        (
            [
             new UpdateWorkoutSessionSetRequest
             (
                session.Exercises[0].Sets[0].Id,1,12,12.0m
             )
            ]
        );
        var res = await _client.PatchAsJsonAsync($"/api/workout-sessions/{session.Id}/exercises/{session.Exercises[0].Id}",requestBody);
        Assert.Equal(HttpStatusCode.Conflict,res.StatusCode);
        var error = await res.Content.ReadFromJsonAsync<string>();

        Assert.Equal("Nie można edytować zakończonej sesji",error);
        db.ChangeTracker.Clear();
        var sessionAfterRequest = await db.WorkoutSessions.Include(x=>x.Exercises).ThenInclude(y=>y.Sets).FirstOrDefaultAsync(x=>x.Id==session.Id);
        Assert.NotNull(sessionAfterRequest);
        var actualSets = sessionAfterRequest.Exercises[0].Sets
            .OrderBy(x => x.SetNumber)
            .Select(x => new { x.Id, x.SetNumber, x.Reps, x.Weight })
            .ToList();

        Assert.Equal(originalSets, actualSets);
    }

[Fact]
public async Task FinishWorkoutSession_ShouldReturnNotFound_WhenSessionDoesNotExist()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user = await UserHelper.CreateUserAsync(userManager);
    AuthHelper.AuthorizeClient(_client, user, tokenService);

    var nonExistingSessionId = Guid.NewGuid();

    // Act
    var response = await _client.PatchAsync(
        $"/api/workout-sessions/{nonExistingSessionId}/finish",
        null);

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}

[Fact]
public async Task DeleteWorkoutSessionExercise_ShouldRemoveExerciseAndReorderRemainingExercises()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user = await UserHelper.CreateUserAsync(userManager);

    var exercise1 = await ExerciseHelper.CreateUserExerciseAsync(db, user);
    var exercise2 = await ExerciseHelper.CreateUserExerciseAsync(db, user);
    var exercise3 = await ExerciseHelper.CreateUserExerciseAsync(db, user);

    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(
        db, user, null, [exercise1, exercise2, exercise3]);

    var sessionExercises = session.Exercises
        .OrderBy(x => x.Order)
        .ToList();

    var firstExerciseId = sessionExercises[0].Id;
    var deletedExerciseId = sessionExercises[1].Id;
    var thirdExerciseId = sessionExercises[2].Id;

    var deletedSetIds = sessionExercises[1].Sets
        .Select(x => x.Id)
        .ToList();

    AuthHelper.AuthorizeClient(_client, user, tokenService);

    // Act
    var response = await _client.DeleteAsync(
        $"/api/workout-sessions/{session.Id}/exercises/{deletedExerciseId}");

    // Assert
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    db.ChangeTracker.Clear();

    var sessionFromDb = await db.WorkoutSessions
        .Include(x => x.Exercises)
            .ThenInclude(x => x.Sets)
        .SingleAsync(x => x.Id == session.Id);

    var remainingExercises = sessionFromDb.Exercises
        .OrderBy(x => x.Order)
        .ToList();

    Assert.Equal(2, remainingExercises.Count);

    Assert.Collection(
        remainingExercises,
        first =>
        {
            Assert.Equal(firstExerciseId, first.Id);
            Assert.Equal(1, first.Order);
        },
        second =>
        {
            Assert.Equal(thirdExerciseId, second.Id);
            Assert.Equal(2, second.Order);
        }
    );

    var deletedExerciseExists = await db.WorkoutSessionExercises
        .AnyAsync(x => x.Id == deletedExerciseId);

    Assert.False(deletedExerciseExists);

    var deletedSetsExist = await db.WorkoutSessionSets
        .AnyAsync(x => deletedSetIds.Contains(x.Id));

    Assert.False(deletedSetsExist);
}

[Fact]
public async Task DeleteWorkoutSessionExercise_ShouldReturnNotFound_ForOtherUsersSession()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user1 = await UserHelper.CreateUserAsync(userManager);
    var user2 = await UserHelper.CreateUserAsync(userManager);

    var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user2);

    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(
        db, user2, null, [exercise]);

    var sessionExercise = Assert.Single(session.Exercises);

    var sessionExerciseId = sessionExercise.Id;

    var originalSets = sessionExercise.Sets
        .OrderBy(x => x.SetNumber)
        .Select(x => new { x.Id, x.SetNumber, x.Reps, x.Weight })
        .ToList();

    AuthHelper.AuthorizeClient(_client, user1, tokenService);

    // Act
    var response = await _client.DeleteAsync(
        $"/api/workout-sessions/{session.Id}/exercises/{sessionExerciseId}");

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

    db.ChangeTracker.Clear();

    var exerciseFromDb = await db.WorkoutSessionExercises
        .Include(x => x.Sets)
        .SingleOrDefaultAsync(x => x.Id == sessionExerciseId);

    Assert.NotNull(exerciseFromDb);

    var actualSets = exerciseFromDb.Sets
        .OrderBy(x => x.SetNumber)
        .Select(x => new { x.Id, x.SetNumber, x.Reps, x.Weight })
        .ToList();

    Assert.Equal(originalSets, actualSets);
}

[Fact]
public async Task DeleteWorkoutSessionExercise_ShouldReturnNotFound_WhenExerciseDoesNotExistInSession()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user = await UserHelper.CreateUserAsync(userManager);
    var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);

    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(
        db, user, null, [exercise]);

    var sessionExercise = Assert.Single(session.Exercises);

    var originalSets = sessionExercise.Sets
        .OrderBy(x => x.SetNumber)
        .Select(x => new { x.Id, x.SetNumber, x.Reps, x.Weight })
        .ToList();

    var nonExistingSessionExerciseId = Guid.NewGuid();

    AuthHelper.AuthorizeClient(_client, user, tokenService);

    // Act
    var response = await _client.DeleteAsync(
        $"/api/workout-sessions/{session.Id}/exercises/{nonExistingSessionExerciseId}");

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

    db.ChangeTracker.Clear();

    var sessionFromDb = await db.WorkoutSessions
        .Include(x => x.Exercises)
            .ThenInclude(x => x.Sets)
        .SingleAsync(x => x.Id == session.Id);

    var remainingExercise = Assert.Single(sessionFromDb.Exercises);

    Assert.Equal(sessionExercise.Id, remainingExercise.Id);
    Assert.Equal(exercise.Id, remainingExercise.ExerciseId);

    var actualSets = remainingExercise.Sets
        .OrderBy(x => x.SetNumber)
        .Select(x => new { x.Id, x.SetNumber, x.Reps, x.Weight })
        .ToList();

    Assert.Equal(originalSets, actualSets);
}

[Fact]
public async Task DeleteWorkoutSessionExercise_ShouldReturnConflict_WhenRemovingLastExercise()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user = await UserHelper.CreateUserAsync(userManager);
    var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);

    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(
        db, user, null, [exercise]);

    var sessionExercise = Assert.Single(session.Exercises);

    var originalSets = sessionExercise.Sets
        .OrderBy(x => x.SetNumber)
        .Select(x => new { x.Id, x.SetNumber, x.Reps, x.Weight })
        .ToList();

    AuthHelper.AuthorizeClient(_client, user, tokenService);

    // Act
    var response = await _client.DeleteAsync(
        $"/api/workout-sessions/{session.Id}/exercises/{sessionExercise.Id}");

    // Assert
    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

    db.ChangeTracker.Clear();

    var sessionFromDb = await db.WorkoutSessions
        .Include(x => x.Exercises)
            .ThenInclude(x => x.Sets)
        .SingleAsync(x => x.Id == session.Id);

    var remainingExercise = Assert.Single(sessionFromDb.Exercises);

    Assert.Equal(sessionExercise.Id, remainingExercise.Id);
    Assert.Equal(1, remainingExercise.Order);

    var actualSets = remainingExercise.Sets
        .OrderBy(x => x.SetNumber)
        .Select(x => new { x.Id, x.SetNumber, x.Reps, x.Weight })
        .ToList();

    Assert.Equal(originalSets, actualSets);
}

[Fact]
public async Task DeleteWorkoutSessionExercise_ShouldReturnConflict_WhenSessionIsFinished()
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user = await UserHelper.CreateUserAsync(userManager);

    var exercise1 = await ExerciseHelper.CreateUserExerciseAsync(db, user);
    var exercise2 = await ExerciseHelper.CreateUserExerciseAsync(db, user);

    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(
        db, user, null, [exercise1, exercise2]);

    var sessionExercise = session.Exercises
        .OrderBy(x => x.Order)
        .First();

    var originalExercises = session.Exercises
        .OrderBy(x => x.Order)
        .Select(x => new
        {
            x.Id,
            x.ExerciseId,
            x.Order,
            Sets = x.Sets
                .OrderBy(s => s.SetNumber)
                .Select(s => new { s.Id, s.SetNumber, s.Reps, s.Weight })
                .ToList()
        })
        .ToList();

    AuthHelper.AuthorizeClient(_client, user, tokenService);

    var finishResponse = await _client.PatchAsync(
        $"/api/workout-sessions/{session.Id}/finish", null);

    Assert.Equal(HttpStatusCode.OK, finishResponse.StatusCode);

    // Act
    var response = await _client.DeleteAsync(
        $"/api/workout-sessions/{session.Id}/exercises/{sessionExercise.Id}");

    // Assert
    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

    db.ChangeTracker.Clear();

    var sessionFromDb = await db.WorkoutSessions
        .Include(x => x.Exercises)
            .ThenInclude(x => x.Sets)
        .SingleAsync(x => x.Id == session.Id);

    Assert.NotNull(sessionFromDb.FinishedAt);

    var actualExercises = sessionFromDb.Exercises
        .OrderBy(x => x.Order)
        .Select(x => new
        {
            x.Id,
            x.ExerciseId,
            x.Order,
            Sets = x.Sets
                .OrderBy(s => s.SetNumber)
                .Select(s => new { s.Id, s.SetNumber, s.Reps, s.Weight })
                .ToList()
        })
        .ToList();

    Assert.Equal(originalExercises.Count, actualExercises.Count);

    for (int i = 0; i < originalExercises.Count; i++)
    {
        Assert.Equal(originalExercises[i].Id, actualExercises[i].Id);
        Assert.Equal(originalExercises[i].ExerciseId, actualExercises[i].ExerciseId);
        Assert.Equal(originalExercises[i].Order, actualExercises[i].Order);
        Assert.Equal(originalExercises[i].Sets, actualExercises[i].Sets);
    }
}

[Theory]
[InlineData(true)]
[InlineData(false)]
public async Task DeleteWorkoutSessionExercise_ShouldReturnBadRequest_WhenIdIsEmpty(
    bool emptySessionId)
{
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

    var user = await UserHelper.CreateUserAsync(userManager);
    var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user);

    var session = await WorkoutSessionHelper.CreateWorkoutSessionAsync(
        db, user, null, [exercise]);

    var sessionExercise = Assert.Single(session.Exercises);

    var originalSets = sessionExercise.Sets
        .OrderBy(x => x.SetNumber)
        .Select(x => new { x.Id, x.SetNumber, x.Reps, x.Weight })
        .ToList();

    AuthHelper.AuthorizeClient(_client, user, tokenService);

    var sessionId = emptySessionId ? Guid.Empty : session.Id;
    var sessionExerciseId = emptySessionId
        ? sessionExercise.Id
        : Guid.Empty;

    // Act
    var response = await _client.DeleteAsync(
        $"/api/workout-sessions/{sessionId}/exercises/{sessionExerciseId}");

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    db.ChangeTracker.Clear();

    var sessionFromDb = await db.WorkoutSessions
        .Include(x => x.Exercises)
            .ThenInclude(x => x.Sets)
        .SingleAsync(x => x.Id == session.Id);

    var remainingExercise = Assert.Single(sessionFromDb.Exercises);

    Assert.Equal(sessionExercise.Id, remainingExercise.Id);

    var actualSets = remainingExercise.Sets
        .OrderBy(x => x.SetNumber)
        .Select(x => new { x.Id, x.SetNumber, x.Reps, x.Weight })
        .ToList();

    Assert.Equal(originalSets, actualSets);
}

}

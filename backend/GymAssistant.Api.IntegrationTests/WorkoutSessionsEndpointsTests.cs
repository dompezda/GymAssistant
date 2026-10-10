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
                new WorkoutSessionSetSummaryDto(sessionExercise.Sets[0].Id,1,reps,weight),
                new WorkoutSessionSetSummaryDto(sessionExercise.Sets[1].Id,2,reps+1,weight+1)
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
            new WorkoutSessionSetSummaryDto(set1Id, 1, 1, 11m),
            new WorkoutSessionSetSummaryDto(set2Id, 2, 2, 2m),
            new WorkoutSessionSetSummaryDto(set3Id, 3, 10, 50m)
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
            new WorkoutSessionSetSummaryDto(
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
            new WorkoutSessionSetSummaryDto(set1Id, 1, reps, weightInt),
            new WorkoutSessionSetSummaryDto(set2Id, 2, 15, 50m)
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
            new WorkoutSessionSetSummaryDto(set1Id, 1, 100, 200m),
            new WorkoutSessionSetSummaryDto(set2Id, 2, 100, 200m)
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
            new WorkoutSessionSetSummaryDto(Guid.NewGuid(), 1, 10, 50m)
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
            new WorkoutSessionSetSummaryDto(set1Id, firstSetNumber, 10, 50m),
            new WorkoutSessionSetSummaryDto(set2Id, secondSetNumber, 20, 60m),
            new WorkoutSessionSetSummaryDto(set3Id, thirdSetNumber, 30, 70m)
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
            new WorkoutSessionSetSummaryDto(set1Id, 1, 10, 50m),
            new WorkoutSessionSetSummaryDto(set2Id, 2, 20, 60m),
            new WorkoutSessionSetSummaryDto(Guid.Empty, 3, 30, 70m)
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
            new WorkoutSessionSetSummaryDto(set1Id, 1, 10, 50m),
            new WorkoutSessionSetSummaryDto(set2Id, 2, 20, 60m),
            new WorkoutSessionSetSummaryDto(set1Id, 3, 30, 70m)
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
            new WorkoutSessionSetSummaryDto(Guid.NewGuid(), 1, 10, 50m)
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
}
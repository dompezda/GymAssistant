using System.Net;
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
    public async Task GetWorkoutTemplate_ShouldReturnCurrentUsersTemplate()
    {
        //Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

        var user1 = await UserHelper.CreateUserAsync(userManager);
        var exercise1 = await ExerciseHelper.CreateUserExerciseAsync(db, user1);
        var template = await WorkoutTemplateHelper.CreateWorkoutTemplateAsync(db,user1,new List<Exercise> {exercise1});
        AuthHelper.AuthorizeClient(_client,user1,tokenService);
        db.ChangeTracker.Clear();
        //Act    
        var response = await _client.GetAsync($"/api/workout-templates/{template.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var res = await response.Content.ReadFromJsonAsync<WorkoutTemplateResponseDto>();

        //Assert
        Assert.NotNull(res);
        Assert.Equal(template.Id, res.Id);
        Assert.Equal(template.Title, res.Title);
        Assert.Single(res.Exercises);
        var exerciseFromResponse = res.Exercises[0];
        Assert.Equal(exercise1.Id, exerciseFromResponse.ExerciseId);
        Assert.Equal(exercise1.Name, exerciseFromResponse.ExerciseName);
        Assert.Equal(3, exerciseFromResponse.PlannedSets);
        Assert.Equal(1, exerciseFromResponse.Order);
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
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
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
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
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
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
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
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PatchWorkoutTemplate_ShouldUpdateTemplate()
    {
        //Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();
        var user = await UserHelper.CreateUserAsync(userManager);
        var exercise1 = await ExerciseHelper.CreateUserExerciseAsync(db, user);
        var exercise2 = await ExerciseHelper.CreateUserExerciseAsync(db, user);
        var template = await WorkoutTemplateHelper.CreateWorkoutTemplateAsync(db, user, [exercise1]);
        AuthHelper.AuthorizeClient(_client, user, tokenService);

        //Act
        //CreateWorkoutTemplateRequest
        var request = new CreateWorkoutTemplateRequest
        (
            "Updated Title",
            "Updated Notes",
            new List<CreateWorkoutTemplateExerciseRequest>
            {
                new CreateWorkoutTemplateExerciseRequest
                (
                    exercise2.Id,
                    5,
                    1
                )
            }
        );
        var response = await _client.PatchAsJsonAsync($"/api/workout-templates/{template.Id}", request);
        db.ChangeTracker.Clear();
        //Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var updatedTemplate = await db.WorkoutTemplates
            .Include(x => x.Exercises)
            .FirstOrDefaultAsync(x => x.Id == template.Id);

        Assert.NotNull(updatedTemplate);
        Assert.Equal("Updated Title", updatedTemplate.Title);
        Assert.Equal("Updated Notes", updatedTemplate.Notes);
        var updatedExercise = updatedTemplate.Exercises.Single();
        Assert.Equal(exercise2.Id, updatedExercise.ExerciseId);
        Assert.Equal(5, updatedExercise.PlannedSets);
        Assert.Equal(1, updatedExercise.Order);
    }

    [Fact]
    public async Task PatchWorkoutTemplate_ShouldNormalizeExerciseOrder()
    {
        //Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

        var user = await UserHelper.CreateUserAsync(userManager);
        var exercise1 = await ExerciseHelper.CreateUserExerciseAsync(db, user);
        var exercise2 = await ExerciseHelper.CreateUserExerciseAsync(db, user);
        var exercise3 = await ExerciseHelper.CreateUserExerciseAsync(db, user);
        var template = await WorkoutTemplateHelper.CreateWorkoutTemplateAsync(db, user, [exercise1,exercise3]);
        AuthHelper.AuthorizeClient(_client, user, tokenService);
        //Act
        var request = new CreateWorkoutTemplateRequest
        (
            "Updated Title",
            "Updated Notes",
            new List<CreateWorkoutTemplateExerciseRequest>
            {
                new CreateWorkoutTemplateExerciseRequest
                (
                    exercise1.Id,
                    5,
                    3
                ),
                new CreateWorkoutTemplateExerciseRequest
                (
                    exercise2.Id,
                    4,
                    1
                ),
                new CreateWorkoutTemplateExerciseRequest
                (
                    exercise3.Id,
                    4,
                    2
                )
            }
        );

        var response = await _client.PatchAsJsonAsync($"/api/workout-templates/{template.Id}", request);
        db.ChangeTracker.Clear();
        //Assert
        var updatedTemplate = await db.WorkoutTemplates
            .Include(x => x.Exercises)
            .FirstOrDefaultAsync(x => x.Id == template.Id);

        Assert.NotNull(updatedTemplate);
        var orderedExercises = updatedTemplate.Exercises.OrderBy(x => x.Order).ToList();
        Assert.Equal(exercise2.Id, orderedExercises[0].ExerciseId);
        Assert.Equal(exercise3.Id, orderedExercises[1].ExerciseId);
        Assert.Equal(exercise1.Id, orderedExercises[2].ExerciseId);
    }

    [Fact]
    public async Task PatchWorkoutTemplate_ShouldReturnNotFound_ForOtherUsersTemplate()
    {

        //Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();
        var user1 = await UserHelper.CreateUserAsync(userManager);
        var user2 = await UserHelper.CreateUserAsync(userManager);
        var exercise = await ExerciseHelper.CreateUserExerciseAsync(db, user2);
        var template = await WorkoutTemplateHelper.CreateWorkoutTemplateAsync(db, user2, [exercise]);
        AuthHelper.AuthorizeClient(_client, user1, tokenService);
        //Act
        var request = new CreateWorkoutTemplateRequest
        (
            "Updated Title",
            "Updated Notes",
            new List<CreateWorkoutTemplateExerciseRequest>
            {
                new CreateWorkoutTemplateExerciseRequest
                (
                    exercise.Id,
                    5,
                    1
                )
            }
        );
        var response = await _client.PatchAsJsonAsync($"/api/workout-templates/{template.Id}", request);

        //Assert
        Assert.Equal(HttpStatusCode.NotFound,response.StatusCode);
        db.ChangeTracker.Clear();
        var templateFromDb = await db.WorkoutTemplates
            .Include(x=>x.Exercises)
            .FirstOrDefaultAsync(x=>x.Id==template.Id);
        Assert.NotNull(templateFromDb);
        Assert.Equal(template.Title,templateFromDb.Title);
        Assert.Equal(template.Notes,templateFromDb.Notes);
        var templateExercise = Assert.Single(templateFromDb.Exercises);
        Assert.Equal(exercise.Id, templateExercise.ExerciseId);
        Assert.Equal(3, templateExercise.PlannedSets);
        Assert.Equal(1, templateExercise.Order); 
    }

    [Fact]
    public async Task PatchWorkoutTemplate_WithoutTitle_ShouldReturnBadRequest()
    {
        //Arrange
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenSerivce= scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

        var user = await UserHelper.CreateUserAsync(userManager);
        var exercise1 = await ExerciseHelper.CreateSystemExerciseAsync(db);
        var template = await WorkoutTemplateHelper.CreateWorkoutTemplateAsync(db,user,[exercise1]);
        AuthHelper.AuthorizeClient(_client,user,tokenSerivce);

        //Act
        var requestBody= new CreateWorkoutTemplateRequest
        (
            "",template.Notes,
            [
                new CreateWorkoutTemplateExerciseRequest(exercise1.Id,3,3)
            ]
        );
        var res = await _client.PatchAsJsonAsync($"/api/workout-templates/{template.Id}",requestBody);
        //Assert
        Assert.Equal(HttpStatusCode.BadRequest,res.StatusCode);
    }

    [Fact]
    public async Task PatchWorkoutTemplate_WithoutExercises_ShouldReturnBadRequest()
    {
        //Arrange
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenSerivce= scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

        var user = await UserHelper.CreateUserAsync(userManager);
        var exercise1 = await ExerciseHelper.CreateSystemExerciseAsync(db);
        var template = await WorkoutTemplateHelper.CreateWorkoutTemplateAsync(db,user,[exercise1]);
        var oldTitle = template.Title;
        AuthHelper.AuthorizeClient(_client,user,tokenSerivce);

        //Act
        var requestBody= new CreateWorkoutTemplateRequest
        (
            template.Title,template.Notes,
            []
        );
        var res = await _client.PatchAsJsonAsync($"/api/workout-templates/{template.Id}",requestBody);
        //Assert
        Assert.Equal(HttpStatusCode.BadRequest,res.StatusCode);
    }

    [Fact]
    public async Task PatchWorkoutTemplate_WithOtherUsersExercise_ShouldReturnBadRequest()
    {
        //Arrange
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

        var user1 = await UserHelper.CreateUserAsync(userManager);
        var user2 = await UserHelper.CreateUserAsync(userManager);

        var exercise1 = await ExerciseHelper.CreateUserExerciseAsync(db,user1);
        var exercise2 = await ExerciseHelper.CreateUserExerciseAsync(db,user2);

        var template = await WorkoutTemplateHelper.CreateWorkoutTemplateAsync(db,user1,[exercise1]);

        AuthHelper.AuthorizeClient(_client,user1,tokenService);
        //Act
        var requestBody= new CreateWorkoutTemplateRequest
        (
            template.Title,template.Notes,
            [
                new CreateWorkoutTemplateExerciseRequest(exercise2.Id,3,3)
            ]
        );
        var res = await _client.PatchAsJsonAsync($"/api/workout-templates/{template.Id}",requestBody);
        //Assert
        Assert.Equal(HttpStatusCode.BadRequest,res.StatusCode);
        db.ChangeTracker.Clear();
        var templateFromDb = await db.WorkoutTemplates.Include(x=>x.Exercises).FirstOrDefaultAsync(x=>x.Id==template.Id);
        Assert.NotNull(templateFromDb);
        var templateExercise = Assert.Single(templateFromDb.Exercises);
        Assert.Equal(exercise1.Id, templateExercise.ExerciseId);
    }

    [Fact] //TEN TEST W PRZYSZLOSCI BEDZIE DO SPRAWDZENIA BO W SUMIE MOZNA CHYBA 2 TAKIE SAME CWICZENIA MIEC W PLANIE
    public async Task PatchWorkoutTemplate_WithDuplicateExercises_ShouldReturnBadRequest()
    {
        //Arrange
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

        var user1 = await UserHelper.CreateUserAsync(userManager);
        var exercise = await ExerciseHelper.CreateUserExerciseAsync(db,user1);

        var template = await WorkoutTemplateHelper.CreateWorkoutTemplateAsync(db,user1,[exercise]);

        AuthHelper.AuthorizeClient(_client,user1,tokenService);
        //Act
        var requestBody= new CreateWorkoutTemplateRequest
        (
            template.Title,template.Notes,
            [
                new CreateWorkoutTemplateExerciseRequest(exercise.Id,3,1),
                new CreateWorkoutTemplateExerciseRequest(exercise.Id,4,2)
            ]
        );
        var res = await _client.PatchAsJsonAsync($"/api/workout-templates/{template.Id}",requestBody);
        //Assert
        Assert.Equal(HttpStatusCode.BadRequest,res.StatusCode);
    }

    [Fact] 
    public async Task PatchWorkoutTemplate_WithNonPositivePlannedSets_ShouldReturnBadRequest()
    {
        //Arrange
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

        var user1 = await UserHelper.CreateUserAsync(userManager);
        var exercise = await ExerciseHelper.CreateUserExerciseAsync(db,user1);

        var template = await WorkoutTemplateHelper.CreateWorkoutTemplateAsync(db,user1,[exercise]);

        AuthHelper.AuthorizeClient(_client,user1,tokenService);
        //Act
        var requestBody= new CreateWorkoutTemplateRequest
        (
            template.Title,template.Notes,
            [
                new CreateWorkoutTemplateExerciseRequest(exercise.Id,-2,1),
            ]
        );
        var res = await _client.PatchAsJsonAsync($"/api/workout-templates/{template.Id}",requestBody);
        //Assert
        Assert.Equal(HttpStatusCode.BadRequest,res.StatusCode);
    }    
    
    [Fact] 
    public async Task PatchWorkoutTemplate_WithSystemExercise_ShouldUpdateTemplate()
    {
        //Arrange
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

        var user1 = await UserHelper.CreateUserAsync(userManager);
        var exercise = await ExerciseHelper.CreateUserExerciseAsync(db,user1);
        var systemExercise = await ExerciseHelper.CreateSystemExerciseAsync(db);
        var template = await WorkoutTemplateHelper.CreateWorkoutTemplateAsync(db,user1,[exercise]);

        AuthHelper.AuthorizeClient(_client,user1,tokenService);
        //Act
        var requestBody= new CreateWorkoutTemplateRequest
        (
            template.Title,template.Notes,
            [
                new CreateWorkoutTemplateExerciseRequest(exercise.Id,1,1),
                new CreateWorkoutTemplateExerciseRequest(systemExercise.Id,2,2),
            ]
        );
        var res = await _client.PatchAsJsonAsync($"/api/workout-templates/{template.Id}",requestBody);
        //Assert
        Assert.Equal(HttpStatusCode.NoContent,res.StatusCode);
        db.ChangeTracker.Clear();

        var templateFromDb = await db.WorkoutTemplates
            .Include(x=>x.Exercises)
            .FirstOrDefaultAsync(x=>x.Id == template.Id);
        
        Assert.NotNull(templateFromDb);
        Assert.Equal(template.Notes,templateFromDb.Notes);
        Assert.Equal(2,templateFromDb.Exercises.Count);
        Assert.Contains(
            templateFromDb.Exercises,
            x => x.ExerciseId == systemExercise.Id
        );
    }

    [Fact] 
    public async Task PatchWorkoutTemplate_WithNonExistingId_ShouldReturnNotFound()
    {
        //Arrange
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAccessTokenService>();

        var user1 = await UserHelper.CreateUserAsync(userManager);
        var exercise = await ExerciseHelper.CreateUserExerciseAsync(db,user1);
        AuthHelper.AuthorizeClient(_client,user1,tokenService);
        var nonExistingId = Guid.NewGuid();
        //Act

        var requestBody= new CreateWorkoutTemplateRequest
        (
            $"Test Template","notes",
            [
                new CreateWorkoutTemplateExerciseRequest(exercise.Id,1,1)
            ]
        );
        var res = await _client.PatchAsJsonAsync($"/api/workout-templates/{nonExistingId}",requestBody);
        //Assert
        Assert.Equal(HttpStatusCode.NotFound,res.StatusCode);
    }
}
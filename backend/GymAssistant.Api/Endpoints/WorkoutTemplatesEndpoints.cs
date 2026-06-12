using GymAssistant.Api.Data;
using GymAssistant.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GymAssistant.Api.Endpoints;

public static class WorkoutTemplatesEndpoints
{
    public static void MapWorkoutTemplatesEndpoints(this WebApplication app)
    {
        app.MapGet("/api/workout-templates", async (AppDbContext db) =>
        {
            var result = await db.WorkoutTemplates
                .Select(x => new WorkoutTemplateListItemResponse(
                    x.Id,
                    x.Title,
                    x.Notes
                ))
                .ToListAsync();

            return Results.Ok(result);
        });

        app.MapPost("/api/workout-templates", async (
            CreateWorkoutTemplateRequest request,
            AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Results.BadRequest("Title is required.");
            }

            if (request.Exercises.Count == 0)
            {
                return Results.BadRequest("Workout template must contain at least one exercise.");
            }

            var exerciseIds = request.Exercises
                .Select(x => x.ExerciseId)
                .ToList();

            var existingExercisesCount = await db.Exercises
                .CountAsync(x => exerciseIds.Contains(x.Id));

            if (existingExercisesCount != exerciseIds.Count)
            {
                return Results.BadRequest("One or more exercises do not exist.");
            }

            var workoutTemplate = new WorkoutTemplate
            {
                Id = Guid.NewGuid(),
                UserId = "Caran",
                Title = request.Title.Trim(),
                Notes = string.IsNullOrWhiteSpace(request.Notes)
                    ? null
                    : request.Notes.Trim(),
                Exercises = request.Exercises
                    .OrderBy(x => x.Order)
                    .Select((x, index) => new WorkoutTemplateExercise
                    {
                        Id = Guid.NewGuid(),
                        ExerciseId = x.ExerciseId,
                        Order = index + 1,
                        PlannedSets = x.PlannedSets
                    })
                    .ToList()
            };

            db.WorkoutTemplates.Add(workoutTemplate);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/api/workout-templates/{workoutTemplate.Id}",
                new CreateWorkoutTemplateResponse(workoutTemplate.Id)
            );
        });
    
        app.MapGet("/api/workout-templates/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var workoutTemplate = await db.WorkoutTemplates
                .AsNoTracking()
                .Include(x => x.Exercises)
                .ThenInclude(x => x.Exercise)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (workoutTemplate == null)
            {
                return Results.NotFound();
            }

            var response = new WorkoutTemplateResponseDto(
                workoutTemplate.Id,
                workoutTemplate.Title,
                workoutTemplate.Notes,
                workoutTemplate.Exercises
                    .OrderBy(x => x.Order)
                    .Select(x => new WorkoutTemplateExerciseResponseDto(
                        x.ExerciseId,
                        x.Exercise?.Name ?? "Unknown exercise",
                        x.PlannedSets,
                        x.Order
                    ))
                    .ToList()
            );

            return Results.Ok(response);
        });
    }
}
public record WorkoutTemplateResponseDto(
    Guid Id,
    string Title,
    string? Notes,
    List<WorkoutTemplateExerciseResponseDto> Exercises
);

public record WorkoutTemplateExerciseResponseDto(
    Guid ExerciseId,
    string ExerciseName,
    int PlannedSets,
    int Order
);
public record WorkoutTemplateListItemResponse(
    Guid Id,
    string Title,
    string? Notes
);

public record CreateWorkoutTemplateRequest(
    string Title,
    string? Notes,
    List<CreateWorkoutTemplateExerciseRequest> Exercises
);

public record CreateWorkoutTemplateExerciseRequest(
    Guid ExerciseId,
    int PlannedSets,
    int Order
);

public record CreateWorkoutTemplateResponse(Guid Id);
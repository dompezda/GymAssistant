using GymAssistant.Api.Authentication;
using GymAssistant.Api.Data;
using GymAssistant.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GymAssistant.Api.Endpoints;

public static class WorkoutTemplatesEndpoints
{
    public static void MapWorkoutTemplatesEndpoints(this WebApplication app)
    {

        var group = app.MapGroup("/api/workout-templates")
            .RequireAuthorization();
        group.MapGet("", async (AppDbContext db, ICurrentUser currentUser) =>
        {
            var result = await db.WorkoutTemplates
                .Where(x=>x.UserId == currentUser.UserId)
                .Select(x => new WorkoutTemplateListItemResponse(
                    x.Id,
                    x.Title,
                    x.Notes
                ))
                .ToListAsync();

            return Results.Ok(result);
        });

        group.MapPost("", async (
            CreateWorkoutTemplateRequest request,
            AppDbContext db,
            ICurrentUser currentUser) =>
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
                .Where(x=>x.OwnerUserId==currentUser.UserId || x.IsSystem)
                .CountAsync(x => exerciseIds.Contains(x.Id));

            if(request.Exercises.Count != request.Exercises.Select(x=>x.ExerciseId).Distinct().Count())
            {
                return Results.BadRequest("Duplicate exercises are not allowed.");
            }
            if (existingExercisesCount != exerciseIds.Count)
            {
                return Results.BadRequest("One or more exercises do not exist.");
            }


            var workoutTemplate = new WorkoutTemplate
            {
                Id = Guid.NewGuid(),
                UserId = currentUser.UserId,
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
    
        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db, ICurrentUser currentUser) =>
        {
            var workoutTemplate = await db.WorkoutTemplates
                .AsNoTracking()
                .Include(x => x.Exercises)
                .ThenInclude(x => x.Exercise)
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == currentUser.UserId);

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

        group.MapPatch("/{id:guid}", async (
            Guid id,
            CreateWorkoutTemplateRequest request,
            AppDbContext db,
            ICurrentUser currentUser) =>
        {
           var template = await db.WorkoutTemplates
                .Include(x => x.Exercises)
                .ThenInclude(x => x.Exercise)
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == currentUser.UserId);
           
           if(template == null)
            {
                return Results.NotFound();
            }
            if(request.Title==null || request.Title.Trim() == "")
            {
                return Results.BadRequest("Title is required.");
            }
            if(request.Exercises.Count == 0)
            {
                return Results.BadRequest("Workout template must contain at least one exercise.");
            }
            if(request.Exercises.Count != request.Exercises.Select(x=>x.ExerciseId).Distinct().Count())
            {
                return Results.BadRequest("Duplicate exercises are not allowed.");
            }

            if(template.Title != request.Title.Trim())
            {
                template.Title = request.Title.Trim();
            }

            if(template.Notes != request.Notes)
            {
                template.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
            }
            if(request.Exercises.All(x=>x.PlannedSets>0) == false)
            {
                return Results.BadRequest("Planned sets must be greater than 0.");
            }
            var existingExercisesCount = await db.Exercises
             .Where(x=>x.OwnerUserId==currentUser.UserId || x.IsSystem)
             .CountAsync(x => request.Exercises.Select(e => e.ExerciseId).Contains(x.Id));

            if (existingExercisesCount != request.Exercises.Count)
            {
                return Results.BadRequest("One or more exercises do not exist.");
            }

            var requestByExerciseId = new Dictionary<Guid, CreateWorkoutTemplateExerciseRequest>();
            foreach(var exercise in request.Exercises)
            {
                requestByExerciseId[exercise.ExerciseId] = exercise;
            }
            var templateByExerciseId = new Dictionary<Guid, WorkoutTemplateExercise>();
            foreach(var exercise in template.Exercises)
            {
                templateByExerciseId[exercise.ExerciseId] = exercise;
            }

            foreach(var exercise in templateByExerciseId)
            {
                if(!requestByExerciseId.ContainsKey(exercise.Key))
                {
                    db.Remove(exercise.Value);
                }
            }
            int index = 1;
            foreach(var exercise in requestByExerciseId)
            {

                if(templateByExerciseId.ContainsKey(exercise.Key))
                {
                    var templateExercise = templateByExerciseId[exercise.Key];
                    var requestExercise = exercise.Value;
                    if(templateExercise.PlannedSets != requestExercise.PlannedSets)
                    {
                        templateExercise.PlannedSets = requestExercise.PlannedSets;
                    }
                    templateExercise.Order = index;
                }
                else
                {
                    var newTemplateExercise = new WorkoutTemplateExercise
                    {
                        Id = Guid.NewGuid(),
                        ExerciseId = exercise.Key,
                        PlannedSets = exercise.Value.PlannedSets,
                        Order = index,
                        WorkoutTemplateId = template.Id
                    };
                    db.Add(newTemplateExercise);
                }
                index+=1;
            }


            await db.SaveChangesAsync();

            return Results.NoContent();
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
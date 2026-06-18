using GymAssistant.Api.Data;
using GymAssistant.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GymAssistant.Api.Endpoints;

public static class WorkoutSessionsEndpoints
{
    public static void MapWorkoutSessionsEndpoints(this WebApplication app)
    {
        app.MapPost("/api/workout-sessions", async (
            CreateWorkoutSessionRequest request,
            AppDbContext db) =>
        {
            if (request.Exercises.Count == 0)
            {
                return Results.BadRequest("Workout session must contain at least one exercise.");
            }

            if (request.WorkoutTemplateId != null)
            {
                var templateExists = await db.WorkoutTemplates
                    .AnyAsync(x => x.Id == request.WorkoutTemplateId.Value);

                if (!templateExists)
                {
                    return Results.BadRequest("Workout template does not exist.");
                }
            }

            var exerciseIds = request.Exercises
                .Select(x => x.ExerciseId)
                .Distinct()
                .ToList();

            var existingExercisesCount = await db.Exercises
                .CountAsync(x => exerciseIds.Contains(x.Id));

            if (existingExercisesCount != exerciseIds.Count)
            {
                return Results.BadRequest("One or more exercises do not exist.");
            }

            var session = new WorkoutSession
            {
                Id = Guid.NewGuid(),
                WorkoutTemplateId = request.WorkoutTemplateId,
                UserId = "Caran",
                Date = DateTime.Now,
                Exercises = request.Exercises
                    .Select((exercise, exerciseIndex) => new WorkoutSessionExercise
                    {
                        Id = Guid.NewGuid(),
                        ExerciseId = exercise.ExerciseId,
                        Order = exerciseIndex + 1,
                        Sets = exercise.Sets
                            .OrderBy(x => x.SetNumber)
                            .Select((set, setIndex) => new WorkoutSessionSet
                            {
                                Id = Guid.NewGuid(),
                                SetNumber = setIndex + 1,
                                Weight = set.Weight,
                                Reps = set.Reps
                            })
                            .ToList()
                    })
                    .ToList()
            };

            db.WorkoutSessions.Add(session);
            await db.SaveChangesAsync();

            return Results.Created(
                $"/api/workout-sessions/{session.Id}",
                new CreateWorkoutSessionResponse(session.Id)
            );
        });

        app.MapGet("/api/workout-sessions/{id:guid}", async (
        Guid id,
        AppDbContext db) =>
        {
            var session = await db.WorkoutSessions
                .Include(x => x.Exercises)
                    .ThenInclude(x => x.Exercise)
                .Include(x => x.Exercises)
                    .ThenInclude(x => x.Sets)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (session == null)
            {
                return Results.NotFound();
            }
            
            var response = new WorkoutSessionResponseDto(
                session.Id,
                session.WorkoutTemplateId,
                session.UserId,
                session.Date,
                session.Exercises
                    .OrderBy(x => x.Order)
                    .Select(x => new WorkoutSessionExerciseResponseDto(
                        x.Id,
                        x.ExerciseId,
                        x.Exercise.Name,
                        x.Order,
                        x.Sets
                            .OrderBy(s => s.SetNumber)
                            .Select(s => new WorkoutSessionSetResponseDto(
                                s.Id,
                                s.SetNumber,
                                s.Weight,
                                s.Reps
                            ))
                            .ToList()
                    ))
                    .ToList()
            );

            return Results.Ok(response);
        });
    }
    
}



public record CreateWorkoutSessionRequest(
    Guid? WorkoutTemplateId,
    List<CreateWorkoutSessionExerciseRequest> Exercises
);

public record CreateWorkoutSessionExerciseRequest(
    Guid ExerciseId,
    List<CreateWorkoutSessionSetRequest> Sets,
    Guid SessionExerciseId
);  

public record CreateWorkoutSessionSetRequest(
    int SetNumber,
    decimal? Weight,
    int? Reps
);

public record CreateWorkoutSessionResponse(Guid Id);

public record WorkoutSessionResponseDto(
    Guid? Id,
    Guid? WorkoutTemplateId,
    string UserId,
    DateTime Date,
    List<WorkoutSessionExerciseResponseDto> Exercises
);

public record WorkoutSessionExerciseResponseDto
(
    Guid Id,
    Guid ExerciseId,
    string ExerciseName,
    int Order,
    List<WorkoutSessionSetResponseDto> Sets
);

public record WorkoutSessionSetResponseDto(
    Guid Id,
    int SetNumber,
    decimal? Weight,
    int? Reps
);

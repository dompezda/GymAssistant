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
    }
}

public record CreateWorkoutSessionRequest(
    Guid? WorkoutTemplateId,
    List<CreateWorkoutSessionExerciseRequest> Exercises
);

public record CreateWorkoutSessionExerciseRequest(
    Guid ExerciseId,
    List<CreateWorkoutSessionSetRequest> Sets
);

public record CreateWorkoutSessionSetRequest(
    int SetNumber,
    decimal? Weight,
    int? Reps
);

public record CreateWorkoutSessionResponse(Guid Id);
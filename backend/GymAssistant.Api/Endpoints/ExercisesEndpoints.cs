using GymAssistant.Api.Data;
using GymAssistant.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GymAssistant.Api.Endpoints;

public static class ExercisesEndpoints
{
    public static void MapExercisesEndpoints(this WebApplication app)
    {
        app.MapGet("/api/exercises", async (AppDbContext db) =>
        {
            var exercises = await db.Exercises.ToListAsync();

            var result = exercises
                .Select(x => new ExerciseListItemResponse(
                    x.Id,
                    x.Name,
                    x.MuscleGroup.HasValue ? (int)x.MuscleGroup.Value : null,
                    x.MuscleGroup?.ToString(),
                    x.IsSystem
                ))
                .ToList();

            return Results.Ok(result);
        });

        app.MapPost("/api/exercises", async (CreateExerciseRequest request, AppDbContext db) =>
        {
            try
            {
                var exercise = new Exercise
                {
                    Name = request.Name,
                    MuscleGroup = request.MuscleGroup
                };

                db.Exercises.Add(exercise);
                await db.SaveChangesAsync();

                return Results.Created($"/api/exercises/{exercise.Id}", exercise);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.ToString());
            }
        });
    }
}

public record CreateExerciseRequest(string Name, MuscleGroup? MuscleGroup);
public record ExerciseListItemResponse(
    Guid Id,
    string Name,
    int? MuscleGroup,
    string? MuscleGroupLabel,
    bool IsSystem
);
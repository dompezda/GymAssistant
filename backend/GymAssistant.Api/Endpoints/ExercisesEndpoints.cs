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
            return await db.Exercises.ToListAsync();
        });

        app.MapPost("/api/exercises", async (CreateExerciseRequest request, AppDbContext db) =>
        {
            var exercise = new Exercise
            {
                Name = request.Name,
                MuscleGroup = request.MuscleGroup
            };

            db.Exercises.Add(exercise);
            await db.SaveChangesAsync();

            return Results.Created($"/api/exercises/{exercise.Id}", exercise);
        });
    }
}

public record CreateExerciseRequest(string Name, MuscleGroup? MuscleGroup);
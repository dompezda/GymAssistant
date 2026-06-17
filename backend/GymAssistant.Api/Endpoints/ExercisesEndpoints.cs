using GymAssistant.Api.Data;
using GymAssistant.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GymAssistant.Api.Endpoints;

public static class ExercisesEndpoints {
  public static void MapExercisesEndpoints(this WebApplication app) {
    app.MapGet("/api/exercises", async (AppDbContext db) => {
      var result = await db.Exercises
        .Include(x => x.ExerciseMuscleGroups)
        .Select(x => new ExerciseListItemResponse(
          x.Id,
          x.Name,
          x.ExerciseMuscleGroups
          .Select(mg => (int) mg.MuscleGroup)
          .ToArray(),
          x.ExerciseMuscleGroups
          .Select(mg => mg.MuscleGroup.ToString())
          .ToArray(),
          x.IsSystem
        ))
        .ToListAsync();

      return Results.Ok(result);
    });

    app.MapPost("/api/exercises", async (CreateExerciseRequest request, AppDbContext db) => {
      if (string.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest("Nazwa ćwiczenia jest wymagana.");

      if (request.MuscleGroups is null || request.MuscleGroups.Length == 0)
        return Results.BadRequest("Wybierz przynajmniej jedną grupę mięśniową.");

      var invalidGroups = request.MuscleGroups
        .Where(x => !Enum.IsDefined(typeof (MuscleGroup), x))
        .ToArray();

      if (invalidGroups.Length > 0)
        return Results.BadRequest("Nieprawidłowa grupa mięśniowa.");

      var exercise = new Exercise {
        Name = request.Name.Trim(),
          ExerciseMuscleGroups = request.MuscleGroups
          .Distinct()
          .Select(group => new ExerciseMuscleGroup {
            MuscleGroup = (MuscleGroup) group
          })
          .ToList()
      };

      db.Exercises.Add(exercise);
      await db.SaveChangesAsync();

      return Results.Created($"/api/exercises/{exercise.Id}", new
      {
          id = exercise.Id,
          name = exercise.Name,
          muscleGroups = exercise.ExerciseMuscleGroups
              .Select(x => (int)x.MuscleGroup)
              .ToArray(),
          muscleGroupLabels = exercise.ExerciseMuscleGroups
              .Select(x => x.MuscleGroup.ToString())
              .ToArray(),
          isSystem = exercise.IsSystem
      });
    });
  }
}

public record CreateExerciseRequest(string Name, int[] MuscleGroups);
public record ExerciseListItemResponse(
  Guid Id,
  string Name,
  int[] MuscleGroups,
  string[] MuscleGroupLabels,
  bool IsSystem
);
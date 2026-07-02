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
          .ToList(),
          IsSystem = request.isSystem
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

    app.MapPatch("/api/exercises/{exerciseId:guid}", async (
       Guid exerciseId,
       EditExerciseRequest request,
       AppDbContext db
       ) => {
      if(exerciseId == Guid.Empty)
        return Results.BadRequest("ExerciseId cannot be empty");

      if(string.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest("Name cannot be empty");

      if(request.MuscleGroups == null || request.MuscleGroups.Length <= 0)
        return Results.BadRequest("MuscleGroup cannot be empty array");
      
      if(!request.MuscleGroups.All(x=>Enum.IsDefined(typeof(MuscleGroup),x)))
        return Results.BadRequest("Unknown muscle group");

      var exercise = db.Exercises.FirstOrDefault(x=>x.Id == exerciseId);
      if(exercise == null)
        return Results.NotFound(string.Format("Could not find exercise with id {0}",exerciseId));

      exercise.Name = request.Name.Trim();
      exercise.IsSystem = request.IsSystem;
      
      var existingExerciseMuscleGroupsIds = await db.ExerciseMuscleGroups.Where(x=>x.ExerciseId == exerciseId).Select(x=>(int)x.MuscleGroup).ToListAsync();
      var requestedIds = request.MuscleGroups.Distinct().ToList();

      var toAdd = requestedIds.Except(existingExerciseMuscleGroupsIds).ToList();
      var toRemove = existingExerciseMuscleGroupsIds.Except(requestedIds);
      foreach(var item in toRemove)
      {
        var elem = await db.ExerciseMuscleGroups.FirstOrDefaultAsync(x=>x.ExerciseId == exerciseId && (int)x.MuscleGroup == item);
        if(elem!=null)
          db.Remove(elem);
      }
      foreach(var item in toAdd.Distinct())
      {
        db.ExerciseMuscleGroups.Add(new ExerciseMuscleGroup
        {
          ExerciseId = exerciseId,
          MuscleGroup = (MuscleGroup)Enum.ToObject(typeof(MuscleGroup),item),
        });
      }

      await db.SaveChangesAsync();
      return Results.Ok(exercise.Id);
    });
  }
}

public record CreateExerciseRequest(string Name, int[] MuscleGroups, bool isSystem);
public record ExerciseListItemResponse(
  Guid Id,
  string Name,
  int[] MuscleGroups,
  string[] MuscleGroupLabels,
  bool IsSystem
);

public record EditExerciseRequest(
  string Name,
  int[] MuscleGroups,
  bool IsSystem
);
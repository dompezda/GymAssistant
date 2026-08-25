using System.Runtime.CompilerServices;
using GymAssistant.Api.Authentication;
using GymAssistant.Api.Data;
using GymAssistant.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace GymAssistant.Api.Endpoints;

public static class ExercisesEndpoints {
  public static void MapExercisesEndpoints(this WebApplication app) {


    var group = app.MapGroup("/api/exercises")
    .RequireAuthorization();
    group.MapGet("", async (AppDbContext db, ICurrentUser currentUser) => {
      var result = await db.Exercises
        .Where(x=>x.OwnerUserId==currentUser.UserId || x.IsSystem)
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

    group.MapPost("", async (CreateExerciseRequest request, AppDbContext db, ICurrentUser currentUser) => {
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
        OwnerUserId = currentUser.UserId,
        IsSystem = false
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

    group.MapPatch("/{exerciseId:guid}", async (
       Guid exerciseId,
       EditExerciseRequest request,
       AppDbContext db,
       ICurrentUser currentUser
       ) => {
      if(exerciseId == Guid.Empty)
        return Results.BadRequest("ExerciseId cannot be empty");

      if(string.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest("Name cannot be empty");

      if(request.MuscleGroups == null || request.MuscleGroups.Length <= 0)
        return Results.BadRequest("MuscleGroup cannot be empty array");
      
      if(!request.MuscleGroups.All(x=>Enum.IsDefined(typeof(MuscleGroup),x)))
        return Results.BadRequest("Unknown muscle group");

      var exercise = await db.Exercises.FirstOrDefaultAsync(x=>x.Id == exerciseId && x.OwnerUserId == currentUser.UserId && !x.IsSystem);
      if(exercise == null)
        return Results.NotFound(string.Format("Could not find exercise with id {0}",exerciseId));

      exercise.Name = request.Name.Trim();
      
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

    group.MapGet("/{exerciseId:guid}/history",async (
      Guid exerciseId,
      AppDbContext db,
      ICurrentUser currentUser
    ) =>
    {
      if(exerciseId==Guid.Empty)
        return Results.BadRequest("ExerciseId cannot be empty");
      var exercise =await db.Exercises.FirstOrDefaultAsync(x=>x.Id==exerciseId && (x.IsSystem || x.OwnerUserId==currentUser.UserId));
      if(exercise==null)
        return Results.NotFound($"Couldnt find exercise with id {exerciseId}");

      var workouts = await db.WorkoutSessions
      .Where(x=>x.UserId==currentUser.UserId)
      .Include(x=>x.Exercises)
      .ThenInclude(y=>y.Sets)
      .Where(x=>x.Exercises.Any(x=>x.ExerciseId==exercise.Id)).ToListAsync();

      var workoutExercises = workouts.Select(x=> new ExerciseHistoryWorkoutResponse
      (x.Id, x.StartedAt, x.Exercises.Where(y=>y.ExerciseId==exerciseId).SelectMany(y =>
      y.Sets.Select(z=>
      new WorkoutSet(z.Id,z.Reps,z.Weight,z.SetNumber))
      ).ToArray())).OrderByDescending(x=>x.WorkoutDate).ToList();



      ExerciseHistoryResponse res = 
      new ExerciseHistoryResponse(
        exercise!.Name,
        exercise.Id,
        workoutExercises
      );
      
      return Results.Ok(res); 

    });
  }
}

public record ExerciseHistoryResponse(
  string Name,
  Guid Id,
  List<ExerciseHistoryWorkoutResponse> Workouts
);

public record ExerciseHistoryWorkoutResponse(
  Guid Id,
  DateTime WorkoutDate,
  WorkoutSet[] Sets
);

public record WorkoutSet(
  Guid Id,
  int? Reps,
  decimal? Weight,
  int SetNumber
);

public record CreateExerciseRequest(string Name, int[] MuscleGroups);
public record ExerciseListItemResponse(
  Guid Id,
  string Name,
  int[] MuscleGroups,
  string[] MuscleGroupLabels,
  bool IsSystem
);

public record EditExerciseRequest(
  string Name,
  int[] MuscleGroups
);
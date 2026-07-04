using GymAssistant.Api.Data;
using GymAssistant.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Microsoft.VisualBasic;

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

            var exercisesDictionary = await db.Exercises
                .Where(x => exerciseIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name);
            var session = new WorkoutSession
            {
                Id = Guid.NewGuid(),
                WorkoutTemplateId = request.WorkoutTemplateId,
                UserId = "Caran",
                Exercises = request.Exercises
                    .Select((exercise, exerciseIndex) => new WorkoutSessionExercise
                    {
                        Id = Guid.NewGuid(),
                        ExerciseId = exercise.ExerciseId,
                        ExerciseNameSnapshot = exercisesDictionary[exercise.ExerciseId],
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
    
        app.MapPatch("/api/workout-sessions/{sessionId:guid}/exercises/{sessionExerciseId:guid}",async(
            Guid sessionId,
            Guid sessionExerciseId,
            UpdateWorkoutSessionExerciseRequest request,
            AppDbContext db) =>
        {

            if(request.Sets == null || request.Sets.Length == 0)
                return Results.BadRequest("Sets cannot be null or empty");
            
            if(request.Sets.Any(x=>x.reps<0 || x.weight <0))
                return Results.BadRequest("Reps and weight have to be 0 or greater for each set");
            HashSet<Guid> guids = request.Sets.Select(x=>x.id).ToHashSet<Guid>();
            if(guids.Contains(Guid.Empty))
                return Results.BadRequest("Set id cannot be empty");
            if(guids.Count != request.Sets.Length)
                return Results.BadRequest("Found duplicated ids");
            var session = await db.WorkoutSessions
            .Include(x => x.Exercises)
                .ThenInclude(x => x.Sets)
            .FirstOrDefaultAsync(x => x.Id == sessionId);

            if(session == null)
                return Results.NotFound();
            var exerciseFromDb = session.Exercises.FirstOrDefault(x=>x.Id == sessionExerciseId);
            if(exerciseFromDb == null)
                return Results.NotFound();
            var dbSets = exerciseFromDb.Sets;
        
            List<WorkoutSessionSet> setsToRemove = new List<WorkoutSessionSet>();
            dbSets.ForEach(x =>
            {
                
                if(!guids.Contains(x.Id))
                    setsToRemove.Add(x);
            });
            foreach(var item in setsToRemove)
            {
                db.WorkoutSessionSets.Remove(item);
            }

            int index = 1;
            foreach(var set in request.Sets)
            {
                var current = dbSets.FirstOrDefault(x=>x.Id == set.id);
                if(current == null)
                {
                    current = new WorkoutSessionSet
                    {
                        Id=set.id,
                        //Id = Guid.NewGuid();
                        WorkoutSessionExerciseId = exerciseFromDb.Id,
                        WorkoutSessionExercise = exerciseFromDb
                    };
                    db.WorkoutSessionSets.Add(current);
                    exerciseFromDb.Sets.Add(current);
                } 
                current.SetNumber = index;
                current.Reps = set.reps;
                current.Weight = set.weight;
                index +=1;    
            }
            await db.SaveChangesAsync();
            return Results.Ok();
        });
        app.MapPatch("/api/workout-sessions/{sessionId:guid}/finish",async(
            Guid sessionId,
            AppDbContext db
        )=>
        {
            if(sessionId == Guid.Empty)
                return Results.BadRequest("SessionId cannot be empty");
            var session = db.WorkoutSessions.FirstOrDefault(x=>x.Id == sessionId);
            if(session== null)
                return Results.BadRequest("Couldnt find Session with selected Id");
            session.FinishedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok();
        });

app.MapGet("/api/workout-sessions/{sessionId:guid}/summary", async (
    Guid sessionId,
    AppDbContext db
) =>
{
    if (sessionId == Guid.Empty)
        return Results.BadRequest("SessionId cannot be empty");

    var session = await db.WorkoutSessions
        .Include(x => x.Exercises)
            .ThenInclude(x => x.Sets)
        .FirstOrDefaultAsync(x => x.Id == sessionId);
    if (session == null)
        return Results.NotFound("Session not found");

    var result = new WorkoutSessionSummaryResponseDto(
        StartedAt: session.StartedAt,
        FinishedAt: session.FinishedAt,
        SessionExercises: session.Exercises
        .OrderBy(x=>x.Order)
        .Select(x =>
        {
            return new WorkoutSessionExerciseSummaryDto(
                ExerciseId: x.ExerciseId,
                ExerciseName: x.ExerciseNameSnapshot,
                Sets: x.Sets
                .OrderBy(x=>x.SetNumber)
                .Select(y =>
                {
                    return new WorkoutSessionSetSummaryDto
                    (
                        id: y.Id,
                        reps:y.Reps,
                        setNumber: y.SetNumber,
                        weight: y.Weight
                    );
                }).ToArray(),
                SessionExerciseId:x.Id,
                Order:x.Order
            );
        }).ToArray()
    );

    return Results.Ok(result);
});
    
app.MapPost("/api/workout-sessions/{sessionId:guid}/exercises", async (
    Guid sessionId,
    AddExerciseToWorkoutSessionRequest request,
    AppDbContext db) =>
    {
        if (sessionId == Guid.Empty)
            return Results.BadRequest("SessionId cannot be empty");
        if(request.ExerciseId == Guid.Empty)
            return Results.BadRequest("ExerciseId cannot be empty");
        var session = await db.WorkoutSessions
            .Include(x => x.Exercises)
            .FirstOrDefaultAsync(x => x.Id == sessionId);

        if (session == null)
            return Results.NotFound("Session not found");
        if(session.Exercises.Any(x => x.ExerciseId == request.ExerciseId))
            return Results.Conflict("Exercise already exists in the session");
        var exercise = await db.Exercises.FirstOrDefaultAsync(x => x.Id == request.ExerciseId);
        if(exercise == null)
            return Results.NotFound("Exercise does not exist");

        var newSessionExercise = new WorkoutSessionExercise
        {
            Id = Guid.NewGuid(),
            ExerciseId = request.ExerciseId,
            ExerciseNameSnapshot = exercise.Name,
            Order = session.Exercises.Count + 1,
            WorkoutSessionId = session.Id,
            Sets = new List<WorkoutSessionSet>
            {
                new WorkoutSessionSet
                {
                    Id = Guid.NewGuid(),
                    SetNumber = 1,
                    Reps = null,
                    Weight = null
                }
            }
        };
        db.WorkoutSessionExercises.Add(newSessionExercise);
        await db.SaveChangesAsync();

        var dto = new WorkoutSessionExerciseResponseDto(
            Id: newSessionExercise.Id,
            ExerciseId: newSessionExercise.ExerciseId,
            ExerciseName: newSessionExercise.ExerciseNameSnapshot,
            Order: newSessionExercise.Order,
            Sets: newSessionExercise.Sets
                .OrderBy(x => x.SetNumber)
                .Select(y => new WorkoutSessionSetResponseDto(
                    Id: y.Id,
                    SetNumber: y.SetNumber,
                    Reps: y.Reps,
                    Weight: y.Weight
                )).ToList()
        );
        return Results.Ok(dto);
    });    
    }    
}


public record AddExerciseToWorkoutSessionRequest(
    Guid ExerciseId
);

public record WorkoutSessionSummaryResponseDto(
 DateTime StartedAt,
 DateTime? FinishedAt,
 WorkoutSessionExerciseSummaryDto[] SessionExercises
);

public record WorkoutSessionExerciseSummaryDto(
    Guid ExerciseId,
    WorkoutSessionSetSummaryDto[] Sets,
    Guid SessionExerciseId,
    string ExerciseName,
    int? Order
);

public record UpdateWorkoutSessionExerciseRequest (
    WorkoutSessionSetSummaryDto[] Sets
);
public record WorkoutSessionSetSummaryDto(
    Guid id,
    int setNumber,
    int? reps,
    decimal? weight
);

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

using GymAssistant.Api.Authentication;
using GymAssistant.Api.Data;
using GymAssistant.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GymAssistant.Api.Endpoints;

public static class WorkoutSessionsEndpoints
{
    public static void MapWorkoutSessionsEndpoints(this WebApplication app)
    {

        var group = app.MapGroup("/api/workout-sessions")
            .RequireAuthorization();

        group.MapGet("",async(
            AppDbContext db,
            ICurrentUser currentUser)=>
        {
            var sessions = await db.WorkoutSessions
            .Where(x=>x.FinishedAt != null && x.UserId == currentUser.UserId)
            .OrderByDescending(x=>x.StartedAt)
            .Select(session => new WorkoutSessionListItemResponse(
                    session.Id,
                    session.Notes,
                    session.StartedAt,
                    session.FinishedAt,
                    session.Exercises.Count(),
                    session.Exercises.Sum(x=>x.Sets.Count()),
                    session.Exercises.SelectMany(x=>x.Sets).Sum(y=>(y.Reps ?? 0)*(y.Weight ?? 0)),
                    session.FinishedAt != null 
                        ? (int)(session.FinishedAt.Value - session.StartedAt).TotalMinutes 
                        : 0
                ))
            .ToListAsync();
            return Results.Ok(sessions);
        });


        group.MapPost("", async (
            CreateWorkoutSessionRequest request,
            AppDbContext db,
            ICurrentUser currentUser) =>
        {
            if (request.Exercises.Count == 0)
            {
                return Results.BadRequest("Trening musi zawierać przynajmniej jedno ćwiczenie.");
            }

            if (request.WorkoutTemplateId != null)
            {
                var templateExists = await db.WorkoutTemplates
                    .Where(x => x.UserId == currentUser.UserId)
                    .AnyAsync(x => x.Id == request.WorkoutTemplateId.Value);

                if (!templateExists)
                {
                    return Results.BadRequest("Wybrany plan treningowy nie istnieje.");
                }
            }

            var exerciseIds = request.Exercises
                .Select(x => x.ExerciseId)
                .Distinct()
                .ToList();

            var existingExercisesCount = await db.Exercises
                .Where(x=>x.OwnerUserId == currentUser.UserId || x.IsSystem)
                .CountAsync(x => exerciseIds.Contains(x.Id));

            if (existingExercisesCount != exerciseIds.Count)
            {
                return Results.BadRequest("Jedno lub więcej wybranych ćwiczeń nie istnieje.");
            }

            var exercisesDictionary = await db.Exercises
                .Where(x => exerciseIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name);
            var session = new WorkoutSession
            {
                Id = Guid.NewGuid(),
                WorkoutTemplateId = request.WorkoutTemplateId,
                UserId = currentUser.UserId,
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

        group.MapGet("/{id:guid}", async (
        Guid id,
        AppDbContext db,
        ICurrentUser currentUser) =>
        {
            var session = await db.WorkoutSessions
                .Where(x => x.UserId == currentUser.UserId)
                .Include(x => x.Exercises)
                    .ThenInclude(x => x.Sets)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (session == null)
            {
                return Results.NotFound("Nie znaleziono treningu.");
            }
            
            var response = new WorkoutSessionResponseDto(
                session.Id,
                session.WorkoutTemplateId,
                session.Exercises
                    .OrderBy(x => x.Order)
                    .Select(x => new WorkoutSessionExerciseResponseDto(
                        x.Id,
                        x.ExerciseId,
                        x.ExerciseNameSnapshot,
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
    
        group.MapPatch("/{sessionId:guid}/exercises/{sessionExerciseId:guid}",async(
            Guid sessionId,
            Guid sessionExerciseId,
            UpdateWorkoutSessionExerciseRequest request,
            AppDbContext db,
            ICurrentUser currentUser) =>
        {

            if(request.Sets == null || request.Sets.Length == 0)
                return Results.BadRequest("Ćwiczenie musi zawierać przynajmniej jedną serię.");
            
            if(request.Sets.Any(x=>x.reps<0 || x.weight <0))
                return Results.BadRequest("Liczba powtórzeń i ciężar nie mogą być ujemne.");
            HashSet<Guid> guids = request.Sets.Select(x=>x.id).ToHashSet<Guid>();
            if(guids.Contains(Guid.Empty))
                return Results.BadRequest("Id serii nie może być puste.");
            if(guids.Count != request.Sets.Length)
                return Results.BadRequest("Wykryto zduplikowane identyfikatory serii.");
            var session = await db.WorkoutSessions
            .Where(x => x.UserId == currentUser.UserId)
            .Include(x => x.Exercises)
                .ThenInclude(x => x.Sets)
            .FirstOrDefaultAsync(x => x.Id == sessionId);

            if(session == null)
                return Results.NotFound("Nie znaleziono treningu.");
            var exerciseFromDb = session.Exercises
                .FirstOrDefault(x=>x.Id == sessionExerciseId);
            if(exerciseFromDb == null)
                return Results.NotFound("Nie znaleziono ćwiczenia w tym treningu.");
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
        group.MapPatch("/{sessionId:guid}/finish",async(
            Guid sessionId,
            AppDbContext db,
            ICurrentUser currentUser
        )=>
        {
            if(sessionId == Guid.Empty)
                return Results.BadRequest("Id treningu nie może być puste.");
            var session =await db.WorkoutSessions
                .Where(x => x.UserId == currentUser.UserId)
                .FirstOrDefaultAsync(x=>x.Id == sessionId);
            if(session== null)
                return Results.BadRequest("Nie znaleziono treningu.");
            session.FinishedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok();
        });

        group.MapGet("/{sessionId:guid}/summary", async (
            Guid sessionId,
            AppDbContext db,
            ICurrentUser currentUser) =>
        {
    if (sessionId == Guid.Empty)
        return Results.BadRequest("Id treningu nie może być puste.");

    var session = await db.WorkoutSessions
        .Where(x => x.UserId == currentUser.UserId)
        .Include(x => x.Exercises)
            .ThenInclude(x => x.Sets)
        .FirstOrDefaultAsync(x => x.Id == sessionId);
    if (session == null)
        return Results.NotFound("Nie znaleziono treningu.");

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

        group.MapDelete("/{sessionId:guid}/exercises/{sessionExerciseId:guid}",async (
            Guid sessionId,
            Guid sessionExerciseId,
            AppDbContext db,
            ICurrentUser currentUser
        )=>
        {
            if(sessionId==Guid.Empty)
                return Results.BadRequest("Id treningu nie może być puste.");
            if(sessionExerciseId == Guid.Empty)
                return Results.BadRequest("Id ćwiczenia w treningu nie może być puste.");


    var session =await db.WorkoutSessions
        .Where(x=>x.UserId == currentUser.UserId)
        .Include(x=>x.Exercises)
        .FirstOrDefaultAsync(x=>x.Id == sessionId);
    if(session==null)
        return Results.NotFound("Nie znaleziono treningu.");

    if(session.FinishedAt!=null)
        return Results.Conflict("Nie można usuwać ćwiczeń z zakończonego treningu.");

    var exercise = session.Exercises
        .FirstOrDefault(x=>x.Id==sessionExerciseId);

    if(exercise==null)
        return Results.NotFound("Nie znaleziono ćwiczenia w tym treningu.");

    if(session.Exercises.Count<=1)
        return Results.Conflict("Nie można usunąć ostatniego ćwiczenia z treningu.");

    db.Remove(exercise);
    var remainingExercises = session.Exercises
        .Where(x=>x.Id!=exercise.Id)
        .OrderBy(x=>x.Order)
        .ToList();
    int index = 1;
    foreach(var item in remainingExercises)
    {
        item.Order = index;
        index+=1;
    }
    await db.SaveChangesAsync();
    return Results.NoContent();
});
    
    group.MapPost("/{sessionId:guid}/exercises", async (
        Guid sessionId,
        AddExerciseToWorkoutSessionRequest request,
        AppDbContext db,
        ICurrentUser currentUser) =>
    {
        if (sessionId == Guid.Empty)
            return Results.BadRequest("Id treningu nie może być puste.");
        if(request.ExerciseId == Guid.Empty)
            return Results.BadRequest("Id ćwiczenia nie może być puste.");
        var session = await db.WorkoutSessions
            .Where(x => x.UserId == currentUser.UserId)
            .Include(x => x.Exercises)
            .FirstOrDefaultAsync(x => x.Id == sessionId);

        if (session == null)
            return Results.NotFound("Nie znaleziono treningu.");
        if(session.Exercises.Any(x => x.ExerciseId == request.ExerciseId))
            return Results.Conflict("To ćwiczenie jest już dodane do treningu.");
        var exercise = await db.Exercises
        .Where(x=>x.OwnerUserId == currentUser.UserId || x.IsSystem)
        .FirstOrDefaultAsync(x => x.Id == request.ExerciseId);
        if(exercise == null)
            return Results.NotFound("Wybrane ćwiczenie nie istnieje.");

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

public record WorkoutSessionListItemResponse(
    Guid Id,
    string? Notes,
    DateTime StartedAt,
    DateTime? FinishedAt,
    int ExerciseCount,
    int SetCount,
    decimal? TotalVolume,
    int? DurationMinutes
);



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

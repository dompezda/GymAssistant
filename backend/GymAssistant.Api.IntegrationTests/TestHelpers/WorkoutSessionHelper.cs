using GymAssistant.Api.Data;
using GymAssistant.Api.Models;

public static class WorkoutSessionHelper
{
    public static async Task<WorkoutSession> CreateWorkoutSessionAsync(AppDbContext dbContext, ApplicationUser user, WorkoutTemplate? template, IEnumerable<Exercise> exercises)
    {
        var sessionExercises = new List<WorkoutSessionExercise>();
        int order =1;
        foreach(var exercise in exercises)
        {
            sessionExercises.Add(new WorkoutSessionExercise()
            {
                ExerciseId = exercise.Id,
                Order=order,
                ExerciseNameSnapshot = exercise.Name,
                Sets = new List<WorkoutSessionSet>()
                {
                  new WorkoutSessionSet()
                  {
                      Reps = 1,
                      Weight = 11,
                      SetNumber = 1
                  },
                  new WorkoutSessionSet()
                  {
                      Reps = 2,
                      Weight = 2,
                      SetNumber = 2
                  },   
                }
            });
            order++;
        }

        var session = new WorkoutSession()
        {
            User = user,
            //StartedAt = DateTime.UtcNow,
            WorkoutTemplate = template,
            Exercises = sessionExercises
        };
        dbContext.WorkoutSessions.Add(session);
        await dbContext.SaveChangesAsync();
        return session;
    }
}
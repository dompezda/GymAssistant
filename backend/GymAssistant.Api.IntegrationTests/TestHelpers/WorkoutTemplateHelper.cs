using GymAssistant.Api.Data;
using GymAssistant.Api.Models;

public static class WorkoutTemplateHelper
{
    public static async Task<WorkoutTemplate> CreateWorkoutTemplateAsync(AppDbContext dbContext, ApplicationUser user, IEnumerable<Exercise> exercises)
    {
        string guid = Guid.NewGuid().ToString();
        int order = 1;
        var workoutTemplateExercises = new List<WorkoutTemplateExercise>();
        foreach (var exercise in exercises)
        {
            var workoutTemplateExercise = CreateWorkoutTemplateExercise(exercise,order);
            workoutTemplateExercises.Add(workoutTemplateExercise);
            order++;
        }
        WorkoutTemplate workoutTemplate = new WorkoutTemplate
        {
            Title = $"{guid}Test Workout Template",
            Notes = "Test Notes",
            UserId = user.Id,
            Exercises = workoutTemplateExercises
        };
        dbContext.WorkoutTemplates.Add(workoutTemplate);
        await dbContext.SaveChangesAsync();
        return workoutTemplate;
    }

    private static WorkoutTemplateExercise CreateWorkoutTemplateExercise(Exercise exercise, int order)
    {
        return new WorkoutTemplateExercise
        {
            ExerciseId = exercise.Id,
            Order = order,
            PlannedSets = 3,
        };
    }
}
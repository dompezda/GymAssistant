using GymAssistant.Api.Data;
using GymAssistant.Api.Models;

public static class ExerciseHelper
{
    public static async Task<Exercise> CreateUserExerciseAsync(AppDbContext dbContext, ApplicationUser user)
    {
        string guid = Guid.NewGuid().ToString();
        Exercise exercise = new Exercise
        {
            Name = $"{guid}Test Exercise",
            OwnerUserId = user.Id,
            IsSystem = false
        };
        dbContext.Exercises.Add(exercise);
        await dbContext.SaveChangesAsync();
        return exercise;
    }
    public static async Task<Exercise> CreateSystemExerciseAsync(AppDbContext dbContext)
    {
        string guid = Guid.NewGuid().ToString();
        Exercise exercise = new Exercise
        {
            Name = $"{guid}Test Exercise",
            OwnerUserId = null,
            IsSystem = true
        };
        dbContext.Exercises.Add(exercise);
        await dbContext.SaveChangesAsync();
        return exercise;
    }
}
using GymAssistant.Api.Data;
using GymAssistant.Api.Models;

public static class ExerciseHelper
{
    public static async Task<Exercise> CreateUserExerciseAsync(AppDbContext dbContext, ApplicationUser user)
    {
        return await CreateUserExerciseAsync(dbContext, user, [MuscleGroup.FullBody]);
    }
    public static async Task<Exercise> CreateSystemExerciseAsync(AppDbContext dbContext)
    {
        return await CreateSystemExerciseAsync(dbContext, [MuscleGroup.FullBody]);
    }
    public static async Task<Exercise> CreateUserExerciseAsync(AppDbContext dbContext, ApplicationUser user, IEnumerable<MuscleGroup> muscleGroups)
    {
        if(muscleGroups == null || !muscleGroups.Any())
        {
            throw new ArgumentException("Muscle groups cannot be null or empty.", nameof(muscleGroups));
        }
        string guid = Guid.NewGuid().ToString();
        Exercise exercise = new Exercise
        {
            Name = $"{guid}Test Exercise",
            OwnerUserId = user.Id,
            IsSystem = false,
            ExerciseMuscleGroups = muscleGroups.Select(mg => new ExerciseMuscleGroup
            {
                MuscleGroup = mg
            }).ToList()
        };
        dbContext.Exercises.Add(exercise);
        await dbContext.SaveChangesAsync();
        return exercise;
    }
    public static async Task<Exercise> CreateSystemExerciseAsync(AppDbContext dbContext, IEnumerable<MuscleGroup> muscleGroups)
    {
        if(muscleGroups == null || !muscleGroups.Any())
        {
            throw new ArgumentException("Muscle groups cannot be null or empty.", nameof(muscleGroups));
        }
        string guid = Guid.NewGuid().ToString();
        Exercise exercise = new Exercise
        {
            Name = $"{guid}Test Exercise",
            OwnerUserId = null,
            IsSystem = true,
            ExerciseMuscleGroups = muscleGroups.Select(mg => new ExerciseMuscleGroup
            {
                MuscleGroup = mg
            }).ToList()
        };
        dbContext.Exercises.Add(exercise);
        await dbContext.SaveChangesAsync();
        return exercise;
    }
}
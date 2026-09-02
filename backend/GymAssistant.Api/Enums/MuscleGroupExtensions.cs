public static class MuscleGroupExtensions
{
    public static string ToDisplayName(this MuscleGroup muscleGroup) =>
        muscleGroup switch
        {
            MuscleGroup.Chest => "Klatka piersiowa",
            MuscleGroup.Back => "Plecy",
            MuscleGroup.Legs => "Nogi",
            MuscleGroup.Shoulders => "Barki",
            MuscleGroup.Biceps => "Biceps",
            MuscleGroup.Triceps => "Triceps",
            MuscleGroup.Core => "Mięśnie głębokie",
            MuscleGroup.FullBody => "Całe ciało",
            _ => muscleGroup.ToString()
        };
}

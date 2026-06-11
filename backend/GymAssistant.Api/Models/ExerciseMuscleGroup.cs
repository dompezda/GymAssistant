namespace GymAssistant.Api.Models;

public class ExerciseMuscleGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    public MuscleGroup MuscleGroup { get; set; }
}
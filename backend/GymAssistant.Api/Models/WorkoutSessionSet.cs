namespace GymAssistant.Api.Models;
public class WorkoutSessionSet
{
    public Guid Id { get; set; }

    public Guid WorkoutSessionExerciseId { get; set; }
    public WorkoutSessionExercise WorkoutSessionExercise { get; set; } = null!;

    public int SetNumber { get; set; }
    public decimal? Weight { get; set; }
    public int Reps { get; set; }
}
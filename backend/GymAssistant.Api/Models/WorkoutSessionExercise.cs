namespace GymAssistant.Api.Models;
public class WorkoutSessionExercise
{
    public Guid Id { get; set; }

    public Guid WorkoutSessionId { get; set; }
    public WorkoutSession WorkoutSession { get; set; } = null!;

    public Guid ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    public int Order { get; set; }
    public string ExerciseNameSnapshot { get; set; } = string.Empty;

    public List<WorkoutSessionSet> Sets { get; set; } = [];
}
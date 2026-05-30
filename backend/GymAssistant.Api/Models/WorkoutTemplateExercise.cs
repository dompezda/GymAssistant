namespace GymAssistant.Api.Models;
public class WorkoutTemplateExercise
{
    public Guid Id { get; set; }

    public Guid WorkoutTemplateId { get; set; }
    public WorkoutTemplate WorkoutTemplate { get; set; } = null!;

    public Guid ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    public int Order { get; set; }
    public int PlannedSets { get; set; }
}
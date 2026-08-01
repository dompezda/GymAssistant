namespace GymAssistant.Api.Models;

public class WorkoutSession
{
    public Guid Id { get; set; }

    public Guid? WorkoutTemplateId { get; set; }
    public WorkoutTemplate? WorkoutTemplate { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public DateTime StartedAt  { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }
    public string? Notes { get; set; }

    public List<WorkoutSessionExercise> Exercises { get; set; } = [];
}
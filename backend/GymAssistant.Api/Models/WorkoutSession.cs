namespace GymAssistant.Api.Models;

public class WorkoutSession
{
    public Guid Id { get; set; }

    public Guid? WorkoutTemplateId { get; set; }
    public WorkoutTemplate? WorkoutTemplate { get; set; }

    public string UserId { get; set; } = "Caran";
    public DateTime? StartedAt  { get; set; }
    public DateTime? FinishedAt { get; set; }

    public DateTime Date {get;set;}
    public string? Notes { get; set; }

    public List<WorkoutSessionExercise> Exercises { get; set; } = [];
}
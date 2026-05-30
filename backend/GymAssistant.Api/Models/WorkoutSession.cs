namespace GymAssistant.Api.Models;

public class WorkoutSession
{
    public Guid Id { get; set; }

    public Guid WorkoutTemplateId { get; set; }
    public WorkoutTemplate WorkoutTemplate { get; set; } = null!;

    public string UserId { get; set; } = "Caran";
    public DateTime Date { get; set; }
    public string? Notes { get; set; }

    public List<WorkoutSessionExercise> Exercises { get; set; } = [];
}
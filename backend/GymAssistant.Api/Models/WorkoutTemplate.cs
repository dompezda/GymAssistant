namespace GymAssistant.Api.Models;
public class WorkoutTemplate
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public List<WorkoutTemplateExercise> Exercises { get; set; } = [];
}
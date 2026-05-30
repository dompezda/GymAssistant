namespace GymAssistant.Api.Models;
public class WorkoutTemplate
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = "Caran";
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public List<WorkoutTemplateExercise> Exercises { get; set; } = [];
}
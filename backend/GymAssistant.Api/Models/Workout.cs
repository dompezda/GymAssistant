namespace GymAssistant.Api.Models;

public class Workout
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = "Caran";
    public DateTime Date { get; set; }
    public string? Notes { get; set; }

    public List<WorkoutExercise> WorkoutExercises { get; set; } = [];

}
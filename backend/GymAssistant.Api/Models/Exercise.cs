namespace GymAssistant.Api.Models;

public class Exercise
{
    public Exercise()
    {
        Id = Guid.NewGuid();
        IsSystem = false;
    }

    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<ExerciseMuscleGroup> ExerciseMuscleGroups { get; set; } = [];
    public Guid? OwnerUserId { get; set; }
    public bool IsSystem { get; set; }
}


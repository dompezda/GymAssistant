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
    public MuscleGroup? MuscleGroup { get; set; }
    public bool IsSystem { get; private set; }
}


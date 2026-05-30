using GymAssistant.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GymAssistant.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Exercise> Exercises => Set<Exercise>();

    public DbSet<WorkoutTemplate> WorkoutTemplates => Set<WorkoutTemplate>();
    public DbSet<WorkoutTemplateExercise> WorkoutTemplateExercises => Set<WorkoutTemplateExercise>();

    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();
    public DbSet<WorkoutSessionExercise> WorkoutSessionExercises => Set<WorkoutSessionExercise>();
    public DbSet<WorkoutSessionSet> WorkoutSessionSets => Set<WorkoutSessionSet>();
}
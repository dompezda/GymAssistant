using GymAssistant.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GymAssistant.Api.Data;

public class AppDbContext : IdentityUserContext<ApplicationUser, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(x => x.ApplicationUser)
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>()
            .Property(x => x.TokenHash)
            .IsRequired()
            .HasColumnType("binary(32)");
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(x => x.TokenHash)
            .IsUnique();

        modelBuilder.Entity<ExerciseMuscleGroup>()
            .HasOne(x => x.Exercise)
            .WithMany(x => x.ExerciseMuscleGroups)
            .HasForeignKey(x => x.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExerciseMuscleGroup>()
            .Property(x => x.MuscleGroup)
            .HasConversion<int>();

        modelBuilder.Entity<ExerciseMuscleGroup>()
            .HasIndex(x => new { x.ExerciseId, x.MuscleGroup })
            .IsUnique();

        modelBuilder.Entity<Exercise>()
            .ToTable(table => table.HasCheckConstraint(
                "CK_Exercises_SystemOwnership",
                "(IsSystem = 1 AND OwnerUserId IS NULL) OR (IsSystem = 0 AND OwnerUserId IS NOT NULL)"
            ));
    }

    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<ExerciseMuscleGroup> ExerciseMuscleGroups => Set<ExerciseMuscleGroup>();


    public DbSet<WorkoutTemplate> WorkoutTemplates => Set<WorkoutTemplate>();
    public DbSet<WorkoutTemplateExercise> WorkoutTemplateExercises => Set<WorkoutTemplateExercise>();

    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();
    public DbSet<WorkoutSessionExercise> WorkoutSessionExercises => Set<WorkoutSessionExercise>();
    public DbSet<WorkoutSessionSet> WorkoutSessionSets => Set<WorkoutSessionSet>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

}
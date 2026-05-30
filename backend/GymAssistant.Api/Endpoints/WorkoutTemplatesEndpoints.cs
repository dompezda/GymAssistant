using GymAssistant.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace GymAssistant.Api.Endpoints;

public static class WorkoutTemplatesEndpoints
{
    public static void MapWorkoutTemplatesEndpoints(this WebApplication app)
    {
        app.MapGet("/api/workout-templates", async (AppDbContext db) =>
        {
            var result = await db.WorkoutTemplates
                .Select(x => new WorkoutTemplateListItemResponse(
                    x.Id,
                    x.Title,
                    x.Notes
                ))
                .ToListAsync();

            return Results.Ok(result);
        });

    }   
}

public record WorkoutTemplateListItemResponse(
    Guid Id,
    string Title,
    string? Description
);
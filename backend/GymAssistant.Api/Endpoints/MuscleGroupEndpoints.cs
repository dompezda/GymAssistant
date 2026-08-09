using GymAssistant.Api.Data;
using GymAssistant.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GymAssistant.Api.Endpoints;
public static class MuscleGroupEndpoints
{
    public static void MapMuscleGroupsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/muscle-groups", () =>
        {
            return Enum.GetValues<MuscleGroup>()
                .Select(x => new
                {
                    value = (int)x,
                    label = x.ToString()
                });
        }).RequireAuthorization();
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymAssistant.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutSessionDiscardedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DiscardedAt",
                table: "WorkoutSessions",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscardedAt",
                table: "WorkoutSessions");
        }
    }
}

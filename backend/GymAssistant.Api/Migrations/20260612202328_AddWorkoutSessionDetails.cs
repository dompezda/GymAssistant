using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymAssistant.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutSessionDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Reps",
                table: "WorkoutSessionSets",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateTime>(
                name: "FinishedAt",
                table: "WorkoutSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "WorkoutSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExerciseNameSnapshot",
                table: "WorkoutSessionExercises",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinishedAt",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "WorkoutSessions");

            migrationBuilder.DropColumn(
                name: "ExerciseNameSnapshot",
                table: "WorkoutSessionExercises");

            migrationBuilder.AlterColumn<int>(
                name: "Reps",
                table: "WorkoutSessionSets",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}

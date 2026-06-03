using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymAssistant.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeWorkoutTemplateNullableInSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_WorkoutTemplates_WorkoutTemplateId",
                table: "WorkoutSessions");

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkoutTemplateId",
                table: "WorkoutSessions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_WorkoutTemplates_WorkoutTemplateId",
                table: "WorkoutSessions",
                column: "WorkoutTemplateId",
                principalTable: "WorkoutTemplates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutSessions_WorkoutTemplates_WorkoutTemplateId",
                table: "WorkoutSessions");

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkoutTemplateId",
                table: "WorkoutSessions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutSessions_WorkoutTemplates_WorkoutTemplateId",
                table: "WorkoutSessions",
                column: "WorkoutTemplateId",
                principalTable: "WorkoutTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

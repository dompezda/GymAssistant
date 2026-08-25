using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymAssistant.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseOwnershipConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Exercises_SystemOwnership",
                table: "Exercises",
                sql: "(IsSystem = 1 AND OwnerUserId IS NULL) OR (IsSystem = 0 AND OwnerUserId IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Exercises_SystemOwnership",
                table: "Exercises");
        }
    }
}

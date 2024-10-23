using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class UpdateExercise : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Exercise",
                newName: "ExerciseId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExerciseId",
                table: "Exercise",
                newName: "Id");
        }
    }
}

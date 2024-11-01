using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend_api.Migrations
{
    public partial class UpdateSchedule_v2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExerciseId",
                table: "Schedules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExerciseTypeId",
                table: "Schedules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_ExerciseId",
                table: "Schedules",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_ExerciseTypeId",
                table: "Schedules",
                column: "ExerciseTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Exercisese_ExerciseId",
                table: "Schedules",
                column: "ExerciseId",
                principalTable: "Exercisese",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_ExerciseTypes_ExerciseTypeId",
                table: "Schedules",
                column: "ExerciseTypeId",
                principalTable: "ExerciseTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Exercisese_ExerciseId",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_ExerciseTypes_ExerciseTypeId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_ExerciseId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_ExerciseTypeId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "ExerciseId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "ExerciseTypeId",
                table: "Schedules");
        }
    }
}
